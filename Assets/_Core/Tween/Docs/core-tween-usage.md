# Core Tween B1

Dependency: DOTween 1.3.030 bản miễn phí, tải từ https://dotween.demigiant.com/download.php, giữ license trong Assets/Plugins/Demigiant/DOTween.
Namespace: ColonyFlow.Core.Tweening. Assembly: ColonyFlow.Core.Tweening; DOTween.dll được auto-reference theo plugin metadata.

## Startup

Gọi trên Unity main thread từ bootstrap ứng dụng, trước khi bất kỳ consumer nào tạo tween:

    TweenRuntime.Initialize(new TweenSettings());

Mặc định: capacity 256 tweeners/64 sequences, safe mode bật, chỉ log lỗi; recycling tắt. Settings được snapshot. Cùng cấu hình init lặp an toàn; khác cấu hình hoặc DOTween đã được init bên ngoài sẽ báo lỗi.

## Consumer

    private TweenScope scope;

    void OnEnable()
    {
        scope = new TweenScope(gameObject);
    }

    void Animate(Vector3 destination)
    {
        scope.Track(transform.DOMove(destination, .3f));
    }

    void OnDisable()
    {
        scope?.Dispose();
        scope = null;
    }

Thêm using DG.Tweening và ColonyFlow.Core.Tweening. Nếu consumer dùng asmdef, tham chiếu ColonyFlow.Core.Tweening; plugin DOTween.dll auto-referenced.

- Với Sequence: dựng chuỗi rồi Track Sequence ngoài cùng; không Track tween con. Không thêm tween đã Track vào Sequence sau đó.
- Track giữ trạng thái paused/playing và callback; trả native instance để chain. Không đổi link/recycling sau Track hoặc Track cùng tween ở scope khác.
- CancelAll kill complete=false, scope còn dùng lại được. Dispose idempotent, không dùng lại được.
- Kill có thể chạy callback OnKill của caller. Core không chiếm OnKill/OnComplete.
- Destroy owner được native SetLink xử lý. Consumer vẫn Dispose để bỏ references. Disable không tự cancel: consumer chủ động Dispose hoặc CancelAll.
- Prune bỏ reference tween đã kill/complete; tự chạy trước Track. Scope sống lâu gọi Prune sau batch khi cần.
- UI chạy khi pause: cấu hình SetUpdate(UpdateType.Normal, true) trên tween độc lập/outer Sequence.
- Core không tự phục hồi position/scale. Consumer lưu scale gốc nếu animation cần scale; không giả định Vector3.one.
- CancelAll dùng snapshot: tween tạo mới từ OnKill thuộc batch mới. Dispose đánh dấu trước callback nên Track trong Dispose bị từ chối.
- Recycling của tween được Track luôn tắt để reference đã kill không trỏ sang tween được tái sử dụng.

## Tích hợp gameplay

Module chưa thay logic kiến/Box. AntGameplay có Paused/SpeedMultiplier riêng; chúng không tự đồng bộ với Time.timeScale hay tween engine.
Không để Advance và tween cùng ghi một transform/property. Việc đưa pickup/jump vào tween cần tích hợp riêng.

## Scope

Owner phải là GameObject trong scene, không phải prefab asset. API sử dụng trên Unity main thread. Module không KillAll/Clear DOTween engine và không hủy object owner.

## Source

Plan tại Docs/2026-09-18-core-tween-b1.md trong cùng folder module.