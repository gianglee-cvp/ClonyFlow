using System;
using DG.Tweening;
using UnityEngine;

namespace ColonyFlow.Core.Tweening
{
    public static class TweenRuntime
    {
        private static TweenSettings applied;
        private static UnityEngine.Object engine;
        internal static bool IsInitialized => applied != null && DOTween.instance != null && ReferenceEquals(engine, DOTween.instance);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { applied = null; engine = null; }

        public static void Initialize(TweenSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.TweenersCapacity <= 0 || settings.SequencesCapacity <= 0 ||
                settings.TweenersCapacity < settings.SequencesCapacity)
                throw new ArgumentOutOfRangeException(nameof(settings), "Capacities must be positive; tweeners must cover sequences.");
            if (!Enum.IsDefined(typeof(LogBehaviour), settings.LogBehaviour))
                throw new ArgumentOutOfRangeException(nameof(settings), "Invalid log behaviour.");

            if (IsInitialized)
            {
                if (applied.TweenersCapacity != settings.TweenersCapacity ||
                    applied.SequencesCapacity != settings.SequencesCapacity ||
                    applied.UseSafeMode != settings.UseSafeMode ||
                    applied.LogBehaviour != settings.LogBehaviour)
                    throw new InvalidOperationException("TweenRuntime is already initialized with different settings.");
                return;
            }
            if (DOTween.instance != null)
                throw new InvalidOperationException("Initialize TweenRuntime before creating any DOTween tween.");

            if (!Application.isPlaying)
                throw new InvalidOperationException("TweenRuntime requires Play Mode.");
            var initialization = DOTween.Init(false, settings.UseSafeMode, settings.LogBehaviour);
            if (initialization == null)
                throw new InvalidOperationException("DOTween cannot initialize while the application is quitting.");
            initialization.SetCapacity(settings.TweenersCapacity, settings.SequencesCapacity);
            engine = DOTween.instance;
            applied = new TweenSettings
            {
                TweenersCapacity = settings.TweenersCapacity,
                SequencesCapacity = settings.SequencesCapacity,
                UseSafeMode = settings.UseSafeMode,
                LogBehaviour = settings.LogBehaviour
            };
        }
    }
}