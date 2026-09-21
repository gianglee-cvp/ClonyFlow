# Tiến trình phát triển Art Colony

> File theo dõi chung cho toàn project. Mỗi hệ thống hoặc tính năng được thêm thành một phần riêng; ghi nhận việc đã làm và đã kiểm chứng. Kế hoạch chưa triển khai không được đánh dấu là code hoàn thành.

## Trạng thái hiện tại

**Đang ở bước:** Đã triển khai demo Normal Box/Slot/Ant 3D theo [AntGameplayPlan](Plans/AntGameplayPlan.md). Unity batch đã kiểm tra mô phỏng end-to-end 201 gạch và render preview; chờ kiểm tra click/Play Mode trực tiếp của người dùng.

| Phần | Trạng thái | Ghi chú |
| --- | --- | --- |
| Phân tích game | Đã lưu tài liệu | [game-analysis.md](game-analysis.md). |
| Kế hoạch gameplay demo | Đã lưu, cập nhật Map | [Kế hoạch demo](plans/2026-09-17-gameplay-demo.md). |
| Map JSON và spawn | Đã chốt phạm vi, lưu kế hoạch | [Kế hoạch Map](plans/2026-09-17-map-json-spawn.md). |
| Loader/grid/spawn/camera | Đã triển khai, chờ nghiệm thu | Code tại `Assets/_Game/_GamePlay/Script/Map`. |
| Pathfinding và lấy Cell | Đã triển khai, kiểm tra mô phỏng Unity | Navigation/access weight, interior route và collect theo [AntGameplayPlan](Plans/AntGameplayPlan.md). |
| Queue, Slot, Ant, Win/Lose | Demo Normal Box đã triển khai | Ready-first, pool, carry/return/jump, deferred Win; chờ nghiệm thu tương tác trực tiếp. |
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

### 2026-09-18 — MAP-PATH / SLOT / QUEUE / ANT / LEVEL: Demo kiến 3D

- Chốt coordinate contract: row tăng theo -Z, column tăng theo +X, clockwise nhìn từ +Y; 20 × 20, spacing 1.1, tâm trong +/-10.45.
- Camera bake theo giới hạn: orthographic 42 tại (0,80,-15.12), không fit bằng scale/zoom lúc chạy.
- GridNavigation tách navigationWeight cho None và accessWeight cho target; CreatePath trả border, interior route đi qua None.
- Thêm prefab Ant/ColorBox, Ground, 5 Slot có PerimeterEntry và hố 3D; thay UI placeholder gameplay.
- Normal queue, ready-first scheduler, reserve, outgoingCount, taskId-ordered pickup, carry/return/jump và pool.
- IsBoardCleared khóa pick/spawn; IsLevelComplete chờ mọi kiến xuống hố. Có Pause/x2/Restart và deadlock cơ bản.
- Kiểm chứng: Unity 6000.3.9f1 batch biên dịch thành công; AntGameplayVerification.Run mô phỏng lấy hết 201 gạch, không đổi camera/MapRoot, count/reserve hợp lệ và restart sạch.
- Ca riêng đã đạt: empty pocket bị kín/corridor mở, target rìa, tie clockwise, Box cooldown không chặn ready Box, hai pickup cùng frame, Pause và map quá lớn không thay map hiện tại.
- Render preview: Artifacts/ant-gameplay-preview.png. Chưa xác nhận tương tác pointer/Play Mode thực tế trên thiết bị.
- Còn lại: Hidden/Stick, booster, popup, art/animation hoàn chỉnh và tỷ lệ màn hình khác 9:16.

### 2026-09-18 — SLOT / QUEUE: Sửa scene cũ và kiểm tra pick

- Scene trên đĩa đã có gameplay 3D; ảnh người dùng vẫn hiển thị UI placeholder trước đó.
- Thêm GameplaySceneGuard nâng cấp MapDemo cũ đang mở khi vào Play, giữ scene trong bộ nhớ, không reload/ghi đè công việc chưa lưu.
- Tách HandlePointer, đồng bộ physics transform trước raycast.
- Unity batch đã kiểm tra: 5 Slot ban đầu trống, không tự spawn, không pick Box sâu, click theo tọa độ màn hình pick Box đầu vào Slot 0, scene cũ được nâng cấp và bỏ UI Slot mẫu.
- Mô phỏng toàn map 201 gạch vẫn đạt.

### 2026-09-18 — ANT / MAP-PATH: Bỏ pooling, sửa điểm vào Ground

