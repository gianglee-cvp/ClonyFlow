# Booster và quy trình phát triển — plan đã duyệt

Trạng thái: hoàn thành implementation, verification và commit theo thiết kế user đã duyệt. Thêm Slot (`9a1583e`), Pickup (`d203f57`), Thổi/UI (`9d93872`) đã được test/review/commit riêng.

## Câu trả lời đã chốt

User chọn plan/review rồi triển khai3 booster; Hidden/Stick chỉ có điều kiện loại trừ để tích hợp cơ chế sau; Thêm Slot tối đa1 lần/màn; chưa tiêu hao vật phẩm cho booster.

## Thiết kế đã duyệt

- Thêm Slot tạo slot thứ5 bằng runtime layout, cập nhật đồng thời anchor/spawn/entry/slots, giữ index slot cũ. Reset về số slot gốc khi restart. Dùng được khi đang deadlock, nhưng không sau khi board đã cleared. Cờ dùng1 lần reset mỗi màn.
- Pickup bật chế độ chọn box bất kỳ đang hiển thị trong queue, gồm box ở giữa/cuối của phần hiển thị. Box ngoài màn hình chưa có click target; không mở cơ chế reveal Hidden Box. Model có eligibility/box-kind contract để loại trừ Hidden/Stick, mặc định Normal cho JSON cũ. Chọn thành công đưa box lên slot bằng tween, remove đúng vị trí rồi dồn queue. Không có slot trống thì không tiêu thụ thao tác; có thể hủy chế độ chọn.
- Thổi mở lựa chọn màu từ palette còn cell. Xử lý matching Outbound/WaitingPickup bằng outcome không refund: settle outgoing, detach source, xóa reservation và trả kiến về pool. Matching Returning/Jumping tiếp tục animation về hole vì pickup đã resolve. Sau đó remove cell màu, zero matching queue/slot budgets, dọn empty boxes theo lifecycle pooling, rebuild navigation và refresh win/deadlock. Box pending cũng được xử lý để không giữ budget cũ. Booster không hoàn budget màu bị thổi; không làm ảnh hưởng kiến/màu khác.
- UI có3 thao tác và chế độ chọn box/màu, hủy selection rõ ràng; không thêm inventory/purchase UI.
- Rule project ở Docs/development-rules.md, mô tả naming/biến/hàm một trách nhiệm/folder/cache/pooling. Skill planner chỉ thêm hướng dẫn checkpoints có điều kiện khi user yêu cầu build/test/review/commit, giữ scope planner; không nhét luật booster vào skill Core.
- Review baseline phát hiện exhausted-budget stall: thêm detection deadlock khi board chưa cleared, không còn active trip/budget supply; giữ loader cho map visualization, không ép mọi JSON có queue. Sửa/test/commit baseline riêng trước3 booster.

## Kết quả agent review

P2 hiện tại: loader chấp nhận queues rỗng/thiếu ngân sách nhưng deadlock chỉ kiểm tra full slots. P1 integration: Blow không được gọi cancel có refund; identify trip color trước khi cell.ColorId thành0; rebuild navigation ngay. AddSlot phải cập nhật các indexed arrays cùng lúc. Arbitrary Pickup cần acquisition theo box identity và eligibility, không dùng activeSelf để suy ra Hidden/Stick.

Candidate optimization: đánh giá route cost tách khỏi dựng interior list cho candidate thua; chỉ làm khi profiling chứng minh chi phí, không refactor speculative trong booster.

## Yêu cầu

- Lập plan trước, xác định phần trước/sau, review thiết kế và làm rõ câu hỏi.
- Build/test từng phần, agent review code AI về refactor/bug/optimization, sửa vấn đề rồi commit từng phần được kiểm tra.
- Bổ sung skill/rule về naming, biến, từ khóa phù hợp, mỗi hàm một nhiệm vụ và tổ chức folder.
- Thêm Slot: thêm một ô vào hàng slot.
- Pickup: chọn một box bất kỳ trong queue, loại trừ Hidden Box và Stick Box.
- Thổi: chọn màu, remove mọi cell màu đó và đưa ngân sách box cùng màu trong slot/queue về0.

