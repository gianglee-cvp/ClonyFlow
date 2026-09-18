# Core Tween B1

Trạng thái: đã chốt hướng B1; tài liệu thiết kế, chưa cài DOTween hoặc viết production code.

## 1. Mục tiêu và bằng chứng

Yêu cầu đã chốt: core dùng chung cho nhiều game, hỗ trợ cấu hình khởi tạo và lifecycle tween. Gameplay/UI vẫn dùng API DOTween trực tiếp. Chưa xây thư viện pop/fade/animation preset.

Đã kiểm tra:
- Assets/_Core chưa có source module để kế thừa topology.
- ColonyFlow.Map.asmdef hiện chỉ tham chiếu Unity.ugui.
- Chưa tìm thấy DOTween hoặc DI framework trong source/package manifest đã đọc.
- Dùng skill .agents/skills/unity-core-module-planner/SKILL.md và references/plan-template.md.
- Tài liệu chính thức: https://dotween.demigiant.com/documentation.php

Quyết định đề xuất: module mỏng, không interface bọc DOMove/DOScale/Sequence, không manager Update riêng, không registry toàn cục, không singleton MonoBehaviour và không DI adapter khi project chưa có DI.

## 2. Cấu trúc đề xuất

Namespace đề xuất: ColonyFlow.Core.Tweening.

| File mới đề xuất | Trách nhiệm |
| --- | --- |
| Assets/_Core/Tween/Runtime/TweenSettings.cs | Cấu hình startup có validation |
| Assets/_Core/Tween/Runtime/TweenRuntime.cs | Khởi tạo DOTween và kiểm tra cấu hình lặp |
| Assets/_Core/Tween/Runtime/TweenScope.cs | Sở hữu nhóm tween của một GameObject |
| Assets/_Core/Tween/Runtime/ColonyFlow.Core.Tweening.asmdef | Assembly phụ thuộc DOTween |
| Docs/core-tween-usage.md | Quy tắc ownership và ví dụ dùng |

Tên assembly DOTween và reference cuối phải kiểm tra sau khi cài/setup phiên bản được chọn; chưa coi dependency có sẵn. Game tham chiếu core và DOTween nếu gọi API DOTween trực tiếp. Core không tham chiếu ColonyFlow.Map.

DOTween là dependency được chấp nhận cho B1; không có mục tiêu thay tween engine qua cùng interface.

## 3. Contract public đề xuất

Các chữ ký sau là thiết kế, chưa phải API đã tồn tại.

    public sealed class TweenSettings
    {
        public int TweenersCapacity { get; set; } = 256;
        public int SequencesCapacity { get; set; } = 64;
        public bool UseSafeMode { get; set; } = true;
        public LogBehaviour LogBehaviour { get; set; } = LogBehaviour.ErrorsOnly;
    }

    public static class TweenRuntime
    {
        public static void Initialize(TweenSettings settings);
    }

    public sealed class TweenScope : IDisposable
    {
        public TweenScope(GameObject owner);
        public GameObject Owner { get; }
        public bool IsDisposed { get; }
        public T Track<T>(T tween) where T : Tween;
        public void CancelAll();
        public void Prune();
        public void Dispose();
    }

### Startup

- App bootstrap gọi Initialize trên Unity main thread trước khi tạo tween.
- Null settings: ArgumentNullException. Capacity phải dương và tweenersCapacity >= sequencesCapacity; invalid input: ArgumentOutOfRangeException.
- Sao chép giá trị settings tại startup; thay object settings sau đó không đổi cấu hình engine.
- Initialize lần đầu cấu hình native DOTween.Init và capacity. Native recycling mặc định tắt.
- Gọi lại cùng cấu hình: không có side effect. Gọi lại khác cấu hình: InvalidOperationException, không thay settings toàn game giữa chừng.
- Nếu DOTween đã được khởi tạo ngoài module trước lần Initialize đầu: báo cấu hình lifecycle không hợp lệ, tránh nhận là đã áp dụng settings khi native Init không còn tác dụng. Cách phát hiện phải đối chiếu API phiên bản thực tế.
- Không gọi DOTween.Clear hoặc KillAll từ module khi một scene/scope đóng. Engine có lifetime toàn ứng dụng.
- Kiểm tra Play Mode khi tắt domain reload: state bootstrap phải phản ánh native initialization mới, không để cờ static cũ bỏ qua startup.