- Bỏ pool kiến; spawn Instantiate, kết thúc chuyến đi Destroy; pooling để module core sau.
- Đổi navigation sang BFS từ mọi biên None, chọn rìa gần nhất thay vì bias giữa cạnh dưới.
- Trong các đường ngắn bằng nhau chọn ít lần rẽ nhất.
- Perimeter lấy bounds Ground thật: +/-10.95; pixel biên +/-10.45 chỉ là điểm nối vào interior route.
- PerimeterEntry/HoleExit được bake trên mép Ground.
- Unity batch đạt: đường thẳng tới cạnh trên gần nhất, detour tránh vật cản, đường ít rẽ, perimeter đúng Ground và mô phỏng hoàn tất 201 gạch.

### 2026-09-18 — ANT: Chuyển chu vi sang Map Card Background

- Theo yêu cầu mới nhất, perimeter lấy bốn corner của Map Card Background và chiếu lên mặt gameplay.
- Cập nhật PerimeterEntry/HoleExit trên cạnh dưới card; Ground không còn xác định chu vi.
- Unity batch đã đạt kiểm tra bounds card và toàn bộ mô phỏng 201 gạch.

### 2026-09-18 — QUEUE: Cấu hình hàng đợi bằng JSON

- Thêm demo-box-queues.json và demo-box-queues-small.json; giữ thứ tự queue trái/phải và Box đầu/cuối.
- BoxQueueJsonLoader validate cấu trúc, giới hạn queue, palette colorId và count dương.
- AntGameplay đọc TextAsset Box Queues Json; scene generator không tự tính/sort Box từ map nữa.
- Scene cũ chưa gán JSON được gán file mặc định khi vào Play.
- Unity batch đạt: thứ tự authored, hai JSON cân bằng lượng gạch từng màu, reject màu/count sai và mô phỏng hoàn tất 202 gạch theo map mới nhất.
- Hướng dẫn: Docs/box-queue-json.md.

