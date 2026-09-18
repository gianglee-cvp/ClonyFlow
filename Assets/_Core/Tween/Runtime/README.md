# Tween Runtime

`TweenRuntime.Initialize(TweenSettings)` khởi tạo engine trong Play Mode, trả bool. `TweenScope.Track` giữ standalone tween/outermost sequence, link KillOnDestroy, tắt recycling. Track trả null nếu owner/runtime/tween không hợp lệ.

CancelAll cho phép dùng lại scope và không phát OnComplete; Dispose kết thúc scope. Consumer cancel khi recycle/disable, dispose khi object thật sự destroy. Không gọi KillAll/Clear để dọn một actor. Xem [usage](../Docs/core-tween-usage.md).
