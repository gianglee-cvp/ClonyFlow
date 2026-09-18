using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Core.Pooling
{
    internal sealed class PoolEntry
    {
        public readonly GameObject Object;
        private readonly Component[] components;
        private readonly List<IPoolable> hooks = new List<IPoolable>();
        private readonly Dictionary<Type, Component> bindings = new Dictionary<Type, Component>();

        public PoolEntry(GameObject instance)
        {
            Object = instance;
            components = instance.GetComponents<Component>();
            foreach (var component in components)
                if (component is IPoolable hook) hooks.Add(hook);
        }

        public T Bind<T>() where T : Component
        {
            if (bindings.TryGetValue(typeof(T), out var cached)) return cached as T;
            foreach (var component in components)
                if (component is T match) { bindings.Add(typeof(T), match); return match; }
            bindings.Add(typeof(T), null);
            return null;
        }

        public void Spawned() { foreach (var hook in hooks) hook.OnPoolSpawned(); }
        public void Recycled() { foreach (var hook in hooks) hook.OnPoolRecycled(); }
    }

    internal sealed class ObjectPool
    {
        private readonly ObjectPoolManager manager;
        private readonly Queue<PoolEntry> available = new Queue<PoolEntry>();
        private readonly Dictionary<GameObject, PoolEntry> entries = new Dictionary<GameObject, PoolEntry>();
        private readonly HashSet<GameObject> borrowed = new HashSet<GameObject>();
        private readonly Transform container;
        private readonly Vector3 position, scale;
        private readonly Quaternion rotation;
        public readonly GameObject Prefab;
        public bool Closing { get; private set; }

        public ObjectPool(GameObject prefab, Transform root, ObjectPoolManager owner)
        {
            Prefab = prefab; manager = owner;
            position = prefab.transform.localPosition;
            rotation = prefab.transform.localRotation;
            scale = prefab.transform.localScale;
            container = new GameObject(prefab.name + " Pool").transform;
            container.SetParent(root, false);
            container.gameObject.SetActive(false);
        }

        public void Load(int count)
        {
            Prune();
            while (!Closing && available.Count < count) Create();
        }

        private void Create()
        {
            var instance = UnityEngine.Object.Instantiate(Prefab, container, false);
            instance.SetActive(false);
            var entry = new PoolEntry(instance);
            entries.Add(instance, entry);
            available.Enqueue(entry);
            manager.Register(instance, this);
            manager.NotifyInstantiated(instance);
        }

        public PoolEntry Acquire()
        {
            while (available.Count > 0)
            {
                var entry = available.Dequeue();
                if (entry.Object != null) return entry;
                entries.Remove(entry.Object);
                manager.Unregister(entry.Object);
            }
            Create();
            return available.Dequeue();
        }

        public void ReturnUnused(PoolEntry entry) => available.Enqueue(entry);

        public GameObject Activate(PoolEntry entry, Vector3? point, Quaternion? facing, Transform parent, bool world)
        {
            var instance = entry.Object;
            var target = instance.transform;
            target.SetParent(parent, false);
            target.localScale = scale;
            if (world) target.SetPositionAndRotation(point ?? position, facing ?? rotation);
            else { target.localPosition = point ?? position; target.localRotation = facing ?? rotation; }
            borrowed.Add(instance);
            entry.Spawned();
            instance.SetActive(true);
            manager.NotifySpawned(instance);
            return instance;
        }

        public bool Recycle(GameObject instance)
        {
            if (!borrowed.Remove(instance)) return false;
            instance.SetActive(false);
            var entry = entries[instance];
            entry.Recycled();
            instance.transform.SetParent(container, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;
            available.Enqueue(entry);
            manager.NotifyRecycled(instance);
            return true;
        }

        public void RecycleAll()
        {
            foreach (var instance in new List<GameObject>(borrowed))
                if (instance != null) Recycle(instance);
            Prune();
        }

        public void Cleanup(int retain)
        {
            Prune();
            while (available.Count > retain)
            {
                var instance = available.Dequeue().Object;
                entries.Remove(instance);
                manager.Unregister(instance);
                manager.NotifyCleanedUp(instance);
                ObjectPoolManager.DestroyObject(instance);
            }
        }

        public void Close()
        {
            Closing = true;
            RecycleAll();
            Cleanup(0);
            if (container != null) ObjectPoolManager.DestroyObject(container.gameObject);
        }

        private void Prune()
        {
            foreach (var instance in new List<GameObject>(entries.Keys))
            {
                if (instance != null) continue;
                entries.Remove(instance);
                borrowed.Remove(instance);
                manager.Unregister(instance);
            }
            int count = available.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = available.Dequeue();
                if (entry.Object != null) available.Enqueue(entry);
            }
        }
    }
}
