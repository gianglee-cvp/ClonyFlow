# PHÂN TÍCH GAME ART COLONY

> Tài liệu phân tích Gameplay & Technical Design, được biên tập từ nội dung cung cấp. Các quy tắc đã chốt được ưu tiên khi phần mô tả trước đó chưa cụ thể. Các cấu trúc class và đoạn mã dưới đây là mô tả thiết kế, không phải mã triển khai hoàn chỉnh.

## 1. Tổng quan game

| Hạng mục | Mô tả |
| --- | --- |
| Thể loại | Game 3D, góc nhìn top-down. |
| Màn hình | Màn hình dọc; độ phân giải/tỉ lệ tham khảo: 1080 × 1920, 720 × 1600 và 3:4. |
| Map | Map dạng pixel, bố trí trong một hình chữ nhật. |
| Màu sắc | Mỗi màu có một index để xác định thứ tự/loại màu. |
| Tương tác chính | Chọn Box từ hàng đợi, đưa lên Slot để kiến xử lý các Cell cùng màu có thể tiếp cận. |
| Mục tiêu | Xử lý toàn bộ Cell màu trên map. |

Người chơi quyết định thứ tự đưa Box lên Slot. Số Slot giới hạn và khả năng tiếp cận Cell khiến việc chọn màu, thứ tự hàng đợi và thời điểm dùng Booster ảnh hưởng trực tiếp đến diễn biến màn chơi.

## 2. Thuật ngữ

| Thuật ngữ | Ý nghĩa |
| --- | --- |
| `Cell` | Một ô trên map, chứa màu, tọa độ và dữ liệu tìm điểm rìa. |
| `ColorType.None` | Cell trống, có thể được xét làm lối đi nếu có weight hợp lệ. |
| `Box` | Hộp chứa kiến, có màu và số lượng kiến tương ứng. |
| `BoxQueue` | Một hàng đợi Box. |
| `Slot` | Vị trí chứa Box đang tham gia xử lý map. |
| `Ant` | Kiến được gán một Cell mục tiêu để lấy. |
| `targetCell` | Cell màu mà Ant cần lấy. |
| `borderCell` | Cell ở rìa map mà Ant cần di chuyển tới trước khi lấy mục tiêu. |
| `weight` | Giá trị dùng để chọn hướng dẫn về rìa map; `-1` biểu thị chưa có hướng tiếp cận hợp lệ. |
| `previousCell` | Cell trước đó trong chuỗi dẫn về rìa map. |
| Reserve | Giữ một Cell mục tiêu cho một Ant, tránh phân công trùng. |
| Deadlock | Trạng thái gameplay không thể tự tiến triển hoặc tự giải phóng Slot theo điều kiện thua. |

## 3. Box và hàng đợi

### 3.1. Quy tắc chung

- Mỗi Box có một màu xác định và số lượng kiến tương ứng.
- Box được sắp xếp trong nhiều hàng đợi.
- Theo thao tác thông thường, người chơi chỉ pick Box ở đầu mỗi hàng đợi.
- Khi một hàng đợi hết Box, hàng đợi đó biến mất.
- Các hàng đợi còn lại tự căn chỉnh để giữ bố cục đối xứng.
- Sau khi lấy Box, hệ thống cập nhật Box đầu mới và hiển thị màu nếu đó là Hidden Box.

### 3.2. Các loại Box

| Loại | Đặc điểm | Điều kiện pick |
| --- | --- | --- |
| Normal | Hiển thị màu bình thường. | Ở đầu hàng đợi và còn Slot trống; Booster Pickup cho phép chọn ở vị trí khác. |
| Hidden | Bị che màu cho đến khi lên đầu hàng đợi. | Được reveal khi lên đầu hàng đợi, sau đó xét thao tác pick thông thường. Không áp dụng Booster Pickup. |
| Stick | Cụm 2–3 Box liên kết bằng `stickID`, xuất hiện ở các màn sau. | Toàn bộ cụm thỏa điều kiện vị trí và có đủ Slot trống. Không áp dụng Booster Pickup. |

### 3.3. Stick Box

Một cụm Stick Box được xét và di chuyển như một đơn vị. Trước khi thay đổi Queue hoặc Slot, hệ thống kiểm tra:

