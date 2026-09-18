using ColonyFlow.Core.Tweening;
using DG.Tweening;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    // Gameplay supplies time so animation follows the same pause/speed as movement.
    internal sealed class ActorAnimation
    {
        private readonly TweenScope scope;
        private Sequence sequence;
        private float elapsed;

        public ActorAnimation(GameObject owner) => scope = new TweenScope(owner);

        public bool Play(Sequence animation)
        {
            Cancel();
            animation.SetUpdate(UpdateType.Manual).SetAutoKill(false).Pause();
            sequence = scope.Track(animation);
            if (sequence != null) return true;
            animation.Kill(false);
            return false;
        }

        public void Advance(float delta)
        {
            var current = sequence;
            if (current == null || !current.IsActive() || delta <= 0) return;
            elapsed = Mathf.Min(current.Duration(), elapsed + delta);
            current.Goto(elapsed, false);
            if (!current.IsActive() || elapsed < current.Duration()) return;
            sequence = null;
            current.Kill(false);
            scope.Prune();
        }

        public void Cancel()
        {
            sequence = null;
            elapsed = 0;
            scope.CancelAll();
        }

        public void Dispose() => scope.Dispose();
    }
}
