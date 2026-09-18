# Sử dụng Core Pooling

```csharp
using ColonyFlow.Core.Pooling;

// owner là Transform trong scene; prefab là asset, không phải scene instance.
var pool = new ObjectPoolManager(transform);
pool.Load(antPrefab, 32); // minimum available, gọi lại không cộng dồn.
var ant = pool.Spawn(antPrefab, parent: runtimeRoot, spawnInWorldSpace: false);
// Configure/Begin dữ liệu task mới.
pool.Recycle(ant); // true lần đầu; false khi lạ hoặc đã trả.
// Giữ manager qua restart; chỉ dispose khi owner kết thúc.
pool.Dispose();
```

Assembly consumer tham chiếu `ColonyFlow.Core.Pooling`. Core chỉ dùng UnityEngine; chưa có key/async/Resource/DI adapters vì project chưa có các dependency đó.

GameObject/Component overload chia sẻ pool theo prefab GameObject. Position/rotation tùy chọn dùng defaults prefab, world hoặc local theo `spawnInWorldSpace`; scale reset về defaults. Parent phải cùng scene owner. Prefab null/count âm/manager disposed: Load/Recycle false, Spawn null, void no-op. count0 hợp lệ; retainCount0 hủy hết available. Prefab asset luôn là borrowed resource, không bị Unload hủy.

`IPoolable.OnPoolSpawned` chạy khi inactive trước activate; `OnPoolRecycled` chạy sau deactivate trước enqueue. Hook phải khởi tạo cache an toàn khi Awake chưa chạy do prewarm dưới parent inactive. Giữ cache renderer/collider/Transform; reset task, refs, visual, timers và scale cho mỗi lease. Tween CancelAll khi recycle, Dispose khi destroy.

Events: Instantiated chỉ tạo mới; Spawned mỗi acquire; Recycled mỗi release hợp lệ; CleanedUp khi Cleanup/Unload/Dispose hủy instance. Không báo CleanedUp cho object bị bên ngoài destroy. Spawned có thể Recycle ngay; caller kiểm tra state nếu dùng callback này. Các operation còn lại không tái nhập qua events; hooks không tái nhập. Không catch consumer callbacks.

AntGameplay giữ pool kiến/box qua restart. Box rời slot khi hết budget nhưng chờ OutgoingCount0 trước recycle; source được detach sau pickup Resolve. Pickup thất bại hoàn budget và box pending sẽ quay lại slot trống. MapView giữ pool cell; Collect snapshot vị trí trước recycle để ConfirmPickup vẫn nhận đúng vị trí, Clear xóa snapshots.

Kiểm tra PlayMode nằm trong `Tools/Unity/PoolingSmokeChecks.cs` và `LevelSmokeChecks.cs`; chuẩn bị bản sao bằng `python -B Tools/prepare_level_verification.py`, chạy Unity batch executeMethod `LevelSmokeChecks.Run`. Không chạy harness trong Assets của project chính.
