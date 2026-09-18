# Kế hoạch thực hiện Gameplay Demo — Art Colony

> Khi thực hiện kế hoạch: dùng `superpowers:executing-plans` và triển khai từng phần, kiểm tra kết quả trước khi sang phần tiếp theo.

**Mục tiêu:** Tạo một màn demo 3D chơi được từ đầu đến cuối, kiểm chứng vòng lặp chọn Box → đưa lên Slot → spawn kiến → lấy Cell → mở mục tiêu mới → thắng/thua.

**Kiến trúc:** `DemoLevelController` điều phối một màn; Map, Queue, Slot và Ant có trách nhiệm riêng. Dữ liệu logic tách khỏi GameObject hiển thị để kiểm tra quy tắc mà không cần animation.

**Công nghệ:** Unity 6000.3.9f1, C#, URP, Input System, uGUI và Unity Test Framework đã có trong project.

**Tài liệu gốc:** [Phân tích game](../game-analysis.md).

**Trạng thái:** Phần Map đã chốt dùng JSON, ID màu và HEX, một prefab chung, vị trí/góc xoay camera và vị trí Cell đầu tiên. Bản đầu spawn bằng `Instantiate`, dọn bằng `Destroy`; pooling làm sau. Các giả định gameplay khác vẫn là đề xuất. Kế hoạch này không thực hiện code.

**Theo dõi:** [Tiến trình](../progress.md) · [Các việc cần làm](../todo.md) · [Kế hoạch Map JSON và spawn](2026-09-17-map-json-spawn.md).

---

## 1. Phạm vi demo

### Có trong demo đầu tiên

- Một scene gameplay riêng, camera top-down, bố cục màn hình dọc.
- Một map hình chữ nhật, Cell màu và Cell trống.
- Nhiều hàng đợi chỉ chứa Normal Box.
- Năm Slot cố định, nhận Box vào ô trống đầu tiên từ trái sang phải.
- Chọn Box đầu hàng đợi bằng click chuột hoặc chạm.
- Kiểm tra khả năng tiếp cận Cell theo weight/previousCell.
- Reserve Cell, spawn kiến theo interval, di chuyển ngoài map đến borderCell rồi xử lý lấy mục tiêu.
- Chọn Box cùng màu theo khoảng cách đến borderCell; hòa thì ưu tiên Slot index nhỏ hơn.
- Remove Box hết kiến, remove Queue rỗng và căn lại Queue.
- Nút x2, tự x2 khi Queue hết Box.
- Win, Lose do deadlock, nút Restart và hiển thị tiến độ Cell.
- Hình khối, màu phẳng và chuyển động đơn giản đủ để quan sát gameplay.

### Phần làm sau demo đầu tiên

Hidden Box, Stick Box, ba Booster, menu chính, lưu tiến trình, tiền, âm thanh/rung, tutorial, level editor, mỹ thuật hoàn chỉnh và tối ưu dành cho bản phát hành.

Các phần này vẫn thuộc thiết kế game đầy đủ. Chúng được tách thành những phần tiếp theo để demo đầu tập trung vào vòng lặp chính.

## 2. Các hướng làm và lựa chọn

| Hướng | Ưu điểm | Đánh đổi |
| --- | --- | --- |
| Demo 3D bằng primitive — đề xuất | Kiểm chứng logic, bố cục Slot/Queue và chuyển động kiến trong cùng một màn. | Cần dựng camera, view và đường đi ngoài map. |
| Mô phỏng bằng UI | Nhanh kiểm tra trạng thái và thứ tự lựa chọn. | Chưa kiểm chứng cảm giác gameplay 3D và di chuyển ngoài map. |
| Demo đủ Hidden/Stick/Booster | Kiểm chứng nhiều rule ngay từ đầu. | Nhiều trạng thái tương tác, khó xác định nguồn lỗi trước khi vòng lặp cơ bản ổn định. |

## 3. Quy tắc giữ nguyên và giả định demo

### Giữ nguyên từ tài liệu phân tích

