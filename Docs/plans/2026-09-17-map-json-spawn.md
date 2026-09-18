# Kế hoạch Map JSON và spawn vật 3D

**Mục tiêu:** Đọc một map JSON, tạo grid runtime, đặt camera và spawn các viên 3D đúng vị trí/màu.

**Kiến trúc:** Loader đọc/kiểm tra dữ liệu; model giữ Cell và bảng màu; view chịu trách nhiệm tạo, tô màu và dọn GameObject. Dữ liệu không phụ thuộc vào cách spawn để sau này chuyển từ Instantiate/Destroy sang pooling.

**Công nghệ:** Unity, C#, JsonUtility, TextAsset, Renderer và MaterialPropertyBlock.

**Trạng thái:** Đã chốt JSON có camera position/rotation, vị trí viên đầu tiên và một prefab chung. Các quy ước tọa độ bên dưới là mặc định đề xuất. Đã viết runtime; kết quả kiểm chứng được theo dõi trong Docs/progress.md.

## 1. Phạm vi

- JSON chứa kích thước, bảng màu ID/HEX, màu từng Cell và thông tin bố cục/camera.
- Một prefab chung gán trong Inspector, chỉ đổi màu giữa các viên.
- Bản đầu dùng `Instantiate` khi tạo map và `Destroy` khi dọn map.
- Dùng JSON dưới dạng TextAsset gán trong Inspector cho demo.
- Chưa làm pathfinding, lấy Cell, Box, Ant hoặc pooling trong phần này.

## 2. Định dạng JSON

```json
{
  "rows": 2,
  "columns": 3,
  "cameraPosition": { "x": 0, "y": 12, "z": -6 },
  "cameraRotation": { "x": 60, "y": 0, "z": 0 },
  "firstCellPosition": { "x": -1, "y": 0, "z": 1 },
  "cellSpacing": { "x": 1, "z": 1 },
  "palette": [
    { "id": 1, "hex": "#E85D75" },
    { "id": 2, "hex": "#4DA8DA" }
  ],
  "cells": [
    1, 2, 0,
    2, 1, 1
  ]
}
```

## 3. Quy ước dữ liệu

- `cells` lưu theo hàng: `index = row * columns + column`.
- `colorId = 0` là Cell trống, không cần mục trong palette và không spawn GameObject.
- ID màu có giá trị dương, duy nhất trong palette; mọi ID khác 0 trong cells phải tồn tại trong palette.
- Palette riêng cho mỗi map. Cell/Box so khớp bằng ID; Renderer dùng màu đã chuyển từ HEX.
- HEX chấp nhận `#RRGGBB` và `#RRGGBBAA`. Alpha được đọc vào dữ liệu; hiển thị trong suốt phụ thuộc material của prefab.
- Tọa độ camera và Cell là world position; cameraRotation là world Euler angles tính bằng độ.
- `firstCellPosition` là vị trí pivot của prefab tại ô `[0,0]`, kể cả ô đó có ID 0. Prefab nên đặt pivot ở tâm viên để vị trí này cũng là tâm viên.
- Cột tăng theo `+X`, hàng tăng theo `-Z`, toàn bộ Cell dùng Y của vị trí đầu.
- Spacing là khoảng cách tâm/pivot giữa các viên, không phải khoảng hở giữa mesh.
- Không tự căn giữa hoặc tự dịch map vì JSON đã quyết định điểm bắt đầu.
- Góc camera, projection, field of view hoặc orthographic size là những tham số khác nhau: phần này chỉ đọc position/rotation; projection và kích thước góc nhìn cấu hình sẵn trong scene.

```text
position.x = firstCellPosition.x + column * cellSpacing.x
position.y = firstCellPosition.y
position.z = firstCellPosition.z - row * cellSpacing.z
```

## 4. File dự kiến

| File | Trách nhiệm |
| --- | --- |
| `Assets/_Game/_GamePlay/Script/Map/Data/MapJsonData.cs` | DTO serializable, giữ các trường JSON. |
| `Assets/_Game/_GamePlay/Script/Map/Data/MapJsonLoader.cs` | Parse, validate và chuyển HEX sang màu Unity. |
| `Assets/_Game/_GamePlay/Script/Map/Cell.cs` | Dữ liệu runtime: row, column và colorId; trường path bổ sung ở phần sau. |
| `Assets/_Game/_GamePlay/Script/Map/MapModel.cs` | Tạo grid và bảng màu runtime mới cho mỗi lượt load. |
| `Assets/_Game/_GamePlay/Script/Map/MapView.cs` | Gán JSON/prefab/camera/map root, load, spawn và clear. |
| `Assets/_Game/_GamePlay/Script/Map/CellView.cs` | Gắn Cell vào GameObject và tô Renderer theo palette. |
| `Assets/_Game/Data/Maps/demo-map.json` | Dữ liệu demo. |
| `Assets/_Game/_GamePlay/Prefabs/MapCell.prefab` | Một prefab viên dùng chung. |
| `Assets/_Game/_GamePlay/Scenes/MapDemo.unity` | Scene kiểm tra riêng phần Map. |
| `Assets/_Game/_GamePlay/Tests/Editor/MapJsonTests.cs` | Kiểm tra parse, ID/HEX, kích thước và tọa độ. |