### 2026-09-18 — Sửa pickup từ xa và bố cục slot
- Hàng thả kiến có 4 slot, căn giữa; slot ban đầu trống.
- Target màu ở biên vẫn cần đi từ cạnh Map Card Background vào điểm đứng cạnh gạch trước khi pickup. Đường quay về đi ngược đoạn tiếp cận.
- Unity Editor verification hoàn thành 202 viên gạch; kiểm tra khoảng cách pickup cho cả biên và nội thất, 4 slot, counts, reservation, hole jump và camera/MapRoot cố định. Chưa kiểm tra trực tiếp trên thiết bị.
### 2026-09-18 — Cleanup code và package
- Xóa verification, preview helper, UI mẫu không được sử dụng, API navigation thừa và Readme/TutorialInfo URP template.
- Gỡ Multiplayer Center; giữ dependency URP/uGUI và module engine mặc định. Chi tiết ở Docs/unity-package-audit.md.
- Unity 6000.3.9f1 biên dịch và tạo layout thành công trong project cô lập (exit 0). Không thêm bộ test thay thế.
### 2026-09-18 — Tuyến ngắn nhất từ slot tới gạch
- Tính cả đoạn đi quanh card và đoạn vào grid bằng Dijkstra theo slot; chọn Box theo tổng tuyến.
- Xác minh tuyến ngang từ cạnh trái trong trường hợp target gần mép trên của grid; mô phỏng demo hoàn thành hết gạch và trip (Unity exit 0).
- Probe chỉ chạy trong project cô lập và đã xóa source sau kiểm tra.
### 2026-09-18 — MapCard ngoài Canvas
- Tách card thành GameplayRoot/MapCard với Shadow/Frame/Surface SpriteRenderer; AntGameplay lấy bounds từ reference Surface.
- Tuyến ngoài vẫn bám chu vi hình chữ nhật của card; scale card độc lập trước khi khởi tạo/restart level.
- Unity probe xác nhận ngoài Canvas, bounds đổi theo scale và hoàn thành gameplay; exit 0. Đã xóa source probe tạm.
### 2026-09-18 — Bám rìa card khi scale
- Khi tổng độ dài hòa, chọn route ít ô grid hơn, tránh rời card sớm để đi dọc biên grid.
- CardBounds dùng localBounds + transform của Surface. Advance cập nhật chu vi, entry/hole exit và waypoint trên cạnh card của kiến đang chạy khi bounds đổi.
- Probe scale card sau khi kiến spawn xác nhận entry cập nhật và gameplay hoàn thành, Unity exit 0. Source probe đã xóa.
### 2026-09-18 — Chặn cắt vùng ngoài để men grid
- Bounds đúng card; lỗi thật là Dijkstra cho phép cắt vùng None từ cạnh dưới rồi men biên grid.
- Với lối thẳng khả dụng, kiến phải bám perimeter card đến vị trí cùng hàng/cột rồi rẽ vào; so sánh tổng đường trong tập lối hợp lệ.
- Diagnostic MapDemo đo entry cạnh trái x=-21.01 thay cho entry cạnh dưới; các tuyến mẫu và gameplay hoàn thành, Unity exit 0. Xóa source probe sau kiểm tra.
### 2026-09-18 — Box spawn và destroy
- Mỗi Slot có AntSpawnPoint ở mép trên Box (+Z), AntGameplay dùng điểm này để spawn và tính đường tới perimeter.
- Khi Allocate làm AntCount về 0, Box visual được Destroy ngay trong Play. Active ant vẫn giữ task; pickup/cancel không dereference Box đã bị destroy.
- Scene đã regenerate với các AntSpawnPoint mới; Unity compile và Apply layout thành công.
### 2026-09-18 — Hoàn thiện góc nhìn nghiêng
- Camera fixed 78°, bố cục bake qua ray–plane XZ; card 3D Lit nhận bóng thay SpriteRenderer và ground nhỏ.
- Mesh bo cạnh, URP Lit, ánh sáng ấm và bóng mềm; shadow distance phù hợp vị trí camera. Tắt SRP Batcher sau A/B render xác minh lỗi màu tĩnh trong project.
- Đã đối chiếu GUID và chuyển scene/prefab/mesh/material đã kiểm chứng về project chính; giữ mapJson và boxQueuesJson.
- Render request URP tạo preview 1080x1920 với màu và count UI đầy đủ, lưu Artifacts/angled-gameplay-preview.png.
- Mô phỏng Editor xác nhận 4 slot, hủy/release spent Box ngay, hoàn thành toàn bộ gạch/trip, camera/root giữ nguyên. Chưa chạy thử thiết bị thật.
- Đã xóa script preview tạm trong bản cô lập, không thêm bộ test vào source game.
### 2026-09-18 — Ô liền và card bo tròn
- Kích thước cell mặc định bằng khoảng cách tâm, bỏ khe 0.1 units; giữ nguyên grid coordinates và dữ liệu level.
- Mesh RoundedCube cập nhật cong mượt bốn góc, normal liên tục; MapCell tăng smoothness lên 0.48, metallic 0.
- Xác minh kích thước gapless và render 1080x1920 thành công. Mesh/material copy về giữ nguyên GUID; không cần thay bố cục scene.
- Preview lưu Artifacts/tile-rounded-preview.png; source preview tạm đã xóa.
### 2026-09-18 — Chuyển gameplay Unlit
- Đổi MapCell và material Gameplay sang URP Unlit, tắt cast/receive shadow trong scene/prefab và generator.
- Giữ geometry bo góc, ô liền nhau, camera/layout/navigation. Loại ảnh hưởng Light và highlight Lit.
- Render 1080x1920 xác minh frame intensity 0 và 10 giống từng pixel; ảnh không còn vệt đen do shading/đổ bóng.
- Preview Artifacts/unlit-gameplay-preview.png; xóa source kiểm tra tạm.
- 2026-09-18: Áp dụng Lit mềm cho cell/Box/kiến (metallic 0, smoothness 0.35, không cast/receive shadow), card/hố Unlit; tắt PC SSAO, light 0.85, ambient trung tính. Đáy mesh cell cách card 0.2 unit; gạch pickup chuyển từ vị trí visual sang vị trí mang. Unity 6000.3.9f1 biên dịch và bake scene thành công (exit 0), asset đã đưa về project chính sau kiểm tra GUID. Hình ảnh và gameplay người dùng tự kiểm chứng.
- 2026-09-18: Camera Orthographic 60°, size 9; bake lại card/slot/hố theo viewport. Đổi đơn vị world 9/42 trong JSON map, prefab, collider, queue/spawn/jump và antSpeed; giữ MapRoot scale 1, 4 slot, palette/count. Giữ cell clearance 0.2. Unity biên dịch/bake exit 0; JSON parse hợp lệ; GUID asset giữ nguyên.
- 2026-09-18 (đính chính): Khôi phục kích thước map trước thay đổi 9/42. Demo firstCellPosition=(-10.45,0,10.45), spacing=1.1; star firstCellPosition=(-9.9,0,9.9), spacing=1.65, cellScale=(1.55,0.55,1.55). Camera giữ 60°, size 9, MapRoot scale 1. Card bao toàn bộ grid, slot/hố/queue nằm dưới card. Kích thước Box/kiến đã chỉnh cho camera nhỏ được giữ nguyên. Camera dọc này chỉ thấy rộng 10.125 world unit, nên map cũ rộng khoảng 22 unit bị cắt; không fit bằng scale hoặc zoom runtime. Unity compile/bake exit 0.
- 2026-09-18: Hoàn tất layout map theo chiều rộng card/số cột JSON. Grid và navigation cùng kích thước, cell sát nhau; card trở về vùng phía trên, chiều sâu bake theo số hàng. Unity kiểm chứng 20 và 13 cột cùng rộng 8.30375 unit, clearance 0.2 và camera/root fixed; exit 0. Scene đưa về project chính giữ GUID, script kiểm chứng tạm xóa.
- 2026-09-18: MapView dùng RoundedMapCell.prefab mới trong Prefabs, visual lấy từ rounded_cube.glb người dùng thêm. Editor chuẩn hóa bounds model về 1×1×1, sau đó MapView áp kích thước grid theo card. Giữ material MapCell Lit mềm, không cast/receive shadow, clearance 0.2. MapCell.prefab cũ giữ nguyên.
- Hole/slot/queue được bake theo mép dưới card chiếu lên màn hình: hố thấp hơn 4% màn hình, slot 10.5%, queue 17%. Vùng pointer dùng collider queue thay giới hạn cứng 0.11–0.32.
- Unity kiểm chứng pass: cell GLB khớp kích thước grid; hole dưới card; 3 hàng queue trên footer; khởi tạo gameplay/pick queue và chạy kiến 10 giây không lỗi. Kiểm tra headless, hình ảnh người dùng tự đối chiếu. Helper kiểm chứng đã xóa.
- 2026-09-18: Camera X=45°, size=9. Card cố định trong viewport y≈0.5..0.862, không tăng chiều sâu theo số hàng. Grid fit cả chiều rộng và chiều sâu card theo rows/columns. CellHeightMultiplier=2 (độ dày Y gấp đôi tỷ lệ gốc), clearance giữ 0.2. Dành thêm chiều sâu cho phần cell cao chiếu lên màn hình và dịch grid xuống tương ứng. Kiểm chứng demo/star: raised cell nằm trong card; toàn card nằm nửa trên, cao dưới nửa màn hình; Unity exit 0. Helper kiểm chứng xóa.
- 2026-09-18: Triển khai reference layout: 4 slot/Box rộng 13%W, khe slot 2.25%W, bù chiều sâu dưới camera 45°; queue bước cột 15.5%W, bước hàng 7.3%H, ba hàng gần footer; header lên cao, footer bar 4.5%H và nút khóa lớn. Đồng bộ queue spacing/collider/spawn, count lớn và canvas quay 45°. Kiểm tra ratio/queue-footer/spawn và kiến 15 giây pass. Preview 1080×1920 tại Artifacts/reference-layout-preview.png; cần người dùng đối chiếu trực quan. Helper tạm xóa, GUID giữ nguyên.
- 2026-09-18: Sửa count Label: canvas hướng thẳng theo camera, tâm chiếu trùng tâm Face, đặt mặt phẳng nhãn trước toàn bộ bounds Box để phần dưới chữ không bị mesh che. Cỡ chữ 90 thay 100. Runtime Initialize căn nhãn, kể cả canvas inactive; prefab editor bake cùng cách tính. Slot/Box giữ chiều rộng bằng nhau 13%W. Kiểm chứng hướng canvas và depth của toàn mesh, PickQueue/kiến 5 giây pass; Unity exit 0. Cần Apply Fixed Portrait Layout trong Editor đang mở để cập nhật kích thước slot và footer trên scene cũ.
- 2026-09-18: Ant.prefab root scale=2×, generator giữ scale prefab hiện có khi Apply Fixed Portrait Layout thay vì ghi về 1. Pickup không đổi root scale; carried brick lấy world size từ CellScale×MapRoot.lossyScale và bù scale parent để giữ kích thước viên gạch. Unity kiểm chứng root 2x sau bake, custom scale (2,1.5,2.5) không đổi qua pickup/carry, block giữ world size; exit 0. Helper tạm xóa, prefab GUID giữ nguyên.