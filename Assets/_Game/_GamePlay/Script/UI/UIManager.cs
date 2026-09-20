using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class UIManager : Singleton<UIManager>
    {
        [SerializeField] private Transform parent;
        private readonly Dictionary<Type, UICanvas> canvasActives = new Dictionary<Type, UICanvas>();
        private readonly Dictionary<Type, UICanvas> canvasPrefabs = new Dictionary<Type, UICanvas>();
        private bool initialized;

        public void OnInit()
        {
            if (initialized) return;
            initialized = true;
            if (parent == null) parent = transform;
            foreach (var prefab in Resources.LoadAll<UICanvas>("UI"))
                if (prefab != null) canvasPrefabs[prefab.GetType()] = prefab;
        }

        public T OpenUI<T>() where T : UICanvas
        {
            var canvas = GetUI<T>();
            if (canvas == null) return null;
            canvas.Setup();
            canvas.Open();
            return canvas;
        }

        public void CloseUI<T>(float delay = 0) where T : UICanvas
        {
            if (IsLoaded<T>()) canvasActives[typeof(T)].Close(delay);
        }

        public void CloseUIDirectly<T>() where T : UICanvas
        {
            if (IsLoaded<T>()) canvasActives[typeof(T)].CloseDirectly();
        }

        public bool IsLoaded<T>() where T : UICanvas =>
            canvasActives.TryGetValue(typeof(T), out var canvas) && canvas != null;

        public bool IsOpened<T>() where T : UICanvas => IsLoaded<T>() && canvasActives[typeof(T)].gameObject.activeSelf;

        public T GetUI<T>() where T : UICanvas
        {
            OnInit();
            if (!IsLoaded<T>())
            {
                var prefab = GetUIPrefab<T>();
                if (prefab == null) return null;
                var canvas = Instantiate(prefab, parent);
                canvas.gameObject.SetActive(false);
                canvasActives[typeof(T)] = canvas;
            }
            return canvasActives[typeof(T)] as T;
        }

        public T GetUIPrefab<T>() where T : UICanvas
        {
            OnInit();
            return canvasPrefabs.TryGetValue(typeof(T), out var prefab) ? prefab as T : null;
        }

        public void CloseAllUI()
        {
            foreach (var canvas in canvasActives.Values)
                if (canvas != null && canvas.gameObject.activeSelf) canvas.CloseDirectly();
        }
    }
}
