using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Core.Pooling
{
    public sealed class ObjectPoolManager : IObjectPoolManager
    {
        private readonly Transform root;
        private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();
        private readonly Dictionary<GameObject, ObjectPool> instances = new Dictionary<GameObject, ObjectPool>();
        private bool disposed;
        private int notificationDepth;
        private bool CanOperate => !disposed && root != null && notificationDepth == 0;
        public event Action<GameObject> Instantiated;
        public event Action<GameObject> Spawned;
        public event Action<GameObject> Recycled;
        public event Action<GameObject> CleanedUp;

        public ObjectPoolManager(Transform owner)
        {
            if (owner == null || !owner.gameObject.scene.IsValid()) return;
            root = new GameObject("Object Pools").transform;
            root.SetParent(owner, false);
            root.gameObject.SetActive(false);
        }

        private ObjectPool GetPool(GameObject prefab)
        {
            if (!CanOperate || prefab == null || prefab.scene.IsValid()) return null;
            if (pools.TryGetValue(prefab, out var existing)) return existing;
            var pool = new ObjectPool(prefab, root, this);
            pools.Add(prefab, pool);
            return pool;
        }

        public bool Load(GameObject prefab, int count = 1)
        {
            if (count < 0) return false;
            var pool = GetPool(prefab);
            if (pool == null) return false;
            pool.Load(count);
            return true;
        }

        public bool Load<T>(T prefab, int count = 1) where T : Component => Load(prefab != null ? prefab.gameObject : null, count);

        private bool ValidParent(Transform parent) => CanOperate &&
            (parent == null || parent.gameObject.scene == root.gameObject.scene);

        public GameObject Spawn(GameObject prefab, Vector3? position = null, Quaternion? rotation = null,
            Transform parent = null, bool spawnInWorldSpace = true)
        {
            if (!ValidParent(parent)) return null;
            var pool = GetPool(prefab);
            return pool == null ? null : pool.Activate(pool.Acquire(), position, rotation, parent, spawnInWorldSpace);
        }

        public T Spawn<T>(T prefab, Vector3? position = null, Quaternion? rotation = null,
            Transform parent = null, bool spawnInWorldSpace = true) where T : Component
        {
            if (prefab == null || !ValidParent(parent)) return null;
            var pool = GetPool(prefab.gameObject);
            if (pool == null) return null;
            var entry = pool.Acquire();
            var component = entry.Bind<T>();
            if (component == null) { pool.ReturnUnused(entry); return null; }
            pool.Activate(entry, position, rotation, parent, spawnInWorldSpace);
            return component;
        }

        public bool Recycle(GameObject instance) => !disposed && root != null && instance != null &&
            instances.TryGetValue(instance, out var pool) && !pool.Closing && pool.Recycle(instance);
        public bool Recycle(Component instance) => Recycle(instance != null ? instance.gameObject : null);

        public void RecycleAll(GameObject prefab)
        {
            if (CanOperate && prefab != null && pools.TryGetValue(prefab, out var pool)) pool.RecycleAll();
        }
        public void RecycleAll(Component prefab) => RecycleAll(prefab != null ? prefab.gameObject : null);
        public void Cleanup(GameObject prefab, int retainCount = 1)
        {
            if (CanOperate && retainCount >= 0 && prefab != null && pools.TryGetValue(prefab, out var pool)) pool.Cleanup(retainCount);
        }
        public void Cleanup(Component prefab, int retainCount = 1) => Cleanup(prefab != null ? prefab.gameObject : null, retainCount);
        public void Unload(GameObject prefab)
        {
            if (!CanOperate || prefab == null || !pools.TryGetValue(prefab, out var pool)) return;
            pools.Remove(prefab);
            pool.Close();
        }
        public void Unload(Component prefab) => Unload(prefab != null ? prefab.gameObject : null);
        public void Dispose()
        {
            if (disposed || notificationDepth != 0) return;
            disposed = true;
            foreach (var pool in new List<ObjectPool>(pools.Values)) pool.Close();
            pools.Clear(); instances.Clear();
            if (root != null) DestroyObject(root.gameObject);
        }

        internal void Register(GameObject instance, ObjectPool pool) => instances.Add(instance, pool);
        internal void Unregister(GameObject instance) => instances.Remove(instance);
        internal void NotifyInstantiated(GameObject instance) => Notify(Instantiated, instance);
        internal void NotifySpawned(GameObject instance) => Notify(Spawned, instance);
        internal void NotifyRecycled(GameObject instance) => Notify(Recycled, instance);
        internal void NotifyCleanedUp(GameObject instance) => Notify(CleanedUp, instance);
        private void Notify(Action<GameObject> handler, GameObject instance)
        {
            notificationDepth++;
            handler?.Invoke(instance);
            notificationDepth--;
        }
        internal static void DestroyObject(GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(instance);
            else UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