### Scope và Track

- Owner là GameObject scene object hợp lệ, chưa destroyed; null/destroyed owner: ArgumentNullException hoặc ArgumentException.
- Scope được tạo sau Initialize; chưa Initialize: InvalidOperationException.
- Track nhận native Tween/Sequence, trả đúng instance với đúng kiểu T để caller tiếp tục chain API.
- Null tween: ArgumentNullException; tween đã kill: ArgumentException.
- Scope đã Dispose hoặc owner đã destroyed: ObjectDisposedException; không tự kill input tween khi Track thất bại vì ownership chưa chuyển.
- Track cùng tween trong cùng scope: idempotent, chỉ lưu một lần.
- Track chỉ nhận tween độc lập hoặc Sequence ngoài cùng. Với Sequence, caller tạo các tween con rồi Track Sequence. Không Track tween con hoặc chuyển tween đã Track vào Sequence sau đó.
- Một tween thuộc một scope; caller không đăng ký lại vào scope khác. B1 không thêm registry toàn cục để phát hiện vi phạm này.
- Track gắn SetLink(owner, KillOnDestroy) cho tween ngoài cùng. Caller không đổi SetLink sau đó.
- Track đặt SetRecyclable(false); caller không bật lại recycling cho tween đã Track. Đây là hạn chế B1 để reference tween đã kill không được tái sử dụng thành tween của owner khác.
- Track giữ nguyên ID, target, easing, timeScale, update mode, autoKill, callback và trạng thái paused/playing của caller.
- Không gắn/ghi đè OnKill hoặc OnComplete để quản lý collection; tránh tranh quyền callback của caller.
- Không tự chạy hoặc restart tween khi Track.

### CancelAll, Prune và Dispose

- CancelAll kill các tween scope đang sở hữu với complete=false, sau đó bỏ references. Không gọi Complete trước kill.
- Scope vẫn dùng lại được sau CancelAll; tween mới có thể Track.
- Dispose đánh dấu disposed trước khi hủy tween; idempotent. Track từ callback trong lúc Dispose phải bị từ chối.
- CancelAll dùng snapshot và bỏ collection trước khi kill để OnKill callback không gây lỗi khi thay đổi collection. Tween caller tạo và Track trong OnKill thuộc batch mới, không bị batch cũ hủy.
- OnKill của caller có thể chạy do kill; OnComplete không được dùng để báo gameplay hoàn thành khi cancel. Kiểm chứng với phiên bản DOTween được chọn.
- Prune bỏ tween inactive; tự prune trước Track. Scope owner sống lâu có thể gọi Prune sau các batch animation để giải phóng reference đã hoàn thành.
- Không cần polling Update riêng. Native SetLink xử lý destroy owner; caller vẫn Dispose scope khi kết thúc ownership để bỏ references.
- Khi owner bị disable: B1 không tự hủy. Caller gọi CancelAll trong OnDisable nếu muốn animation không chạy lại khi tái sử dụng; owner bật lại dùng scope cũ hoặc scope mới tùy lifecycle consumer.
- Pause/resume theo scope chưa nằm trong B1. Caller dùng native API và quyết định policy theo game.

## 4. Invariants và ownership

| Thành phần | Creator/owner | Kết thúc |
| --- | --- | --- |
| DOTween engine | App bootstrap | Ứng dụng kết thúc; scope không quản lý engine toàn cục |
| TweenScope | Component hoặc controller màn hình | Consumer Dispose |
| Native tween | Caller tạo; chuyển lifecycle cho scope sau Track thành công | Completion theo autoKill, CancelAll, Dispose hoặc destroy owner |
| Transform/Canvas/material | Gameplay/UI sở hữu | Core không destroy hoặc reset |
| Gameplay callback | Caller | Core không thay luật count/pickup/win |

Core không tự đặt scale về Vector3.one và không tự restore transform khi cancel. Animation scale sau này phải lấy scale gốc của đối tượng; cancel/restore là quyết định consumer, chưa xây effect helper trong B1.

