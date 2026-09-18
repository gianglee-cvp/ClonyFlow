# Quy tắc phát triển Unity gameplay

## Quy trình

Chốt yêu cầu → plan theo dependency và acceptance → review thiết kế → test tái hiện → build từng phần → test PlayMode → agent review độc lập → sửa vấn đề → commit đúng phần đã kiểm tra. Không coi code AI là đúng chỉ vì biên dịch. Phân biệt lỗi hiện tại, rủi ro integration và tối ưu cần profiling.

## Naming và trách nhiệm

PascalCase cho type/public API; camelCase cho biến/field như source hiện tại. Tên hành động bắt đầu bằng động từ; predicate diễn tả điều kiện, ví dụ HasBudgetSupply. Giữ public contracts đã dùng, không rename toàn project chỉ để theo quy tắc. Biến có đơn vị rõ khi cần phân biệt world/local/duration/count. Giá trị có ý nghĩa gameplay nên là named constant hoặc serialized configuration. Tránh tên temp/data/manager mơ hồ khi có nhiều loại trong cùng scope.

Mỗi hàm thực hiện một nhiệm vụ quan sát được. Coordinator được gọi các bước có tên rõ; không chia nhỏ máy móc thành wrapper không thêm nghĩa. Boolean flags phải biểu đạt ý nghĩa, không dùng true/false cho nhiều outcome khác nhau. Invalid input trả bool/null/no-op theo contract; project không explicit throw/try/catch.

## Unity lifecycle

Không lookup component trong loop mỗi frame; cache root component một lần khi tạo/bind. Không dùng GetComponentInChildren/Parent hoặc tìm hierarchy bằng tên. Giữ prefab references tường minh. Pool recycle reset dữ liệu lease và hủy tween; dispose khi destroy. Không recycle source khi trip còn tham chiếu; snapshot trước khi object visual được reuse. Gameplay pause/x2 cấp thời gian cho animation của mình.

## Folder

Core có module/runtime/docs riêng và MD ở từng folder; dependency Core không trỏ tới gameplay. Gameplay Map/Data giữ schema, Map/Ant giữ actor/navigation, Map/Booster giữ booster và UI selection. Tách theo trách nhiệm thực tế, không tạo folder cho từng class. UI chỉ nhận input/hiển thị và gọi API gameplay; không ghi trực tiếp collection nội bộ.

## Review và commit

Review ownership, state transitions, reservation, budget accounting, reset, repeated calls, invalid inputs, scene teardown và runtime layout. Test outcome thật: số cell, task, budget, instance identity và pause; không chỉ match source text. Commit chỉ paths/diff thuộc phần đã review; giữ thay đổi cá nhân ngoài scope. Không tối ưu speculative; route-cost allocations chỉ refactor khi profiling cho thấy chi phí.