1. Chỉ pick Box đầu Queue; cần có Slot trống.
2. Slot có index/vị trí cố định; không dồn Box đang ở Slot sang vị trí khác.
3. Láng giềng dẫn đường phải là Cell trống (`colorId = 0`, tương đương `None` trong tài liệu phân tích) có weight hợp lệ.
4. Chọn láng giềng hợp lệ có weight thấp nhất, lưu previousCell.
5. `CreatePath` chỉ trả về borderCell, không tạo path nội bộ để kiến chạy.
6. Kiến đi ngoài map; tại borderCell thực hiện animation/logic lấy targetCell.
7. Một target Cell chỉ được giữ cho một Ant tại một thời điểm.
8. Slot đầy chưa đủ để thua; cần kiểm tra khả năng tự tiến triển.
9. Win khi toàn bộ Cell màu đã được xử lý.

### Giả định cần xác nhận trước khi code

| Điểm chưa rõ | Lựa chọn đề xuất cho demo |
| --- | --- |
| Map mẫu | Dùng vùng hình màu 8 × 8, bao quanh bằng một vòng Cell `None` trong ma trận 10 × 10. Vòng trống là vùng kỹ thuật, không tính vào tiến độ. |
| Queue mẫu | Ba Queue, ba màu; số kiến được tính đúng theo số Cell màu tương ứng. Dùng giá trị nhỏ để quan sát được mỗi lượt xử lý. |
| Chọn target Cell | Duyệt Cell có thể lấy theo row rồi column tăng dần, xét Cell chưa reserve và có Box cùng màu đủ điều kiện spawn. |
| Các hướng có weight bằng nhau | Ưu tiên trên → dưới → trái → phải để kết quả ổn định. |
| Weight rìa | Dùng khoảng cách bước ngắn nhất dọc chu vi từ tâm cạnh dưới, cộng 1; nếu có hai Cell tâm thì lấy khoảng cách nhỏ nhất tới một trong hai. |
| Lan truyền weight | Cập nhật bằng lan truyền/relaxation đến khi không còn weight cần giảm; không coi một lượt duyệt ma trận là đủ cho mọi bố cục. |
| Di chuyển ngoài map | Chọn hướng ngắn nhất quanh chu vi; nếu hai hướng bằng nhau, chọn chiều kim đồng hồ. Khoảng cách chọn Box dùng chính tuyến di chuyển này. |
| Kiến đang đi | `antCount` giảm khi lấy Cell thành công, theo flow nguồn. Theo dõi `inFlightCount` riêng; chỉ được spawn khi `antCount - inFlightCount > 0`. |
| Đặt reserve | Đặt reserve và tăng in-flight khi thực sự cấp một lượt spawn; nếu chưa có borderCell hợp lệ thì không spawn, không giữ reserve. |
| Box gần nhất chưa tới interval | Chỉ xét các Box đã tới interval và còn kiến khả dụng; trong tập đó áp dụng quy tắc khoảng cách/index. |
| Hiệu ứng lấy Cell | Kiến tới điểm rìa, chạy hiệu ứng ngắn tại mục tiêu, Cell biến mất và kiến được thu hồi; không cho kiến đi xuyên map. |
| x2 | Nút đổi 1x/2x; khi Queue đã hết thì giữ 2x tới cuối màn. Restart trả về 1x. |
| Trạng thái chưa đầy Slot nhưng hết khả năng tiến triển | Chưa tự kết luận Lose ngoài rule gốc; dữ liệu demo được kiểm tra để không tạo trường hợp này ngoài ý muốn. |

Vòng trống kỹ thuật giúp xử lý mục tiêu nằm sát mép hình màu mà không cần đặt thêm rule cho targetCell trùng rìa ma trận trong demo đầu.

## 4. Cấu trúc file dự kiến