1. Tìm toàn bộ Box có cùng `stickID`.
2. Số Slot trống đủ cho tất cả Box trong cụm.
3. Mỗi Box ở đầu hàng đợi hoặc nằm ngay dưới Box thuộc cùng cụm, theo cấu hình level.
4. Chỉ khi toàn bộ cụm hợp lệ mới cho phép pick.

Khi hợp lệ, cả cụm đi lên các Slot trống từ trái sang phải. Thứ tự Box trong cụm theo thứ tự đọc hàng đợi từ trái sang phải; trong cùng một hàng đợi, đọc từ trên xuống dưới.

## 4. Slot và cơ chế spawn

### 4.1. Quy tắc Slot

- Mặc định có **5 Slot**.
- Booster Add Slot mở thêm **1 Slot**, tăng từ 5 lên 6.
- Box hợp lệ nhảy vào Slot trống đầu tiên theo thứ tự trái sang phải.
- Mỗi Slot theo dõi interval và timer spawn của Box đang chứa.
- Box hết kiến được loại khỏi Slot.
- Vị trí và index Slot cố định. Slot trống được tái sử dụng từ trái sang phải.

### 4.2. Chọn Box khi có nhiều Box cùng màu

Với một `targetCell` đã chọn:

1. Tính `borderCell` bằng `CreatePath(targetCell)`.
2. Xét các Box cùng màu trên Slot có thể spawn Ant tới mục tiêu.
3. Tính khoảng cách từ điểm spawn của từng Box tới `borderCell` theo đường đi quanh cạnh ngoài map.
4. Chọn Box có khoảng cách ngắn nhất.
5. Nếu khoảng cách bằng nhau, ưu tiên Box có Slot index nhỏ hơn, tức nằm bên trái hơn.

Quy tắc này giúp kết quả lựa chọn Box luôn xác định.

## 5. Ant và logic lấy Cell

### 5.1. Điều kiện mục tiêu

Cell mục tiêu phải có màu phù hợp với Box và có thể tiếp cận theo dữ liệu Map. Mỗi Cell chỉ được reserve cho một Ant tại một thời điểm.

### 5.2. Vòng đời xử lý

1. Box lên Slot; hệ thống xác định các Cell cùng màu có thể lấy.
2. Chọn Cell mục tiêu và Box phù hợp.
3. Reserve mục tiêu để tránh nhiều Ant chọn cùng một Cell.
4. Đến interval spawn, Ant được lấy từ pool và gán `targetCell`.
5. `CreatePath(targetCell)` trả về `borderCell` tương ứng.
6. Ant di chuyển từ Box theo cạnh ngoài hình chữ nhật đến `borderCell`.
7. Tại điểm rìa, Ant thực hiện animation/logic lấy `targetCell`.
8. Cell đã lấy trở thành `ColorType.None`; bốn Cell kề trực tiếp cập nhật trạng thái.
9. Giải phóng reserve của mục tiêu, giảm `antCount` của Box theo flow đã mô tả.
10. Khi `antCount = 0`, Box được remove khỏi Slot. Ant được thu hồi về pool sau khi kết thúc vòng đời.

`CreatePath` chỉ xác định điểm rìa. Thiết kế không yêu cầu dựng `List<Cell>` hoặc cho Ant chạy qua toàn bộ chuỗi Cell bên trong map.

Ant sử dụng object pooling do được spawn/despawn liên tục. Tất cả Ant có cùng tốc độ cơ bản.

## 6. Map và dữ liệu Cell

### 6.1. Cấu trúc Cell

```csharp
public class Cell
{
    public ColorType colorType;
    public Cell previousCell;
    public int weight = -1;

    public int row;
    public int column;

    public Cell(int row, int column, ColorType colorType)
    {
        this.row = row;
        this.column = column;
        this.colorType = colorType;
    }
}
```

Map lưu kích thước Cell (`width`, `height`), số hàng/cột và ma trận `Cell[,]`. Dữ liệu màu ban đầu được biểu diễn bằng `ColorType[,] mapData`.

### 6.2. Khởi tạo Map

```csharp
public void InitMap(ColorType[,] mapData)
{
    CreateCells(mapData);
    InitBorderMap();
    InitCells();
}
```

| Bước | Vai trò |
| --- | --- |
| `CreateCells` | Tạo ma trận Cell; mỗi phần tử trong `mapData` xác định `colorType` của Cell tương ứng. |
| `InitBorderMap` | Khởi tạo weight cho các Cell ở rìa. |
| `InitCells` | Xét Cell bên trong và cập nhật weight/previousCell theo các hướng hợp lệ. |