## 5. Flow dùng đề xuất

    App startup:
      TweenRuntime.Initialize(settings)

    Consumer created/enabled:
      scope = new TweenScope(gameObject)
      scope.Track(transform.DOMove(destination, duration))
      scope.Track(DOTween.Sequence().Append(...).Join(...))

    Consumer disabled for reuse:
      scope.CancelAll()
      consumer restores its own visual baseline if required

    Consumer destroyed / screen closed:
      scope.Dispose()

UI cần chạy khi pause: caller cấu hình SetUpdate(..., true) trên tween độc lập hoặc Sequence ngoài cùng. Gameplay dùng time policy của game. AntGameplay hiện có Paused/SpeedMultiplier và tự Advance; Time.timeScale không tự đồng bộ các biến này với DOTween. Việc chuyển animation kiến sang tween cần plan tích hợp riêng, không nằm trong module B1.

Không để native tween và Advance cùng ghi một transform/property mỗi frame.

## 6. Thứ tự công việc

### T1 — Dependency và assembly

Files: dependency DOTween theo bản được chọn, asmdef đề xuất ở trên.
- Cài/setup DOTween khi người dùng yêu cầu implementation; chưa cài trong lượt lập plan.
- Kiểm tra native assembly, API SetLink, IsActive, recycling, Init/capacity và domain reload.
- Acceptance: core và game compile; không tạo vòng reference.

### T2 — Startup

Files: TweenSettings.cs, TweenRuntime.cs.
- Validation, snapshot settings, init một lần và duplicate policy.
- Acceptance: cùng cấu hình lặp không đổi engine; cấu hình khác hoặc init quá muộn báo lỗi rõ.
- Verification: EditMode validation; PlayMode startup và domain reload off.

### T3 — Ownership

File: TweenScope.cs.
- Collection deduplicate bằng reference, snapshot cancellation, prune, dispose và native owner link.
- Acceptance: chỉ hủy tween scope sở hữu; destroy owner hủy outer Sequence; callback caller không bị thay; repeated Dispose an toàn.
- Verification: PlayMode tween hoàn thành/cancel/destroy, inactive owner, callback reentrancy và hai scope độc lập.

### T4 — Hướng dẫn và consumer nhỏ

File: Docs/core-tween-usage.md; consumer thử nghiệm tạm hoặc test fixture.
- Ví dụ tween độc lập, outer Sequence, disable/reuse, scaled transform và UI unscaled time.
- Acceptance: không dùng KillAll/Clear để cleanup scene; scale gốc (2,1.5,2.5) không bị module reset.
- Không chuyển hệ thống di chuyển/dispatch kiến trong task này.

## 7. Kiểm tra dự kiến

Chưa thực hiện bất kỳ kiểm tra module nào; DOTween chưa được cài.

| Tình huống | Kết quả cần đạt |
| --- | --- |
| Scope A Dispose khi B đang chạy | Chỉ A bị kill |
| Destroy owner chứa outer Sequence | Sequence dừng, không callback thao tác object đã destroyed |
| Track lại cùng tween | Không duplicate lifecycle |
| Kill tween rồi tạo tween khác | Scope cũ không kill tween mới; owned tween recycling tắt |
| Caller đặt OnKill/OnComplete | Module không ghi đè; cancellation không giả completion |
| OnKill gọi Track/Dispose/CancelAll | Không sửa collection đang enumerate, không double-dispose |
| Owner disabled rồi reused | Consumer CancelAll làm sạch; không animation cũ chạy tiếp |
| Tween đã paused trước Track | Giữ paused |
| Scale prefab khác 1 | Module không đổi scale; consumer tự chọn scale animation baseline |
| UI tween unscaled, gameplay paused | UI hoạt động đúng update mode; pause game tự tích hợp |
| Play nhiều lần, domain reload off | Startup hợp lệ, không scope/static reference sót |
| Nested Sequence | Consumer chỉ Track outermost Sequence |

## 8. Phần chưa chốt ở mức triển khai

- Phiên bản và cách cài DOTween; kiểm tra assembly/API sau khi dependency có thật.
- Bootstrap thực tế của các game sử dụng module. Hiện chưa có framework/DI để gắn vào.
- Có cần tự cancel khi owner disable qua component binding hay không: đề xuất B1 dùng explicit CancelAll trong OnDisable trước, chưa thêm binding component.

Các mặc định capacity, safe mode và recycling là đề xuất thiết kế, không phải yêu cầu được suy ra từ module Audio/Pool.