```text
Assets/ColonyFlow/
├── Scenes/GameplayDemo.unity
├── Scripts/Gameplay/
│   ├── Data/MapJsonData.cs
│   ├── Data/MapJsonLoader.cs
│   ├── Data/DemoLevelData.cs
│   ├── Map/Cell.cs
│   ├── Map/MapModel.cs
│   ├── Map/BorderRoute.cs
│   ├── Map/MapView.cs
│   ├── Boxes/BoxModel.cs
│   ├── Boxes/BoxView.cs
│   ├── Queues/BoxQueueModel.cs
│   ├── Queues/QueueController.cs
│   ├── Slots/SlotModel.cs
│   ├── Slots/SlotController.cs
│   ├── Ants/AntController.cs
│   ├── Ants/AntView.cs
│   ├── DemoLevelController.cs
│   └── DemoGameplayUI.cs
├── Data/Maps/demo-map.json
├── Data/DemoLevel.asset
├── Prefabs/Cell.prefab
├── Prefabs/Box.prefab
├── Prefabs/Ant.prefab
└── Tests/EditMode/
    ├── MapModelTests.cs
    ├── BorderRouteTests.cs
    ├── QueueSlotTests.cs
    └── DemoStateTests.cs
```

Đây là cấu trúc dự kiến, chưa tạo các file runtime. Nếu cần assembly definition cho test, thêm assembly gameplay và test tương ứng khi dựng bộ kiểm tra.

Map được lưu trong JSON: kích thước, mảng `cells` một chiều chứa ID màu, bảng `palette` ánh xạ ID sang HEX, vị trí/góc xoay camera, vị trí Cell đầu tiên và khoảng cách grid. Cell và Box runtime dùng `int colorId`, không dùng enum tên màu. `DemoLevelData` có thể giữ cấu hình Queue/Slot và reference tới JSON map; không còn là nơi lưu ma trận màu. Xem định dạng và quy ước tại kế hoạch Map riêng.

## 5. Luồng điều phối

```text
Input → QueueController → kiểm tra đầu Queue + Slot trống
      → chuyển Box lên Slot → refresh Queue

Tick theo speed multiplier
      → chọn target có thể lấy và Box đã tới interval
      → xác định borderCell + reserve + in-flight
      → spawn Ant → di chuyển chu vi → lấy Cell
      → cập nhật Map + số kiến + thu hồi Ant
      → refresh Queue/Slot → kiểm tra Win rồi deadlock
```

MapModel sở hữu màu, weight, previousCell và reservation. AntController sở hữu Ant active và bộ đếm in-flight; không để UI sửa dữ liệu gameplay trực tiếp. DemoLevelController điều phối thứ tự refresh, trạng thái màn và Restart.

## 6. Các phần thực hiện

### Phần 1 — Dữ liệu màn và trạng thái cơ bản

**File:** `Data/MapJsonData.cs`, `Data/MapJsonLoader.cs`, `Data/DemoLevelData.cs`, `Boxes/BoxModel.cs`, `Queues/BoxQueueModel.cs`, `Slots/SlotModel.cs` trong thư mục gameplay nêu trên.

1. Đọc bảng màu ID/HEX và dữ liệu Cell từ JSON; khai báo Queue và Box cho màn mẫu bằng cùng ID màu.
2. Biểu diễn map bằng mảng ID phẳng và kiểm tra số phần tử đúng kích thước, ID và HEX hợp lệ.
3. Tạo trạng thái runtime mới từ dữ liệu nguồn mỗi lần bắt đầu; không sửa JSON hoặc asset khi chơi.
4. Xác định số Cell màu và tổng kiến từng màu.
5. Báo lỗi dữ liệu khi số kiến âm, kích thước sai hoặc tổng kiến không khớp map mẫu.

**Đạt khi:** Khởi tạo lại luôn tạo trạng thái sạch; số kiến của mỗi màu bằng số Cell cần lấy của màu đó.

### Phần 2 — Map và tìm điểm rìa

**File:** `Map/Cell.cs`, `Map/MapModel.cs`, `Map/BorderRoute.cs`; kiểm tra tại `MapModelTests.cs`, `BorderRouteTests.cs`.

1. Viết kiểm tra logic cho Cell bị khóa, Cell tiếp cận được qua một hướng None và đường mở sau khi lấy Cell.
2. Tạo Cell, weight rìa, previousCell và lan truyền weight trong vùng trống.
3. Tạo truy vấn mục tiêu cùng màu chưa reserve và có điểm rìa hợp lệ.
4. Tạo `CreatePath` trả về borderCell/null, có bảo vệ chuỗi previousCell bất hợp lệ.
5. Cập nhật Cell đã lấy thành None, cập nhật trực tiếp bốn Cell kề và lan truyền thay đổi cần thiết.
6. Tính tuyến chu vi và khoảng cách dùng chung cho lựa chọn Box và chuyển động Ant.

