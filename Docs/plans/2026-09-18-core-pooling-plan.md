# Core Pooling — thiết kế và kế hoạch triển khai

Trạng thái: đã triển khai và xác minh PlayMode. User duyệt hướng 1 và yêu cầu mỗi folder trong Assets/_Core có MD.

## Mục tiêu và bằng chứng

Tạo Core dùng lại GameObject theo prefab và tích hợp kiến/box/cell, giữ nguyên nearest dispatch, queue tween, đường đi chéo, jump/pop/shrink và pause/x2.

Đã đọc tài liệu CongLy.Pooling do user cung cấp, Packages/manifest.json, Assets/_Core/Tween/Runtime, các actor và AntGameplay, ColonyFlow.Map.asmdef. Source hiện Instantiate mỗi box/kiến và Destroy khi xong. Manifest và tìm kiếm Assets chưa thấy UniTask, VContainer hoặc AssetsManager. Tài liệu nguồn mô tả API, không cung cấp implementation để xác minh.

Yêu cầu xác nhận: core pooling và sử dụng trong gameplay; cải thiện skill từ MD. Các yêu cầu trước còn áp dụng: không explicit throw/try/catch, cache component, không tìm qua children/parent.

### Các hướng

1. **Đề xuất:** manager theo prefab, scope gameplay, generic component cache, reset hook. Không cần package mới; đủ dùng ngay cho actor.
2. Sao chép đầy đủ CongLy với key/async/DI: cần thiết kế thêm Resource ownership và cài dependency đang thiếu; phạm vi lớn hơn nhu cầu hiện tại.
3. Pool riêng cho từng actor: ít public API nhưng lặp ownership/cleanup và khó tái sử dụng trong các module khác.

Lựa chọn đề xuất là hướng 1. Key/async/DI là phần mở rộng chưa triển khai, không giả lập hoặc đổi UniTask sang API khác âm thầm.

## Cấu trúc đề xuất

Namespace `ColonyFlow.Core.Pooling`, assembly độc lập UnityEngine; không phụ thuộc gameplay hoặc Tween.

| File mới | Nhiệm vụ | Owner |
|---|---|---|
| Assets/_Core/Pooling/Runtime/ColonyFlow.Core.Pooling.asmdef | Assembly core | Project |
| Assets/_Core/Pooling/Runtime/IObjectPoolManager.cs | API quản lý pool | Consumer |
| Assets/_Core/Pooling/Runtime/ObjectPoolManager.cs | Mapping prefab/instance, events, lifecycle | AntGameplay |
| Assets/_Core/Pooling/Runtime/ObjectPool.cs | Available queue, borrowed set, tạo/reset/destroy instance | Manager |
| Assets/_Core/Pooling/Runtime/IPoolable.cs | Reset actor trước spawn và khi recycle | Actor |
| Assets/_Core/Pooling/Docs/core-pooling-usage.md | Hợp đồng và ví dụ dùng | Project |

`ObjectPool` là class nội bộ giữ một Transform container inactive; không cần MonoBehaviour chạy Update. Khác topology MonoBehaviour trong tài liệu nguồn vì pool chỉ thực hiện thao tác đồng bộ theo lời gọi manager. Manager nhận parent scene hợp lệ; tạo root pool dưới gameplay, tách khỏi runtimeRoot. Không singleton hoặc DontDestroyOnLoad trong phiên bản này. Module tái sử dụng được vì lifetime do consumer chọn.

## Public API đề xuất