### 6.3. Khởi tạo rìa map

- Lấy Cell giữa cạnh dưới làm điểm trung tâm, gán `weight = 1`.
- Nếu số cột chẵn, sử dụng hai Cell giữa và cùng gán `weight = 1`.
- Khởi tạo weight lần lượt cho cạnh dưới, hai cạnh bên và cạnh trên.
- Weight Cell rìa được tính dựa theo cạnh và index Cell.

### 6.4. Quy tắc Cell bên trong

Xét bốn hướng trực tiếp: trên, dưới, trái, phải.

- Một hướng hợp lệ khi Cell lân cận có `ColorType.None` và `weight >= 0`.
- Chỉ cần ít nhất một hướng hợp lệ để tính weight.
- Chọn Cell lân cận hợp lệ có weight nhỏ nhất.
- Gán `weight = minWeight + 1` và lưu Cell đã chọn vào `previousCell`.
- Nếu không có hướng hợp lệ, gán `weight = -1`, `previousCell = null`.

Khung duyệt được mô tả trong tài liệu nguồn:

```csharp
for (int row = 1; row < rowCount - 1; row++)
{
    for (int col = 1; col < columnCount - 1; col++)
    {
        CheckMinWeight();
    }
}
```

`CheckMinWeight()` thực hiện quy tắc chọn láng giềng hợp lệ có weight nhỏ nhất nêu trên.

### 6.5. Tìm điểm rìa bằng CreatePath

Với Cell màu mục tiêu, chọn một trong bốn Cell lân cận có thể đi qua và có weight nhỏ nhất. Từ đó lần theo `previousCell` đến Cell đầu tiên nằm ở rìa map và trả về Cell rìa đó. Nếu không tìm thấy, trả về `null`.

```csharp
public Cell CreatePath(Cell targetCell)
{
    Cell currentCell = GetMinWeightNeighbor(targetCell);

    while (currentCell != null)
    {
        if (IsBorderCell(currentCell))
        {
            return currentCell;
        }

        currentCell = currentCell.previousCell;
    }

    return null;
}

private bool IsBorderCell(Cell cell)
{
    return cell.row == 0 ||
           cell.row == rowCount - 1 ||
           cell.column == 0 ||
           cell.column == columnCount - 1;
}
```

`GetMinWeightNeighbor(targetCell)` chỉ xét bốn Cell lân cận có thể đi qua và có `weight >= 0`, rồi chọn Cell có weight nhỏ nhất.

### 6.6. Cập nhật sau khi lấy Cell

Đoạn mã mô tả flow:

```csharp
CollectCell(Cell cell)
{
    cell.colorType = ColorType.None;

    ChangeCellState(cell.row + 1, cell.column);
    ChangeCellState(cell.row - 1, cell.column);
    ChangeCellState(cell.row, cell.column + 1);
    ChangeCellState(cell.row, cell.column - 1);
}

ChangeCellState(int row, int column)
{
    // Đổi trạng thái Cell kề sau khi Cell mục tiêu bị lấy.
    // Cập nhật dữ liệu cần thiết của Cell theo trạng thái mới.
}
```

Map đổi trạng thái trực tiếp của bốn Cell kề; không thêm một bước riêng mang tên “kiểm tra 4 Cell”. Mỗi Cell kề cập nhật dữ liệu theo trạng thái mới, bao gồm weight/previousCell nếu thay đổi làm Cell trở nên có thể tiếp cận. Việc đổi trạng thái Cell kề không có nghĩa tự động xóa màu của các Cell đó.

## 7. Booster

| Booster | Tên kỹ thuật | Chức năng và giới hạn |
| --- | --- | --- |
| Thêm Slot | `AddSlot` | Mở thêm một Slot, từ 5 lên 6. |
| Pickup | `Pickup` | Pick một Box bất kỳ trong hàng đợi; không áp dụng cho Hidden Box và Stick Box. |
| Thổi | `RemoveColor` | Chọn màu, remove toàn bộ Cell màu đó và toàn bộ Box cùng màu trong Slot/Queue. |

### 7.1. Flow Booster Thổi

1. Người chơi chọn `ColorType`.
2. Remove toàn bộ Cell có màu đã chọn trên map.
3. Đưa `antCount` của Box cùng màu trong Slot và Queue về 0.
4. Remove các Box đó ngay trong cùng một bước refresh.
5. Đồng bộ dữ liệu map và trạng thái Ant/Box liên quan.
6. Refresh Queue, Slot, Hidden Box và Stick Box bị ảnh hưởng.
7. Kiểm tra lại Win/Lose.

