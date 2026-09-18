# Tiến trình phát triển Art Colony

> File theo dõi chung cho toàn project. Mỗi hệ thống hoặc tính năng được thêm thành một phần riêng; ghi nhận việc đã làm và đã kiểm chứng. Kế hoạch chưa triển khai không được đánh dấu là code hoàn thành.

## Trạng thái hiện tại

**Đang ở bước:** Đã viết code Map JSON/spawn; chờ người dùng setup và kiểm tra trực tiếp trong Unity. Đã kiểm tra biên dịch C# bằng thư viện Unity, chưa xác nhận runtime/test Unity đạt.

| Phần | Trạng thái | Ghi chú |
| --- | --- | --- |
| Phân tích game | Đã lưu tài liệu | [game-analysis.md](game-analysis.md). |
| Kế hoạch gameplay demo | Đã lưu, cập nhật Map | [Kế hoạch demo](plans/2026-09-17-gameplay-demo.md). |
| Map JSON và spawn | Đã chốt phạm vi, lưu kế hoạch | [Kế hoạch Map](plans/2026-09-17-map-json-spawn.md). |
| Loader/grid/spawn/camera | Đã triển khai, chờ nghiệm thu | Code tại `Assets/_Game/_GamePlay/Script/Map`. |
| Pathfinding và lấy Cell | Chưa triển khai | Sau phần tạo/hiển thị Map. |
| Queue, Slot, Ant, Win/Lose | Chưa triển khai | Theo kế hoạch gameplay demo. |
| Pooling Cell | Để sau | Bản đầu dùng Instantiate/Destroy. |

## Tiến trình theo từng phần

### MAP — Dữ liệu JSON và hiển thị Map

- **Trạng thái:** Đã triển khai, chờ kiểm tra trong Unity.
- **Phạm vi:** Đọc JSON, tạo grid, đặt camera, spawn/tô màu một prefab chung và clear/reload.
- **Kế hoạch:** [Map JSON và spawn](plans/2026-09-17-map-json-spawn.md).
- **Đã làm:** DTO/loader và validate JSON, grid runtime, palette ID/HEX, CellView, MapView dùng Instantiate/Destroy, camera, clear/reload; JSON mẫu; bộ EditMode tests; menu tạo/mở demo và hướng dẫn sử dụng.
- **Kiểm chứng:** Runtime, test và code Editor đã biên dịch bằng Roslyn với reference Unity 6000.3.9f1. Chưa có kết quả EditMode/Play Mode thành công.
- **Việc tiếp theo:** Người dùng mở menu tạo demo, chạy Play Mode và EditMode tests theo [hướng dẫn](map-usage.md).
- **Vấn đề còn lại:** Kiểm tra trực quan màu/camera, reload, Console và dữ liệu lỗi. Chưa có pathfinding hoặc pooling.

## Nhật ký toàn project

### 2026-09-17 — MAP: Viết code JSON và spawn

- Code tại `Assets/_Game/_GamePlay/Script/Map`, theo thư mục gameplay hiện có.
- JSON mẫu tại `Assets/_Game/Data/Maps/demo-map.json`: grid 5 × 5, 21 viên màu và 4 Cell trống.
- Reload chuẩn bị map mới trước khi clear map cũ; root cũ được vô hiệu hóa và dọn riêng, không xóa vật khác.
- Tô màu Renderer bằng MaterialPropertyBlock; hỗ trợ `_BaseColor` và `_Color`.
- Đã thêm menu `ColonyFlow → Map → Open or Create Demo` tạo prefab/material/scene và [hướng dẫn sử dụng](map-usage.md).
- Đã biên dịch C# runtime/test/Editor thành công. Chạy Unity trong project tạm gặp lỗi sandbox Package Manager, rồi lỗi thiếu paging memory khi biên dịch các package; chưa có kết quả runtime thành công.
- Người dùng nhận phần setup và test trong Unity; giữ trạng thái nghiệm thu chưa hoàn thành tới khi có phản hồi thực tế.

### 2026-09-17 — Chốt dữ liệu và cách spawn Map

- Map lưu bằng JSON, chứa bảng màu ID/HEX và grid ID một chiều.
- Cell dùng colorId; 0 là ô trống, không dùng enum tên màu cố định.
- JSON lưu vị trí và góc xoay camera, vị trí viên đầu tiên.
- Một prefab chung, chỉ đổi màu giữa các viên.
- Bản đầu dùng Instantiate/Destroy; pooling làm sau.
- Đã lưu quy ước đề xuất về spacing, world position và hướng hàng/cột trong kế hoạch Map.
- Đã tạo file theo dõi tiến trình và danh sách việc cần làm.

## Quy ước cập nhật và thêm phần mới

- File này theo dõi toàn project, không tạo file tiến trình riêng cho từng module.
- Mỗi phần có mã ổn định, ví dụ `MAP`, `QUEUE`, `SLOT`, `ANT`, `SAVE`, `AUDIO`; dùng cùng mã trong [todo.md](todo.md).
- Khi bắt đầu phần mới, thêm vào bảng trạng thái và mục “Tiến trình theo từng phần”; không ghi đè lịch sử phần cũ.
- Trạng thái sử dụng: Chưa bắt đầu, Đã lên kế hoạch, Đang làm, Chờ xử lý, Đã triển khai, Đã kiểm chứng.
- Sau mỗi lượt thực hiện, cập nhật phần tương ứng và thêm nhật ký có ngày, mã phần, thay đổi và kết quả kiểm tra.
- Chỉ chuyển sang “Đã kiểm chứng” khi có kết quả chạy/test tương ứng; cập nhật checkbox trong todo.md theo công việc thực sự hoàn thành.
- Kế hoạch chi tiết lưu tại `Docs/plans/`, hai file theo dõi chỉ giữ trạng thái, kết quả và liên kết.

### Mẫu thêm một phần mới

```markdown
### <MÃ PHẦN> — <Tên hệ thống/tính năng>

- **Trạng thái:** Chưa bắt đầu.
- **Phạm vi:** ...
- **Kế hoạch:** Chưa có / liên kết tài liệu.
- **Đã làm:** ...
- **Kiểm chứng:** Chưa kiểm tra / kết quả thực tế.
- **Việc tiếp theo:** ...
- **Vấn đề còn lại:** Không có / nội dung cần giải quyết.
```

### Mẫu nhật ký

```markdown
### YYYY-MM-DD — <MÃ PHẦN>: <Nội dung>

- Thay đổi: ...
- Kiểm tra và kết quả: ...
- Việc còn lại hoặc vấn đề: ...
```
