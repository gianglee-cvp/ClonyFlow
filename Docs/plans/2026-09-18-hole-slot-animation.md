# Hole and slot animation implementation plan

**Goal:** Kiến rẽ chéo gần hole rồi nhảy vào và thu nhỏ; box nhảy lên slot trước khi thả kiến.

**Approved design:** User approved on 2026-09-18. Inspector controls proximity, jump height and duration. Actors own outer sequences through Core TweenScope. Gameplay advances paused sequences explicitly, using its existing pause and speed policy. Restart, disable and destroy cancel animations.

**Architecture:** ActorAnimation encapsulates scoped sequence playback. BoxActor owns landing state; AntActor owns jump completion and scale reset. AntGameplay trims the last bottom-edge return segment at the hole proximity radius before appending the diagonal approach.

**Tech stack:** Unity 6, DOTween, ColonyFlow.Core.Tweening.

1. Extend Tools/Unity/LevelSmokeChecks.cs to run in Play Mode and assert box landing gates dispatch, ant jump shrinks, pause freezes animation and cancellation clears tweens. Run against old code to observe failures.
2. Add ActorAnimation.cs and Core assembly reference; integrate box DOJump and ant DOJump/DOScale sequences. Advance only owned paused sequences with gameplay delta.
3. Add configurable hole proximity and trim return route on bottom edge. Verify diagonal departure distance and no path cutting through the card.
4. Run authored-level smoke simulations, static asset checks, compiler and diff checks. Update map and Core usage docs.