## Bằng chứng hiện tại

AntGameplay quản lý slot qua boxAnchors/antSpawnPoints/perimeterEntries và slots với độ dài cố định; PickQueue chỉ lấy index0. Model chưa có Hidden/Stick. Pool box giữ source tới khi OutgoingCount0; source detach sau Resolve. Có reservation theo Cell/taskId và cache navigation. Những yếu tố này phải được thiết kế cùng booster, không chỉ thêm nút UI.

## Thứ tự đề xuất

1. **Review baseline:** agent read-only review Core Pooling/gameplay và xác định lỗi hiện tại, khác với rủi ro của feature mới. Chạy kiểm tra cần thiết trước thay đổi. Kiểm tra staged changes; không gom thay đổi chưa review vào commit booster.
2. **Chốt contracts:** trả lời 3 câu hỏi; xác định cách chọn màu/box, số slot, eligibility, lượt dùng và hành vi với kiến đang đi. Present thiết kế để user duyệt.
3. **Rule/skill:** naming C# nhất quán theo source; PascalCase type/public API, camelCase biến/field; tránh tên mơ hồ và magic number có ý nghĩa; predicate đặt theo điều kiện, command theo hành động. Mỗi hàm có một trách nhiệm có thể mô tả rõ; không chia vụn máy móc chỉ để giảm số dòng. Giữ project policy không explicit throw/try/catch, cache root components, không hierarchy lookup. Phân biệt rule project với hướng dẫn skill tái sử dụng.
4. **Slot:** tách slot runtime khỏi serialized layout seed và tạo đủ anchor/spawn/entry tương ứng. Test thêm slot, layout/route, full-slot pick, restart, pause và giới hạn dùng. Build/review/fix/commit phần này trước.
5. **Pickup:** API pick theo box identity/index thay vì chỉ queue front; validate ownership/eligibility, remove đúng box, dồn queue tween, giữ slot landing gate. Test đầu/giữa/cuối, box không hợp lệ, Hidden/Stick theo contracts đã duyệt, click sau queue reflow và recycle. Build/review/fix/commit.
6. **Thổi:** một transaction gameplay cho màu được chọn. Dừng giao task màu đó; quyết định cancel/reroute/return kiến và hoàn/không hoàn budget theo contract; clear reservations; remove cells qua MapView và pooling; zero budgets cả queue/slot; xử lý pending sources/outgoing counters; rebuild navigation; cập nhật complete/deadlock. Test outbound/WaitingPickup/Returning/Jumping, source chưa resolve, các màu khác, nhiều lần bấm và restart. Build/review/fix/commit.
7. **UI/docs:** nối hành động theo pattern UI hiện có hoặc phương án được duyệt; MD mỗi folder Core; usage/schema docs cho gameplay. Test input flow và smoke tất cả level.
8. **Review cuối:** agent kiểm tra hành vi/correctness trước tối ưu. Chỉ tối ưu khi chỉ ra chi phí lặp hoặc lifetime issue cụ thể; không mở rộng abstraction theo phỏng đoán. Kiểm tra diff và commit phạm vi đã xác minh.

## Folder đã triển khai

Gameplay giữ Map/Data cho schema; Map/Ant cho actor/navigation hiện có; Map/Booster cho logic từng booster và coordinator khi cần chia responsibilities. UI chỉ chọn đối tượng/màu và gọi gameplay API. Core Pooling/Tween không phụ thuộc booster, không đưa luật game vào Core. Tách folder theo trách nhiệm và hướng dependency, không tách mỗi class thành một folder.

## Verification và commit