Code Map đã được tạo trong Assets/_Game/_GamePlay/Script/Map. Công cụ Editor tạo/mở scene, prefab và material tại menu ColonyFlow → Map → Open or Create Demo.

## 5. Các việc thực hiện

### Bước 1 — Loader và kiểm tra JSON

1. Tạo DTO có các trường serializable phù hợp JsonUtility.
2. Kiểm tra JSON rỗng, parse lỗi, thiếu trường bắt buộc.
3. Kiểm tra rows/columns dương, số cells khớp kích thước; tránh tràn khi nhân kích thước.
4. Kiểm tra palette không có ID trùng/âm/0, HEX sai định dạng hoặc ID Cell không tồn tại.
5. Kiểm tra position/rotation hữu hạn và spacing hữu hạn, lớn hơn 0.
6. Trả dữ liệu đã kiểm tra hoặc thông báo lỗi chỉ rõ trường/Cell gây lỗi.

**Nghiệm thu:** JSON sai không tạo map một phần và không gây lỗi khó hiểu do truy cập mảng/palette.

### Bước 2 — Grid runtime

1. Chuyển cells phẳng thành grid, giữ cả Cell trống.
2. Lưu colorId thay cho enum tên màu, tạo dictionary ID → Color.
3. Tính world position theo quy ước hàng/cột.
4. Load mới tạo model mới, không sửa nội dung TextAsset.

**Nghiệm thu:** Số Cell logic bằng rows × columns, tọa độ đúng cả khi ô đầu tiên trống.

### Bước 3 — Spawn và tô màu

1. Kiểm tra prefab, Renderer, camera và map root trước khi thay map đang hiển thị.
2. Dùng `Instantiate` tạo một viên cho mỗi Cell khác 0 dưới root dành riêng cho map.
3. Đặt world position theo JSON; không để transform root làm sai vị trí.
4. Áp màu bằng MaterialPropertyBlock, theo thuộc tính shader của material prefab, chẳng hạn `_BaseColor` cho URP Lit.
5. Không sửa shared material hoặc tạo material mới không cần thiết cho từng Cell.
6. Đặt world position và Euler rotation cho camera được gán.

**Nghiệm thu:** Chỉ màu thay đổi giữa các viên, đúng ID/HEX và không làm viên khác đổi màu theo.

### Bước 4 — Clear và load lại

1. Theo dõi các viên do MapView tạo; chỉ dọn các vật thuộc map này.
2. Dùng `Destroy` cho bản đầu; bỏ tham chiếu runtime và ngừng dùng root cũ ngay.
3. Vì Destroy trong Play Mode thực thi cuối frame, vô hiệu hóa root cũ trước khi tạo root mới để tránh hiển thị/collider trùng khi reload cùng frame.
4. JSON mới phải parse/validate thành công trước khi dọn map đang hoạt động; nếu JSON sai thì giữ map cũ.
5. Cho phép gọi Load/Clear rõ ràng để nối với vòng đời Level sau này.

**Nghiệm thu:** Load nhiều lần không nhân đôi map; Clear không xóa vật ngoài MapView.

### Bước 5 — Dữ liệu mẫu và kiểm chứng

1. Tạo JSON mẫu có ít nhất hai màu, Cell 0, vị trí đầu và camera khác origin.
2. Dựng MapDemo, gán TextAsset, prefab, camera và map root trong Inspector.
3. Chạy EditMode tests cho dữ liệu hợp lệ/sai và công thức tọa độ.
4. Kiểm tra trong Play Mode số viên bằng số Cell khác 0, màu/vị trí/camera đúng JSON.
5. Load lại nhiều lần và Clear, kiểm tra Hierarchy/Console không còn vật trùng hoặc exception.

**Nghiệm thu:** Chạy được scene MapDemo từ JSON mà không cần sửa code để đổi bố cục/màu/vị trí camera.

## 6. Pooling — phần làm sau

Khi chuyển pooling, thay thao tác tạo/dọn trong MapView thành lấy/trả object. Giữ nguyên định dạng JSON, grid, palette và công thức vị trí. Khi tái sử dụng viên phải reset Cell reference, màu, transform và trạng thái active. Không xây hệ thống pool trong bước hiện tại.