**Đạt khi:** Cell kín không được chọn; mở đường làm mục tiêu mới khả dụng; chuỗi previousCell kết thúc ở rìa và không đi qua Cell màu.

### Phần 3 — Scene và hiển thị map

**File:** `Scenes/GameplayDemo.unity`, `Map/MapView.cs`, `Prefabs/Cell.prefab`, `Data/Maps/demo-map.json`.

1. Dựng camera và áp world position/Euler rotation từ JSON.
2. Spawn một prefab Cell chung bằng `Instantiate`, đặt vị trí theo ô đầu tiên và spacing trong JSON, tô màu theo palette. ID 0 không spawn. Dọn các vật thuộc map bằng `Destroy`; chuyển pooling ở phần tối ưu sau.
3. Đặt vùng map, Slot và Queue để quan sát được trên màn hình dọc.
4. Thêm cách xem trạng thái weight/target/reserve khi phát triển, không đưa vào HUD chơi chính.

**Đạt khi:** Xem rõ map và đủ chỗ cho Slot/Queue ở Game View 1080 × 1920, 720 × 1600 và tỉ lệ 3:4; Cell None không hiện như Cell màu cần lấy.

### Phần 4 — Pick Box, Queue và Slot

**File:** `Boxes/BoxView.cs`, `Queues/QueueController.cs`, `Slots/SlotController.cs`, `Prefabs/Box.prefab`; kiểm tra tại `QueueSlotTests.cs`.

1. Kiểm tra luật chỉ pick đầu Queue và Slot trống đầu tiên.
2. Nhận pointer qua Input System; raycast tới Box để xác định lựa chọn.
3. Khi pick hợp lệ, chuyển trạng thái Box từ Queue sang Slot trước khi chạy hiệu ứng.
4. Chặn input trùng lên Box đang chuyển, tránh một Box chiếm hai Slot.
5. Cập nhật đầu Queue; remove Queue rỗng và căn lại các Queue còn lại.
6. Hiển thị màu và số kiến trên Box.

**Đạt khi:** Box phía sau không pick được; Slot đầy chặn pick; Slot trống bên trái được dùng trước; click nhanh không nhân đôi Box.

### Phần 5 — Spawn kiến và lấy Cell

**File:** `Ants/AntController.cs`, `Ants/AntView.cs`, `Prefabs/Ant.prefab`, kết nối với `SlotController.cs` và `MapModel.cs`.

1. Kiểm tra reservation độc quyền, giới hạn in-flight và chọn Box cùng màu theo khoảng cách/index.
2. Tick timer Slot; chọn mục tiêu/Box đã sẵn sàng spawn.
3. Xác định borderCell, reserve mục tiêu và tăng in-flight trong cùng lượt cấp spawn.
4. Lấy Ant từ pool, gán nguồn Box, targetCell, borderCell và tuyến chu vi.
5. Di chuyển tới borderCell; thực hiện hiệu ứng lấy targetCell.
6. Hoàn tất lấy: cập nhật map, giảm antCount và in-flight, giải phóng reserve, trả Ant về pool.
7. Remove Box khi antCount bằng 0; tính lại mục tiêu sau khi map thay đổi.
8. Khi hủy màn, thu hồi Ant và dọn reservation mà không tính đó là lấy Cell thành công.

**Đạt khi:** Không lấy trùng Cell, không spawn quá số kiến, Box bị khóa màu chờ đến khi có mục tiêu hợp lệ và Box hết kiến giải phóng đúng Slot.

### Phần 6 — Vòng đời màn, x2, thắng/thua và Restart

**File:** `DemoLevelController.cs`, `DemoGameplayUI.cs`; kiểm tra trạng thái tại `DemoStateTests.cs`.