`Tools/Tests/test_level_assets.py` kiểm tra JSON/assets/MD và project policy; `Tools/Unity/LevelSmokeChecks.cs` và `PoolingSmokeChecks.cs` kiểm tra PlayMode trong bản sao. Thêm scenario booster có kết quả quan sát được, không chỉ kiểm tra code giống implementation. Mỗi phần phải build/test và review đạt trước commit. Commit chỉ paths/diff của phần đó; staged changes đang có cần phân loại trước, không commit toàn workspace bằng git add all.

Checkpoint đã xác minh: baseline deadlock 65 smoke; Thêm Slot 80 smoke; Pickup 98 smoke; transaction Thổi 113 smoke, đều không lỗi. Test đỏ đã được quan sát cho API chưa có trước từng phần. Agent review phát hiện fixture kind bị lặp; fixture đã sửa. Test pointer cần Physics.SyncTransforms trước khi lấy bounds của collider vừa reuse. Test tích hợp cũng phát hiện panel UI cố định 108 pixel chặn box giữa trên headless screen 640x480 (pointer y=93.09); UI và vùng input nay dùng cùng hệ số scale.

Verification cuối: **125 PlayMode smoke checks, 0 failures; 10 asset tests đạt; diff check sạch**. Bao phủ đầy slot/deadlock/slot thứ năm, pause, Pickup theo identity/collider/kind, pooling reset, Thổi ở bốn trip states, slot/queue/pending budgets, reservations, màu khác, repeated calls, palette/selection, completion và restart. Agent read-only review từng phần và tích hợp cuối không còn finding cần sửa. Chưa tích hợp gameplay riêng của Hidden/Stick, inventory hoặc tiêu hao vật phẩm theo scope đã chốt.

## Follow-up: Thêm Slot và session sau script reload

Scene đã lưu thêm được slot thứ năm khi test không dựng lại layout. Log Editor thực tế có NullReference ở `AntGameplay.Advance` khi navigation không còn nhưng initialized vẫn true. Lifecycle cũ không cleanup trước reload hoặc rebuild khi enable. Regression mới đã quan sát test đỏ rồi kiểm chứng sửa: disable phục hồi seed layout, giải phóng map/session/pool; enable rebuild hoặc đợi Map active; Model mới reset WIN; input/UI/booster chỉ dùng session hợp lệ, stale palette được clear. Button/API dùng chung `CanAddSlot` để không trình bày action bấm được nhưng thiếu references. Agent review hai vòng phát hiện startup deferred và stale GUI/input; đã sửa.

Kết quả follow-up: **135 smoke checks, 0 failures; 10 asset tests pass; diff check sạch**. Bao phủ saved scene, cả hai thứ tự enable, Map disable riêng, GUI early return khi Model null, slot sau WIN/restart và toàn bộ regression gameplay trước đó.

## Follow-up: click booster và scene đang mở

Screenshot cho thấy footer khóa placeholder chồng lên booster. Nút Add Slot còn phụ thuộc field `Slot Surfaces` mới: scene đã mở trước khi thêm field có thể giữ references rỗng dù YAML trên đĩa đã được cập nhật. Đã quan sát hai test đỏ: click vị trí Add Slot không tạo slot và scene thiếu field không thể dùng action.

Sửa: draw/hit-test dùng chung Rect; handler nhận Event rõ ràng, xử lý raw MouseDown chưa Used, consume press đã xử lý. MouseUp không gọi lại action. Editor migration một lần khi reload/chuyển Play Mode phục hồi references từ slot seed đã author; runtime không tìm hierarchy. Bỏ ba LockSlot khỏi scene và factory; migration dọn footer cũ trong scene đang mở. Chữ nút giữ 12 pixel qua UI scale.

Verification: **143 smoke checks, 0 failures; 10 asset tests pass; diff check sạch; agent review không còn finding**. Test dùng screen click cho Add Slot/Pickup/Cancel/Blow/palette và Event được truyền vào handler cho MouseDown/MouseUp; không phải kiểm tra render trực tiếp Game View.