Booster không tự động được sử dụng để cứu màn; quyết định sử dụng thuộc về người chơi.

## 8. Tốc độ game

- Tất cả Ant dùng chung một speed multiplier.
- Nút x2 Speed đặt multiplier bằng 2.
- Khi toàn bộ hàng đợi đã hết Box, game tự chuyển sang x2 Speed.
- Spawn interval và animation/tween liên quan dùng cùng quy ước tốc độ để nhịp spawn và tốc độ Ant đồng bộ.

## 9. Điều kiện thắng và thua

### 9.1. Win

Win khi toàn bộ Cell màu cần xử lý trên map đã được lấy hoặc remove. Không còn Cell màu cần xử lý trên map.

### 9.2. Lose

Slot đầy chưa đủ để kết luận thua. Lose xảy ra khi Slot đầy và đồng thời:

1. Không Box nào trên Slot có thể spawn Ant tới Cell hợp lệ.
2. Không Ant active nào có thể hoàn tất lấy Cell để làm thay đổi map, mở thêm đường.
3. Không còn thay đổi trạng thái nào có thể tự giải phóng Slot.

Nếu người chơi còn Booster có thể cứu màn, game có thể hiển thị Popup gợi ý. Game không tự dùng Booster. Nếu người chơi bỏ qua/đóng Popup và trạng thái vẫn deadlock thì chuyển Lose.

## 10. Kiến trúc gameplay

```text
GameManager
LevelManager
└── Level
    ├── Map
    ├── QueueManager
    ├── SlotManager
    ├── AntManager
    └── BoosterManager

UIManager
PopupManager
DataManager
```

Level giữ các hệ thống thuộc màn hiện tại. Các Manager điều phối trạng thái, dữ liệu và vòng đời trong phạm vi trách nhiệm của mình.

### 10.1. GameManager

Quản lý trạng thái tổng của game và gọi sang LevelManager/UIManager. Không trực tiếp xử lý logic Box, Ant hoặc Map.

| Dữ liệu/API | Vai trò |
| --- | --- |
| `GameState currentState` | Trạng thái hiện tại. |
| `bool IsState(GameState state)` | Kiểm tra trạng thái. |
| `void ChangeState(GameState state)` | Chuyển trạng thái. |
| `void OnWin()` | Xử lý chuyển sang trạng thái thắng. |
| `void OnLose()` | Xử lý chuyển sang trạng thái thua. |
| `void OnPause()` | Xử lý tạm dừng. |

`GameState`: `MainMenu`, `Playing`, `Win`, `Lose`, `Pause`.

### 10.2. LevelManager và Level

```text
LevelManager
- int currentLevelID
- Level currentLevel
- void LoadLevel()
- void InitLevel()
- void StartLevel()
- void UnloadLevel()
- void CheckWinLose()

Level
- Map map
- QueueManager queueManager
- SlotManager slotManager
- AntManager antManager
- BoosterManager boosterManager
- void OnInit()
- void OnStart()
- void OnDespawn()
```

LevelManager quản lý load/unload và vòng đời màn chơi. Level giữ reference đến toàn bộ hệ thống gameplay của màn hiện tại.

### 10.3. Box

Box là một đối tượng riêng, thay vì chỉ là dữ liệu nằm trực tiếp trong hàng đợi.

```text
BoxType: Normal, Hidden, Stick

Box
- ColorType colorType
- int antCount
- BoxType boxType
- int stickID
- bool IsEmpty()
- bool CanPick()
- void Reveal()
- void RemoveAnt()
```

`Reveal()` hiển thị màu Hidden Box khi lên đầu hàng đợi. Các Box cùng `stickID` thuộc một cụm Stick Box.

### 10.4. Queue System

```text
QueueManager
- List<BoxQueue> queues
- bool CanPick(Box box)
- void PickBox(Box box)
- void RemoveEmptyQueue()
- void RefreshQueueLayout()
- void RevealFrontBoxes()
- bool CanPickStickBox(Box box)

BoxQueue
- List<Box> boxes
- Box GetFrontBox()
- void RemoveBox(Box box)
- bool IsEmpty()
```

QueueManager kiểm tra quyền pick, xét cả cụm Stick Box, cập nhật đầu hàng đợi, reveal Hidden Box và căn lại bố cục khi Queue rỗng.

