# Pooling Runtime

`IObjectPoolManager` là public API; `ObjectPoolManager` giữ mapping prefab/instance và phát events. `ObjectPool` nội bộ giữ available queue/borrowed set; `PoolEntry` cache component root và `IPoolable` hooks một lần khi tạo instance.

`Load(count)` đảm bảo available tối thiểu. Spawn tự tăng pool; typed spawn dùng cached binding. Recycle false cho instance lạ/trùng; Cleanup chỉ hủy available. Unload/Dispose trả borrowed rồi hủy instance, không hủy prefab asset. API Unity main thread; bool/null/no-op cho input không hợp lệ, không explicit throw/try/catch.

Events sau state transition; CleanedUp có payload trước Destroy. Trong events, Spawn/Load/Cleanup/Unload/Dispose no-op hoặc false/null; Recycle được phép để Spawned handler trả object ngay. Reset hooks không tái nhập lifecycle. Callback consumer phải hợp lệ; module không bắt exception từ callback.
