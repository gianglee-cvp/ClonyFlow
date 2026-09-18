# Sử dụng Level JSON

## Chạy demo

Trong Unity, mở **ColonyFlow → Map → Open or Create Demo**, sau đó nhấn Play. Scene hiện tại là `Assets/_Game/_GamePlay/Scenes/MapDemo.unity`, đang gán `Assets/_Game/Data/Maps/map1.json`. Khi tạo scene mới, công cụ dùng `star-map.json`.

Mỗi level dùng **một JSON** chứa cả map, palette và hàng đợi hộp. Đổi level bằng cách gán `Map Json` trên `MapView`; `AntGameplay` lấy hàng đợi từ cùng model, không có JSON riêng hoặc fallback hộp.

## Dữ liệu

Giữ các trường map: `rows`, `columns`, `cameraPosition`, `cameraRotation`, `firstCellPosition`, `cellSpacing`, `cellScale` (tùy chọn), `palette`, `cells`. Thêm `queues`:

```json
"queues": [
  {
    "boxes": [
      { "colorId": 1, "antCount": 30 },
      { "colorId": 2, "antCount": 15 }
    ]
  }
]
```

- `queues` theo thứ tự trái sang phải; `boxes` theo thứ tự đầu đến cuối hàng. Không sort lại dữ liệu.
- `colorId` dùng chung palette với map; `antCount` là ngân sách kiến của hộp.
- Với level hoàn chỉnh, tổng `antCount` theo từng màu phải bằng số viên màu đó.
- JSON cần đúng cú pháp. Loader trả `null` khi giá trị trường không hợp lệ; không có logic exception để phục hồi JSON sai cú pháp.
- `demo-map-small.json` là cùng map demo với hộp nhỏ hơn; ngân sách vẫn đủ để hoàn thành.
- Demo có ba anchor hàng đợi. Số queue của JSON không được vượt số anchor được gán.

## Gắn vào scene

1. Thêm `MapView`, gán TextAsset level, prefab **CellView**, camera và map root.
2. Gán sẵn mảng renderer trên prefab `CellView`. Gán card surface nếu cần fit map vào card.
3. Trên `AntGameplay`, gán map, camera, prefab hộp/kiến, anchor hộp, điểm spawn, điểm vào card, anchor queue và các điểm về hang.
4. Prefab `BoxActor` cần face renderer, body renderers, count label, count canvas và hit collider. Prefab `AntActor` cần carried brick, carried renderer và abdomen.
5. Bật `Load On Start`, hoặc gọi `MapView.LoadMap()` rồi `AntGameplay.Initialize()`. Cả hai trả `bool` cho biết có thành công không.

Dùng menu component **Load Level JSON**, **Clear Map**, **Restart Level**, hoặc gọi `LoadJson(string)`, `Clear()`, `Restart()`.

## Quy ước và gameplay

- ID 0 là ô trống. Palette dùng HEX `#RRGGBB` hoặc `#RRGGBBAA`; alpha phụ thuộc material.
- Cells đọc theo hàng rồi cột; số phần tử bằng rows × columns. Cột tăng +X, hàng tăng -Z.
- Grid giữ pivot để tìm đường; mesh được nâng riêng lên card surface.
- Camera và layout demo được thiết lập trong Editor. Runtime không tự áp dụng camera position/rotation khi đổi level.
- Tô màu bằng MaterialPropertyBlock, không sửa shared material.
- Chỉ đi qua ô trống. Ưu tiên hướng tiếp cận thẳng thông thoáng; nếu cần rẽ, dùng Dijkstra với chi phí từ điểm vào card.
- Chọn cặp hộp–viên theo tổng quãng đường đi từ điểm spawn. Khoảng cách bằng nhau giữ thứ tự hàng, cột rồi slot.
- Viên đã được kiến đặt trước không được giao lại. Thu thập cập nhật khả năng tiếp cận và xóa cache đường đi.
- Renderer/canvas/collider được gán sẵn; không tìm component qua children/parent. Click tra hộp bằng collider đã đăng ký.
- Kiến, box và cell dùng `ColonyFlow.Core.Pooling`; manager giữ pool qua restart và dispose khi owner bị destroy. Pause và x2 do gameplay quản lý; không phụ thuộc Time.timeScale.

## Pooling

Core tại [Assets/_Core/Pooling](../Assets/_Core/Pooling/README.md), độc lập Tween/gameplay. Mỗi thư mục Core có MD mô tả trách nhiệm.

- `Load(count)` preload số available tối thiểu; spawn tự tăng nếu hết. `Recycle` tự deactivate/reset, trả false khi trùng hoặc instance lạ.
- Kiến preload 32, tăng theo nhu cầu; khi recycle hủy tween và xóa task/source/target/carried visual, khôi phục scale để dùng lại sau cú nhảy về 0.
- Box được preload theo JSON. Hết budget sẽ rời slot nhưng chờ `OutgoingCount == 0` trước khi recycle; pickup lỗi hoàn budget và box pending quay lại slot trống.
- Cell lưu vị trí visual trước khi recycle khi Collect; `GetCellVisualPosition` vẫn trả đúng điểm để kiến nhặt viên. Reload dùng lại instance và gán model/color/scale mới.
- Component root được cache khi tạo instance; không gọi lại GetComponent mỗi lần spawn và không tìm hierarchy.
- Cleanup/restart trả actor về pool trước khi hủy root session; pool root được giữ riêng.