1. Tạo trạng thái demo `Playing`, `Win`, `Lose`; khởi tạo các hệ thống theo thứ tự dữ liệu → map → Queue/Slot → UI.
2. Refresh theo thứ tự Map → Queue → Slot → mục tiêu → Win → deadlock.
3. Chỉ kết luận deadlock khi Slot đầy, không có khả năng spawn hợp lệ, không có Ant có thể hoàn tất và không còn thay đổi tự giải phóng Slot.
4. Phân biệt Box đang chờ interval với Box không có mục tiêu; chờ interval không phải deadlock.
5. Thêm x2 dùng multiplier chung cho di chuyển, timer spawn và hiệu ứng gameplay.
6. Tự x2 khi Queue hết Box.
7. Hiển thị tiến độ, trạng thái Win/Lose và nút Restart.
8. Restart dọn Ant, reserve, timer, Queue, Slot và dựng lại runtime từ dữ liệu ban đầu.

**Đạt khi:** Slot đầy vẫn tiếp tục nếu còn Ant/mục tiêu; Win ưu tiên trước Lose; Restart liên tiếp không để lại Ant hoặc trạng thái cũ.

### Phần 7 — Kiểm chứng demo hoàn chỉnh

**File:** Scene demo, asset dữ liệu demo và các test logic liên quan.

1. Chuẩn bị dữ liệu một màn thắng được với chuỗi pick rõ ràng.
2. Chuẩn bị cấu hình kiểm tra thua: năm Box trên Slot không lấy được màu còn tiếp cận được, không có Ant active.
3. Chạy EditMode tests qua **Window → General → Test Runner → EditMode → Run All**; yêu cầu không có test fail.
4. Chơi thủ công các tình huống trong bảng nghiệm thu bên dưới.
5. Kiểm tra Console không có exception trong toàn bộ lượt chơi và Restart.

**Đạt khi:** Cả đường thắng, deadlock và các tình huống hồi phục đều đúng; demo chơi được trực tiếp trong Editor.

## 7. Tình huống nghiệm thu

| Tình huống | Kết quả cần thấy |
| --- | --- |
| Pick Box đầu Queue khi còn Slot | Box lên Slot trống đầu tiên; Queue cập nhật. |
| Pick Box chưa ở đầu | Không thay đổi trạng thái. |
| Click nhanh nhiều lần cùng Box | Chỉ một lần pick hợp lệ. |
| Box không có Cell cùng màu tiếp cận được | Box chờ, không spawn sai mục tiêu. |
| Hai Box cùng màu sẵn sàng spawn | Chọn theo khoảng cách chu vi, hòa chọn Slot index nhỏ hơn. |
| Nhiều Ant cùng hoạt động | Mỗi Ant có mục tiêu riêng, tổng lượt spawn không vượt kiến khả dụng. |
| Lấy Cell ngoài cùng của vùng hình màu | Cell biến mất; các Cell mới được mở có thể trở thành mục tiêu. |
| Slot đầy nhưng Ant còn làm việc | Chưa Lose. |
| Slot đầy, Box có mục tiêu nhưng đang chờ interval | Chưa Lose. |
| Slot đầy và deadlock thực sự | Chuyển Lose. |
| Cell màu cuối cùng được lấy | Chuyển Win. |
| Queue cuối cùng hết Box | Tự bật x2, kiến còn lại tiếp tục xử lý. |
| Restart trong lúc kiến đang đi và sau Win/Lose | Trở về dữ liệu ban đầu, 1x, không còn Ant/reserve cũ. |

## 8. Thứ tự nên thực hiện

**JSON Map → tạo grid/spawn/camera → Map/path logic → Queue/Slot/input → Ant/collect → Win/Lose/x2/Restart → nghiệm thu.**

Phần đầu tiên làm theo **kế hoạch Map JSON và spawn** để xác nhận dữ liệu, màu, vị trí và camera. Sau đó bổ sung path logic trước khi nối Box và Ant. Pooling cho Cell là việc tối ưu sau, không phải điều kiện để hoàn thành phần spawn đầu tiên.

Demo hoàn thành khi một màn thắng được, một trạng thái deadlock được nhận diện đúng, Restart ổn định và không có exception. Các hệ thống Hidden/Stick/Booster sẽ được lập kế hoạch riêng sau khi vòng lặp này hoạt động.