```csharp
public interface IObjectPoolManager : IDisposable
{
    event Action<GameObject> Instantiated;
    event Action<GameObject> Spawned;
    event Action<GameObject> Recycled;
    event Action<GameObject> CleanedUp;

    bool Load(GameObject prefab, int count = 1);
    bool Load<T>(T prefab, int count = 1) where T : Component;
    GameObject Spawn(GameObject prefab, Vector3? position = null,
        Quaternion? rotation = null, Transform parent = null,
        bool spawnInWorldSpace = true);
    T Spawn<T>(T prefab, Vector3? position = null,
        Quaternion? rotation = null, Transform parent = null,
        bool spawnInWorldSpace = true) where T : Component;
    bool Recycle(GameObject instance);
    bool Recycle(Component instance);
    void RecycleAll(GameObject prefab);
    void RecycleAll(Component prefab);
    void Cleanup(GameObject prefab, int retainCount = 1);
    void Cleanup(Component prefab, int retainCount = 1);
    void Unload(GameObject prefab);
    void Unload(Component prefab);
}

public interface IPoolable
{
    void OnPoolSpawned();
    void OnPoolRecycled();
}
```

Constructor đề xuất: `ObjectPoolManager(Transform owner)`. Owner null/scene không hợp lệ tạo manager không hoạt động: Load/Recycle false, Spawn null, các thao tác void no-op. API chỉ trên Unity main thread; không hứa thread safety.

Đổi có chủ đích so với nguồn: Load void → bool; Recycle void/throw → bool; tên namespace theo Core project; thêm reset hook; bỏ dependency không có ở giai đoạn này; recycle tự SetActive(false). Không thay đổi mặc định count=1, retainCount=1 và spawnInWorldSpace=true.

- `Load` đảm bảo số available tối thiểu, không cộng count mỗi lần gọi. count=0 hợp lệ; count âm/prefab null/manager disposed trả false. Component overload chung pool theo prefab.gameObject.
- `Spawn` tự tạo pool nếu chưa preload, tăng instance khi available rỗng; không warning bắt buộc. Invalid prefab/parent scene khác hoặc manager disposed trả null. Prefab phải thuộc asset; không nhận instance đang mượn như prefab mới.
- world-space true dùng world position/rotation; false dùng local position/rotation dưới parent. Giá trị bỏ qua dùng transform mặc định prefab theo cùng hệ tọa độ, không giữ tọa độ lần spawn trước. Reset localScale về scale gốc prefab trước actor hook.
- Typed overload cache component của root theo instance/type lúc tạo hoặc lần bind đầu, không GetComponent mỗi lần spawn và không tìm hierarchy. Component thiếu trả null và không để instance bị đánh dấu borrowed.
- `Recycle` false cho instance lạ, đã trả, destroyed hoặc manager disposed. Chỉ release hợp lệ phát event; không thêm available lần hai.
- `Cleanup` chỉ destroy available xuống retainCount; retainCount âm no-op. Borrowed không bị đụng tới.
- `RecycleAll` dùng snapshot borrowed để callback thay đổi tập hợp không phá vòng lặp. Hook/events là callback consumer, không catch exception. Không tuyên bố event hỗ trợ Unload/Dispose tái nhập nếu chưa thiết kế và kiểm tra guard cụ thể.
- `Unload` recycle borrowed của prefab rồi destroy toàn bộ available/container và xóa mapping; không destroy prefab asset. Manager Dispose hủy mọi pool và root của mình, idempotent.
- `CleanedUp(instance)` phát cho mỗi instance bị manager hủy, gồm Cleanup/Unload/Dispose; payload được gửi trước Destroy khi object còn truy cập được. Không phát cho instance đã bị external Destroy.

## State, reset và thứ tự events

Mỗi instance thuộc đúng một pool và ở đúng một trong hai tập: available hoặc borrowed. Instance externally destroyed được loại khỏi collection và cache; spawn bỏ qua entry null. Không tìm component lại mỗi frame. Cache hook chỉ lấy component root khi tạo instance; prefab actor đặt component ở root như source hiện tại.

Create dưới container inactive → lưu transform defaults/cache root components → đăng ký ownership → Instantiated.
Spawn → lấy available sống hoặc Create → reparent/configure transform khi inactive → thêm borrowed → OnPoolSpawned → activate → Spawned.
Recycle → xác nhận borrowed → bỏ borrowed → deactivate/hủy tween qua OnDisable → OnPoolRecycled → reparent container/reset defaults → enqueue available → Recycled.

