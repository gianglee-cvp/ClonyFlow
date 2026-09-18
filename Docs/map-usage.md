# Sử dụng Map JSON

## Chạy demo

Trong Unity, mở **ColonyFlow → Map → Open or Create Demo**, sau đó nhấn Play. Công cụ tạo hoặc mở scene `Assets/_Game/_GamePlay/Scenes/MapDemo.unity`, prefab và material dùng chung. Scene đọc `Assets/_Game/Data/Maps/demo-map.json`.

## Gắn Map vào scene khác

1. Thêm component `MapView` vào GameObject quản lý Map.
2. Gán `Map Json` là TextAsset JSON, `Cell Prefab` là một prefab có Renderer, `Map Camera` là camera cần đặt vị trí và `Map Root` là Transform tổ chức các viên.
3. Bật `Load On Start` để spawn khi bắt đầu Play Mode.
4. Trong menu component, dùng **Load Map JSON** để reload và **Clear Map** để dọn. Trong code, dùng `LoadJson(string)` và `Clear()`.

MapView tạo root riêng dưới Map Root; không xóa các GameObject khác. JSON lỗi sẽ báo lỗi và giữ map đang hoạt động. Vật cũ được vô hiệu hóa ngay trước khi Destroy thực thi cuối frame. Ở Edit Mode, thao tác dọn dùng DestroyImmediate.

## Quy ước

- ID 0 là ô trống; các ID dương tra màu trong palette riêng của map.
- Dùng HEX `#RRGGBB` hoặc `#RRGGBBAA`. Trong suốt phụ thuộc material, không chỉ phụ thuộc alpha trong JSON.
- Cells đọc theo row rồi column; số phần tử bằng rows × columns.
- Vị trí đầu là pivot ô `[0,0]`, kể cả khi ô đó trống.
- Cột tăng +X, hàng tăng -Z; spacing là khoảng cách giữa pivot các viên.
- Camera position và rotation là world position và Euler degrees. Projection/FOV/orthographic size vẫn cấu hình trong scene.
- Material prefab cần shader có `_BaseColor` hoặc `_Color`. Tô màu bằng MaterialPropertyBlock, không sửa shared material.
- Bản hiện tại dùng Instantiate/Destroy, chưa dùng pooling hoặc pathfinding.

Định dạng JSON đầy đủ: [Kế hoạch Map](plans/2026-09-17-map-json-spawn.md).

## Kiểm tra

Mở **Window → General → Test Runner → EditMode → Run All**, hoặc chọn assembly `ColonyFlow.Map.Tests`. Bộ test kiểm tra dữ liệu lỗi, grid/vị trí, alpha HEX, màu Renderer, camera, reload và Clear.