### 10.5. Slot System

```text
SlotManager
- const int DEFAULT_SLOT_COUNT = 5
- List<Slot> slots
- bool HasEnoughSlot(int count)
- void AddBox(Box box)
- void RemoveBox(Box box)
- Slot GetFirstEmptySlot()
- void AddBoosterSlot()

Slot
- Box currentBox
- float spawnInterval
- float spawnTimer
- bool IsEmpty()
- void SetBox(Box box)
- void Clear()
- void UpdateSpawn()
```

SlotManager quản lý sức chứa, thêm/remove Box và mở Slot bằng Booster. Slot lưu Box hiện tại cùng nhịp spawn.

### 10.6. Ant System

```text
AntManager
- Pool<Ant> antPool
- List<Ant> activeAnts
- Ant SpawnAnt(Box box, Cell targetCell)
- void CollectAnt(Ant ant)
- bool IsCellReserved(Cell cell)

Ant
- ColorType colorType
- Cell targetCell
- Cell borderCell
- float moveSpeed
- void OnInit(Box box, Cell targetCell)
- void MoveToBorder()
- void CollectCell()
- void OnDespawn()
```

AntManager quản lý pool, Ant active và reserve mục tiêu. Ant giữ màu, mục tiêu, điểm rìa và thực hiện di chuyển/lấy Cell.

### 10.7. Booster System

```text
BoosterType: AddSlot, Pickup, RemoveColor

BoosterManager
- void ApplyBooster(BoosterType type)
- void AddSlot()
- bool Pickup(Box box)
- void RemoveColor(ColorType color)
```

BoosterManager điều phối tác động của Booster tới Map, Queue, Slot và các trạng thái liên quan.

## 11. Dữ liệu level và người chơi

### 11.1. Dữ liệu level

Cấu hình level được tách khỏi logic runtime để có thể tạo/chỉnh màn bằng data.

```text
SOLevel
- List<LevelData> levels
- LevelData GetLevel(int index)

LevelData
- int levelID
- ColorType[,] mapData
- List<QueueData> queues
- int defaultSlotCount = 5
- float antSpawnInterval

QueueData
- List<BoxData> boxes

BoxData
- ColorType colorType
- int antCount
- BoxType boxType
- int stickID
```

Các kiểu dữ liệu trên mô tả dữ liệu logic của level; tài liệu nguồn chưa xác định cách biểu diễn ma trận màu trong Inspector hoặc định dạng lưu asset.

### 11.2. DataManager

| Nhóm dữ liệu | Nội dung | Hình thức được mô tả |
| --- | --- | --- |
| Tĩnh | Level, cấu hình màu, Booster, thông số gameplay. | ScriptableObject như `SOLevel`. |
| Động | Tiến trình, tiền, số lượng Booster, tùy chọn người chơi. | PlayerPrefs hoặc JSON. |

```text
PlayerData
- int currentLevel
- int gold
- int addSlotBoosterCount
- int pickupBoosterCount
- int removeColorBoosterCount
- bool soundOn
- bool vibrationOn
```

## 12. UI System

```text
UIManager
- CanvasMainMenu
- CanvasGameplay
- CanvasWin
- CanvasLose
- CanvasSetting

CanvasGameplay
- Queue UI / world layout
- Slot display
- Booster buttons
- x2 Speed button
- Level progress

PopupManager
- PopupBooster
- PopupOutOfBooster
- PopupTutorial
```

UI hiển thị trạng thái và gửi input sang gameplay system. Logic Map/Box/Ant nằm trong các hệ thống gameplay tương ứng.

## 13. Flow gameplay tổng quát

1. Load map và khởi tạo Cell/weight.
2. Khởi tạo các hàng đợi Box và hệ thống Slot.
3. Nhận thao tác chọn Box của người chơi.
4. Kiểm tra vị trí Box, điều kiện Hidden/Stick Box và số Slot trống.
5. Đưa Box hoặc cả cụm hợp lệ lên Slot.
6. Refresh hàng đợi, reveal Box đầu mới và căn lại layout nếu Queue rỗng.
7. Xác định các Cell cùng màu có thể lấy.
8. Chọn mục tiêu, chọn Box theo khoảng cách đến điểm rìa và reserve Cell.
9. Spawn Ant theo interval.
10. Ant đi theo cạnh ngoài map tới điểm rìa và lấy Cell mục tiêu.
11. Cập nhật Cell đã lấy và trạng thái bốn Cell kề trực tiếp.
12. Cập nhật số kiến; Box hết kiến được remove khỏi Slot.
13. Refresh các hệ thống và kiểm tra Win/Lose.
14. Lặp lại cho đến khi thắng hoặc rơi vào điều kiện thua.

