namespace ColonyFlow.Core.Pooling
{
    public interface IPoolable
    {
        void OnPoolSpawned();
        void OnPoolRecycled();
    }
}