Awake có thể chưa chạy khi prewarm dưới parent inactive. Hook actor phải khởi tạo cache một lần an toàn trước lần activate đầu, không giả định Awake đã chạy. OnEnable không được phụ thuộc dữ liệu task cũ. Khôi phục scale trước khi activate để kiến shrink về 0 không tái xuất hiện với scale 0.

Events được phát sau khi ownership/state tương ứng đã nhất quán. Callback Spawned có thể recycle ngay; trả instance cho caller với state hiện tại, không khẳng định còn borrowed sau callback. Quy định consumer không tái nhập lifecycle trong reset hook; events khác có guard theo trạng thái manager/pool.

## Tích hợp gameplay và ownership

AntGameplay giữ một manager trong lifetime scene. Initialize/Restart recycle actor của session trước, giữ pool để dùng lại; OnDestroy Dispose manager. Pool root không nằm trong runtimeRoot sẽ bị destroy mỗi restart. Prewarm đề xuất: kiến 32 available, box theo tổng số box của JSON. Đây là warmup, không hard cap; hiệu chỉnh sau profiling.

AntActor implement reset hook: hủy tween; reset state Inactive, task/source/target/routes/waypoint, pickup state; tắt carried brick; khôi phục scale gốc. TweenScope CancelAll trên recycle, Dispose chỉ khi object thực sự bị destroy. Begin vẫn là nơi gán một task mới.

BoxActor reset: hủy queue/slot tween; reset SlotIndex=-1, landing=false, Timer, count/color/queue/outgoing data và label trước Initialize. Giữ reference renderer/canvas/collider đã cache.

**Không recycle box khi thả kiến cuối cùng:** Spawn hiện gọi ReleaseBox ngay khi AntCount=0; kiến vẫn giữ Source và chưa Resolve. Bản pooled phải tách slot release và recycle:

1. Remove box khỏi slots/click registry khi ngân sách đã cấp hết.
2. Nếu OutgoingCount>0, giữ box inactive trong danh sách pending, chưa trả pool.
3. ResolvePickup cập nhật đúng source cũ và xóa source reference của kiến sau Resolve.
4. Khi OutgoingCount=0, remove pending rồi recycle box. Nhánh pickup thất bại hoàn ngân sách phải trả box về slot còn trống hoặc giữ pending retry theo quy tắc rõ ràng, không âm thầm mất budget. Trong flow hợp lệ collect đã reserved sẽ thành công; test riêng nhánh lỗi.
5. Restart hủy session rồi recycle tất cả queues/slots/pending/ants, clear ownership/reservation/collider mappings trước session mới.

Box trong queue và slot vẫn chỉ dùng một tween theo ActorAnimation; ant return đứng ngoài hole rồi jump/pop/shrink. Ant source/target không bị giữ qua lần reuse.

MapView sở hữu manager riêng cùng Core cho CellView, sống đến OnDestroy để reuse qua LoadJson/Restart. Clear trả các cell còn borrowed trước khi hủy Generated Map. CellView hook recycle xóa reference Cell; Initialize gán cell/color mới. Thu thập lưu vị trí visual vào snapshot theo Cell **trước** recycle rồi xóa active view mapping; GetCellVisualPosition đọc active view hoặc snapshot. Điều này giữ flow hiện tại Collect → GetCellVisualPosition → ConfirmPickup mà không đọc transform của cell đã tái sử dụng. Clear cũng xóa snapshot. MapView luôn tạo cell dưới Generated Map inactive và cấu hình transform/model trước khi activate root.

## Key/async/DI từ tài liệu nguồn

Giữ ngoài production giai đoạn này: `Load(object key)`, `UniTask LoadAsync(object key,...,CancellationToken ct)`, `[Key]` helpers, IAssetsManager/ILoggerManager và CongLy.DI/VContainer adapters. Muốn triển khai phải có hợp đồng Resource trước.