## 14. Thứ tự refresh sau thay đổi gameplay

1. Cập nhật trạng thái Cell/Box vừa thay đổi.
2. Cập nhật weight và previousCell của Map nếu có Cell bị remove.
3. Refresh Queue: remove Box rỗng, reveal Hidden Box ở đầu, kiểm tra lại Stick Box; remove Queue rỗng và căn lại layout khi cần.
4. Refresh Slot: remove Box có `antCount = 0`; giữ vị trí/index Slot cố định, tái sử dụng ô trống từ trái sang phải.
5. Tính lại các Cell có thể lấy cho Box đang trên Slot.
6. Nếu có thể spawn, chọn target Cell và Box theo quy tắc khoảng cách tới `borderCell`.
7. Kiểm tra Win trước; nếu chưa Win thì kiểm tra deadlock để xác định Lose.

## 15. Ràng buộc nhất quán của thiết kế

- Một Cell mục tiêu không được đồng thời gán cho hai Ant.
- Stick Box phải được kiểm tra toàn cụm trước khi thay đổi Queue/Slot.
- Chọn Box cùng màu phải tuân theo khoảng cách tới điểm rìa và Slot index khi hòa.
- Weight/previousCell phải phản ánh khả năng tiếp cận sau khi map thay đổi.
- `CreatePath` chỉ trả về điểm rìa, không tạo danh sách đường đi bên trong map.
- Booster Thổi cập nhật Map, Box, Slot, Queue và trạng thái Ant liên quan trong cùng một lần xử lý nhất quán.
- x2 Speed sử dụng chung quy ước tốc độ cho Ant, spawn và animation/tween liên quan.
- Slot đầy vẫn có thể tiếp tục gameplay nếu Ant/Box còn tạo được tiến triển.

## 16. Các chi tiết chưa được xác định trong tài liệu nguồn

Các nội dung dưới đây chưa có quy tắc đầy đủ; không được xem là yêu cầu gameplay đã chốt:

| Nội dung | Phạm vi còn chưa rõ |
| --- | --- |
| Thứ tự chọn target Cell | Chưa có tiêu chí ưu tiên khi nhiều Cell cùng màu đều hợp lệ. |
| Cell mục tiêu nằm ở rìa | Chưa mô tả đầy đủ cách xử lý khi chính target Cell là Cell rìa nhưng không có láng giềng trống hợp lệ. |
| Tính weight rìa | Chưa có công thức cụ thể cho từng cạnh, góc và trường hợp bằng nhau. |
| Đồng bộ weight bên trong | Khung duyệt một lượt chưa mô tả cách xử lý Cell phụ thuộc vào láng giềng được cập nhật sau hoặc thay đổi lan truyền. |
| Thời điểm trừ kiến | Flow nguồn trừ `antCount` sau khi lấy Cell; chưa mô tả cách tính phần kiến đã spawn nhưng còn đang di chuyển để tránh spawn vượt số lượng. |
| Reserve trước spawn | Chưa xác định thời điểm hủy reserve nếu việc spawn bị gián đoạn hoặc mục tiêu bị Booster remove. |
| Ant đang hoạt động khi dùng Thổi | Có yêu cầu đồng bộ nhưng chưa xác định Ant bị hủy, thu hồi hay kết thúc animation theo cách nào. |
| Stick Box sau khi Thổi | Chưa xác định cụm còn lại được giữ liên kết hay chuyển thành Box thường khi một phần cụm bị remove. |
| Đường đi ngoài map | Chưa xác định hướng đi một chiều/hai chiều quanh chu vi và cách xử lý khi hai hướng có khoảng cách bằng nhau. |
| Deadlock khi Slot chưa đầy | Điều kiện Lose đã chốt yêu cầu Slot đầy; chưa mô tả các trạng thái hết khả năng tiến triển khi Slot vẫn còn trống. |
| Pause và x2 Speed | Chưa mô tả chi tiết pause timer/tween, khả năng tắt x2 hoặc trạng thái tốc độ khi bắt đầu màn mới. |

