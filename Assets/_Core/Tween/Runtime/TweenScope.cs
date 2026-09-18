using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ColonyFlow.Core.Tweening
{
    /// <summary>Owns standalone tweens and outermost sequences. Use on the Unity main thread.</summary>
    public sealed class TweenScope : IDisposable
    {
        private readonly List<Tween> tweens = new List<Tween>();
        private bool disposed;
        public GameObject Owner { get; }
        public bool IsDisposed => disposed || Owner == null;

        public TweenScope(GameObject owner) => Owner = owner;

        /// <summary>
        /// Transfer lifecycle of a standalone tween or outermost sequence.
        /// Do not nest it, relink it, enable recycling, or track it in another scope afterwards.
        /// </summary>
        public T Track<T>(T tween) where T : Tween
        {
            if (IsDisposed || !Owner.scene.IsValid() || !TweenRuntime.IsInitialized || tween == null || !tween.IsActive()) return null;
            Prune();
            foreach (var tracked in tweens)
                if (ReferenceEquals(tracked, tween)) return tween;
            tween.SetRecyclable(false);
            tween.SetLink(Owner, LinkBehaviour.KillOnDestroy);
            tweens.Add(tween);
            return tween;
        }

        public void Prune()
        {
            for (int i = tweens.Count - 1; i >= 0; i--)
                if (!tweens[i].IsActive()) tweens.RemoveAt(i);
        }

        public void CancelAll()
        {
            if (disposed) return;
            KillSnapshot();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            KillSnapshot();
        }

        private void KillSnapshot()
        {
            var snapshot = tweens.ToArray();
            tweens.Clear();
            foreach (var tween in snapshot)
                if (tween.IsActive()) tween.Kill(false);
        }
    }
}