## Animation bằng Core Tween

- `AntGameplay` khởi tạo Core Tween trong Play Mode. `ActorAnimation` giữ sequence trong `TweenScope` và cấp thời gian qua `Goto`; không cập nhật tween manual toàn cục.
- Box `DOJump` lên slot trong `Slot Jump Duration` (mặc định 0.35 giây), độ cao `Slot Jump Height` nhân `layoutUnit`. Slot được giữ ngay khi chọn; kiến chỉ xuất phát sau khi box đáp.
- Các box còn lại trượt lên bằng `DOMove`, với `Queue Move Duration` mặc định 0.25 giây. Box mới hiện từ hàng thứ tư cũng trượt lên; các cột còn lại trượt ngang khi queue rỗng.
- `Hole Approach Distance` xác định điểm rẽ trái/phải ở mép dưới, mặc định 12 nhân `layoutUnit` (khoảng 2.57 world unit trong demo). Kiến đi chéo đến điểm đứng ngoài hole rồi mới nhảy. Kiến ở trục giữa cũng chọn một phía tiếp cận.
- `Hole Jump Distance` là khoảng cách XZ từ điểm đứng đến tâm hole, mặc định 3 nhân `layoutUnit` (khoảng 0.64 world unit trong demo). Đặt khoảng này lớn hơn bán kính miệng hole để kiến đứng ngoài trước khi nhảy vào `holeJump`.
- Kiến `DOJump` trong `Jump Duration` (mặc định 0.55 giây). Scale tăng lên `Jump Scale Multiplier` (mặc định 1.25) trong 25% thời gian đầu, rồi giảm về 0 trong phần còn lại. `Jump Height` là độ cao theo world unit. Scale gốc được phục hồi khi bắt đầu chuyến mới.
- Pause/x2 áp dụng cho cả di chuyển và tween; disable, destroy hoặc restart hủy tween và không phát callback hoàn thành chuyến.

## Booster Thêm Slot

`AntGameplay.AddSlot()` thêm một slot, căn giữa hàng và tạo đủ anchor/spawn/entry. Mỗi màn dùng tối đa một lần; pause hoặc board đã clear sẽ từ chối. Có thể dùng để mở hàng slot đang deadlock. Box giữ nguyên index, tween sang anchor mới và chỉ dispatch sau khi đáp. `Restart()` phục hồi layout gốc và lượt dùng.

Scene phải gán `Slot Surfaces` theo thứ tự slot. Editor setup tạo sẵn các references này; runtime không tìm component trong child/parent.

## Booster Pickup

`BeginPickupSelection()` bật chế độ chọn box; `CancelBoosterSelection()` hủy. `PickupBox(box)` chọn một box Normal trong ba hàng queue đang hiển thị, cần slot trống. Có thể chọn box giữa/cuối; box được lấy đúng theo identity, queue dồn bằng tween. Chọn thành công hoặc Restart sẽ hủy selection. Pause và board đã clear không cho dùng booster.

JSON box có `kind` tùy chọn: `0` Normal (mặc định), `1` Hidden, `2` Stick. Pickup loại trừ Hidden/Stick; cơ chế riêng của hai loại này sẽ tích hợp sau. Runtime không suy ra loại box từ trạng thái active.

## Booster Thổi và UI

UI phía dưới có `Add Slot`, `Pickup`, `Blow`. Pickup chọn box trực tiếp trên queue; Blow mở các nút màu còn cell trên map. `Cancel` hủy lựa chọn. UI sở hữu vùng input của mình; đang chọn màu sẽ không vô tình pick box. Booster chưa tiêu hao vật phẩm.

`BeginBlowSelection()` mở lựa chọn màu; `AvailableBlowColors` cung cấp danh sách màu. `BlowColor(colorId)` remove toàn bộ cell màu đó qua pooling, đưa budget box cùng màu trong queue/slot/pending về 0 và dọn box rỗng. Outbound/WaitingPickup cùng màu được hủy, outgoing/reservation được giải phóng và không refund. Returning/Jumping đã lấy cell vẫn hoàn thành chuyến về hole; màn chỉ WIN khi các kiến này về hết. Màu khác giữ nguyên trip/budget/reservation. Navigation được rebuild ngay sau thao tác.

Pause, board đã clear hoặc màu không còn cell sẽ từ chối Thổi. Restart khôi phục JSON, layout và selection của màn.

## Kiểm tra

Kiểm tra dữ liệu, quy tắc code và serialized assets:

```powershell
python -B -m unittest discover -s Tools/Tests -v
```

Smoke test Unity chạy trong bản sao project dưới `Temp/LevelVerification`:

```powershell
python -B Tools/prepare_level_verification.py
& 'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/Project_unity/ColonyFlow/Temp/LevelVerification/Project' -executeMethod LevelSmokeChecks.Run -logFile 'D:/Project_unity/ColonyFlow/Temp/LevelVerification/smoke.log'
```

Bộ kiểm tra xác nhận load map/queue, thứ tự hộp, click collider, chọn viên gần, đường rẽ, pause, resize card, thu thập/trở về/nhảy, hoàn thành level, restart và animation box/kiến trong Play Mode. Kết quả ở `Temp/LevelVerification/smoke-results.txt`.