Thiết kế mở rộng phải xác định: hai key cùng prefab chia sẻ pool nhưng giữ asset lease riêng; direct prefab là borrowed asset; unload một key không phá pool của key khác/direct user; load cùng key chia sẻ operation với cancellation caller riêng; unload vô hiệu hóa completion cũ bằng generation; thất bại không cache success. Chưa chọn implementation vì không có Resource API để chứng minh ownership/release.

Mâu thuẫn nguồn: LoadAsync parameter tên ct nhưng example dùng cancellationToken; CleanedUp là Action<GameObject> nhưng flow ghi Invoke() không argument. Bản triển khai không được sao chép các snippet này nguyên xi như code đã biên dịch.

## Các task và kiểm tra dự kiến

1. **Core API/assembly:** tạo các file Runtime đề xuất; wire ColonyFlow.Map.asmdef tới Core.Pooling. Kiểm tra compile và không dependency vòng.
2. **Pool state:** prewarm, acquire/release, cache, transforms, Cleanup/Unload/Dispose/events. PlayMode: Load3 hai lần vẫn3, spawn/recycle dùng lại cùng ID, duplicate/foreign recycle false, invalid typed request không leak borrowed; local/world transforms/scale đúng.
3. **Actor reset:** implement IPoolable trong AntActor/BoxActor/CellView, tái dùng sau shrink/cancel. PlayMode: ant scale0 → recycle → Begin mới scale gốc; old task/source/brick không còn; tween canceled không phát completed task; cell reuse nhận đúng model/color/scale mới.
4. **Gameplay ownership:** thay Instantiate/Destroy actor bằng Spawn/Recycle trong AntGameplay và MapView; tách slot release/pending boxes; cache collider registration theo session, cell visual snapshot sau Collect. Test source box không được reuse trước pickup Resolve; failed pickup giữ ngân sách; restart khi đang queue shift/slot jump/hole jump không còn actor/reserve/tween cũ; Collect vẫn trả đúng vị trí pickup dù cell đã recycle.
5. **Skill:** bổ sung pooling-analysis.md và route trong unity-core-module-planner; giữ policy preserve source APIs nhưng chỉ rõ deliberate adaptation. Validate skill/reference links và thử scenario alias key, inactive parent, no dependencies. Update cần quyền ghi ngoài workspace cho skill user đã nêu; không chỉnh bản .agents read-only bằng cách vòng qua sandbox.
6. **Verification/docs:** mở rộng Tools/Unity/LevelSmokeChecks.cs và chạy bằng Tools/prepare_level_verification.py trong bản sao. Giữ 48 checks trước, thêm reuse/ownership; chạy 4 authored levels qua nhiều lần restart, so sánh instance IDs và số tạo mới sau warmup. Cập nhật Docs/map-usage.md và core-pooling-usage.md.

Kết quả thực chạy sau triển khai: **64 Unity PlayMode smoke checks, 0 failures** và **10 asset/documentation tests đạt**. Harness xác nhận minimum preload/reuse identity/reset scale/duplicate và foreign recycle/Cleanup giữ borrowed/Dispose, reuse cell qua restart không tạo mới sau warmup, giữ source box khi chờ pickup, refund và retry sau pickup lỗi, pickup position snapshot, cùng 4 level và animation pause/x2/restart. Các scenario key/async/DI chưa chạy vì nằm ngoài phạm vi production đã duyệt.

Skill cá nhân `C:/Users/Admin/.codex/skills/unity-core-module-planner` đã cập nhật pooling-analysis.md và routing; validator UTF-8 đạt, forward-test độc lập xác nhận hướng dẫn ownership/reset/cache/async không áp đặt kiến trúc. Assets/_Core và mọi module/subfolder hiện có đều có MD, được asset test kiểm tra.

## Phạm vi đã duyệt

Hướng 1: core theo prefab trước, lifecycle theo gameplay scene, automatic deactivate/reset, bool/null thay throw, pool kiến/box/cell với pending-source ownership và pickup position snapshot. Skill bổ sung reference pooling tổng quát. Key/async/DI sẽ chỉ triển khai khi có Resource contract/dependency và phạm vi được yêu cầu.
