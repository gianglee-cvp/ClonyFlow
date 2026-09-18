using System;
using UnityEngine;

namespace ColonyFlow.Core.Pooling
{
    public interface IObjectPoolManager : IDisposable
    {
        event Action<GameObject> Instantiated;
        event Action<GameObject> Spawned;
        event Action<GameObject> Recycled;
        event Action<GameObject> CleanedUp;
        bool Load(GameObject prefab, int count = 1);
        bool Load<T>(T prefab, int count = 1) where T : Component;
        GameObject Spawn(GameObject prefab, Vector3? position = null, Quaternion? rotation = null,
            Transform parent = null, bool spawnInWorldSpace = true);
        T Spawn<T>(T prefab, Vector3? position = null, Quaternion? rotation = null,
            Transform parent = null, bool spawnInWorldSpace = true) where T : Component;
        bool Recycle(GameObject instance);
        bool Recycle(Component instance);
        void RecycleAll(GameObject prefab);
        void RecycleAll(Component prefab);
        void Cleanup(GameObject prefab, int retainCount = 1);
        void Cleanup(Component prefab, int retainCount = 1);
        void Unload(GameObject prefab);
        void Unload(Component prefab);
    }
}
