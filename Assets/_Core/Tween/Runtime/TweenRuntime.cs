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
        private static void ResetSession()
        {
            applied = null;
            engine = null;
        }

        public static bool Initialize(TweenSettings settings)
        {
            if (!IsValid(settings)) return false;
            if (IsInitialized) return Matches(settings);
            if (DOTween.instance != null || !Application.isPlaying) return false;
            var initialization = DOTween.Init(false, settings.UseSafeMode, settings.LogBehaviour);
            if (initialization == null) return false;
            initialization.SetCapacity(settings.TweenersCapacity, settings.SequencesCapacity);
            engine = DOTween.instance;
            applied = Snapshot(settings);
            return true;
        }

        private static bool IsValid(TweenSettings settings) =>
            settings != null && settings.TweenersCapacity > 0 && settings.SequencesCapacity > 0 &&
            settings.TweenersCapacity >= settings.SequencesCapacity && Enum.IsDefined(typeof(LogBehaviour), settings.LogBehaviour);

        private static bool Matches(TweenSettings settings) =>
            applied.TweenersCapacity == settings.TweenersCapacity &&
            applied.SequencesCapacity == settings.SequencesCapacity &&
            applied.UseSafeMode == settings.UseSafeMode && applied.LogBehaviour == settings.LogBehaviour;

        private static TweenSettings Snapshot(TweenSettings settings) => new TweenSettings
        {
            TweenersCapacity = settings.TweenersCapacity,
            SequencesCapacity = settings.SequencesCapacity,
            UseSafeMode = settings.UseSafeMode,
            LogBehaviour = settings.LogBehaviour
        };
    }
}
