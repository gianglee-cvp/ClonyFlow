using System;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class MapView : MonoBehaviour
    {
        [SerializeField] private TextAsset mapJson;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private Transform mapRoot;
        [SerializeField] private bool loadOnStart = true;

        private GameObject spawnedRoot;
        public MapModel Model { get; private set; }
        public int SpawnedCellCount { get; private set; }

        public void Configure(GameObject prefab, Camera camera, Transform root, TextAsset json = null)
        {
            cellPrefab = prefab;
            mapCamera = camera;
            mapRoot = root;
            mapJson = json;
        }

        private void Start()
        {
            if (loadOnStart) LoadMap();
        }

        [ContextMenu("Load Map JSON")]
        public void LoadMap()
        {
            try
            {
                if (mapJson == null) throw new InvalidOperationException("Assign a map JSON TextAsset.");
                LoadJson(mapJson.text);
            }
            catch (Exception exception)
            {
                Debug.LogError("Map load failed: " + exception.Message, this);
            }
        }

        public void LoadJson(string json)
        {
            var nextModel = MapJsonLoader.Load(json);
            if (cellPrefab == null) throw new InvalidOperationException("Assign a cell prefab.");
            if (mapCamera == null) throw new InvalidOperationException("Assign a map camera.");
            if (mapRoot == null) throw new InvalidOperationException("Assign a map root.");
            if (spawnedRoot != null && (mapRoot.IsChildOf(spawnedRoot.transform) ||
                mapCamera.transform.IsChildOf(spawnedRoot.transform)))
                throw new InvalidOperationException("Map root and camera cannot belong to the generated map.");
            CellView.ValidatePrefab(cellPrefab);

            // Prepare offscreen; only replace the current map after all cells are ready.
            var nextRoot = new GameObject("Generated Map");
            nextRoot.SetActive(false);
            // Keep generated geometry in world space, independent of the organizing root.
            nextRoot.transform.SetParent(mapRoot, true);
            int count = 0;
            try
            {
                foreach (var cell in nextModel.EnumerateCells())
                {
                    if (cell.IsEmpty) continue;
                    var instance = Instantiate(cellPrefab, cell.Position, Quaternion.identity, nextRoot.transform);
                    instance.name = $"Cell [{cell.Row},{cell.Column}] Color {cell.ColorId}";
                    instance.SetActive(true);
                    var view = instance.GetComponent<CellView>();
                    if (view == null) view = instance.AddComponent<CellView>();
                    view.Initialize(cell, nextModel.GetColor(cell.ColorId));
                    count++;
                }
            }
            catch
            {
                DisposeRoot(nextRoot);
                throw;
            }

            Clear();
            spawnedRoot = nextRoot;
            Model = nextModel;
            SpawnedCellCount = count;
            mapCamera.transform.SetPositionAndRotation(nextModel.CameraPosition,
                Quaternion.Euler(nextModel.CameraRotation));
            spawnedRoot.SetActive(true);
        }

        [ContextMenu("Clear Map")]
        public void Clear()
        {
            if (spawnedRoot != null) DisposeRoot(spawnedRoot);
            spawnedRoot = null;
            Model = null;
            SpawnedCellCount = 0;
        }

        private static void DisposeRoot(GameObject root)
        {
            root.SetActive(false);
            root.transform.SetParent(null, true);
            if (Application.isPlaying) Destroy(root);
            else DestroyImmediate(root);
        }

        private void OnDestroy() => Clear();
    }
}
