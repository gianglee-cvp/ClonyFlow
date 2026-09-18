using DG.Tweening;

namespace ColonyFlow.Core.Tweening
{
    public sealed class TweenSettings
    {
        public int TweenersCapacity { get; set; } = 256;
        public int SequencesCapacity { get; set; } = 64;
        public bool UseSafeMode { get; set; } = true;
        public LogBehaviour LogBehaviour { get; set; } = LogBehaviour.ErrorsOnly;
    }
}