# Các việc cần làm — Art Colony

> Danh sách công việc chung cho toàn project, mở rộng theo từng hệ thống/tính năng. Kết quả thực hiện ghi ở [progress.md](progress.md).

## DOC — Tài liệu và quyết định chung

- [x] Lưu tài liệu phân tích gameplay.
- [x] Lưu kế hoạch gameplay demo.
- [x] Chốt Map JSON, bảng màu ID/HEX và một prefab chung.
- [x] Chốt JSON có camera position/rotation và vị trí viên đầu tiên.
- [x] Chốt bản đầu dùng Instantiate/Destroy, pooling làm sau.
- [x] Lưu [kế hoạch Map JSON/spawn](plans/2026-09-17-map-json-spawn.md).
- [x] Tạo file tiến trình và danh sách công việc.

## MAP — Map JSON và spawn

**Ưu tiên hiện tại.** Kế hoạch: [Map JSON và spawn](plans/2026-09-17-map-json-spawn.md).

- [x] Tạo DTO và loader JSON.
- [x] Viết validate kích thước, Cell count, ID, HEX, tọa độ và spacing.
- [x] Tạo grid runtime bằng colorId và bảng màu ID → Color.
- [x] Viết công thức vị trí theo viên đầu tiên, hàng/cột và spacing.
- [x] Viết CellView tô màu riêng từng Renderer.
- [x] Viết spawn bằng Instantiate, bỏ qua Cell 0.
- [x] Viết áp camera position/rotation từ JSON.
- [x] Viết Clear bằng Destroy và reload thay thế map cũ.
- [x] Tạo JSON mẫu.
- [x] Viết công cụ Editor tạo/mở prefab, material và scene MapDemo.
- [x] Thêm bộ EditMode tests và kiểm tra biên dịch C#.
- [ ] Người dùng tạo/mở demo bằng menu ColonyFlow → Map → Open or Create Demo.
- [ ] Người dùng kiểm tra Play Mode: 21 viên, đúng màu/vị trí/camera, reload không nhân đôi.
- [ ] Kiểm tra JSON lỗi không làm mất map đang hoạt động.
- [ ] Chạy kiểm tra logic và nghiệm thu hiển thị trong Unity.
- [x] Ghi kết quả biên dịch và các hạn chế kiểm tra hiện tại vào progress.md.
- [ ] Cập nhật kết quả nghiệm thu Unity sau phản hồi người dùng.

## MAP-PATH — Tìm đường và cập nhật Map

- [ ] Chốt các giả định pathfinding/gameplay còn lại trước khi code phần tương ứng.
- [ ] Tính weight/previousCell và tìm borderCell.
- [ ] Cập nhật Map sau khi lấy Cell.

## QUEUE — Hàng đợi và Box

- [ ] Queue và pick Normal Box.

## SLOT — Slot gameplay

- [ ] Năm Slot cố định và quy tắc ô trống đầu tiên.

## ANT — Kiến và lấy Cell

- [ ] Reserve mục tiêu, spawn Ant và giới hạn in-flight.
- [ ] Di chuyển Ant quanh map và lấy Cell.

## LEVEL — Vòng đời màn và nghiệm thu demo

- [ ] Refresh, Win/Lose, x2 và Restart.
- [ ] Nghiệm thu demo chơi được từ đầu tới cuối.

## POOL — Pooling

- [ ] Chuyển spawn/clear Cell sang pooling, reset đầy đủ trạng thái khi tái sử dụng.

## BOX-SPECIAL — Các loại Box đặc biệt

- [ ] Hidden Box.
- [ ] Stick Box.

## BOOSTER — Booster gameplay

- [ ] Booster AddSlot, Pickup và RemoveColor.

## Các phần dự kiến, chưa có kế hoạch chi tiết

- [ ] SAVE — Lưu tiến trình người chơi.
- [ ] UI — Menu và các giao diện ngoài demo.
- [ ] TUTORIAL — Hướng dẫn chơi.
- [ ] AUDIO — Âm thanh.
- [ ] HAPTIC — Rung.
- [ ] ART — Mỹ thuật.

## Quy ước cập nhật và thêm phần mới

- Dùng file này cho toàn project, không tạo danh sách công việc riêng thay thế file chung.
- Mỗi hệ thống/tính năng có một mục theo mã ổn định; dùng cùng mã trong progress.md khi bắt đầu theo dõi tiến trình phần đó.
- Thêm phần mới vào trước mục quy ước này. Chia công việc thành checkbox có kết quả cụ thể và liên kết kế hoạch chi tiết nếu có.
- Giữ các công việc đã hoàn thành với `[x]` để lưu lịch sử. Không đánh dấu hoàn thành chỉ vì đã lên kế hoạch.
- Các phần chưa rõ phạm vi chỉ ghi trong mục dự kiến; khi có kế hoạch thì chuyển thành mục riêng.
- Nếu phát sinh việc mới hoặc lỗi, thêm checkbox vào đúng phần và ghi bối cảnh trong progress.md.
- Ưu tiên hiện tại có thể thay đổi theo giai đoạn; không hiểu thứ tự các mục là thứ tự bắt buộc thực hiện toàn project.

### Mẫu thêm một phần mới

```markdown
## <MÃ PHẦN> — <Tên hệ thống/tính năng>

**Kế hoạch:** Chưa có / liên kết tài liệu.
**Phụ thuộc:** Không có / mã phần cần hoàn thành trước.

- [ ] Chốt phạm vi và các quyết định còn thiếu.
- [ ] <Công việc cụ thể>.
- [ ] Kiểm tra và nghiệm thu phần này.
- [ ] Cập nhật kết quả trong progress.md.
```
