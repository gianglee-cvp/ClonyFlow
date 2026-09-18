using System;
using System.Collections.Generic;
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
        [SerializeField] private bool enforceDemoBounds;
        private readonly Dictionary<Cell, CellView> cellViews = new Dictionary<Cell, CellView>();
        private GameObject spawnedRoot;
        public Transform Root => mapRoot;
        public void SetDemoBounds(bool enabled) => enforceDemoBounds = enabled;
        public bool Collect(Cell cell)
        {
            if (Model == null || !Model.TryCollect(cell)) return false;
            if (cellViews.TryGetValue(cell, out var view)) view.gameObject.SetActive(false);
            return true;
        }
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
            if (loadOnStart && Model == null) LoadMap();
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
            if (enforceDemoBounds)
            {
                if (nextModel.Rows > 20 || nextModel.Columns > 20)
                    throw new InvalidOperationException("Fixed portrait demo supports at most 20 rows and 20 columns.");
                foreach (var cell in nextModel.EnumerateCells())
                    if (Mathf.Abs(cell.Position.x) > 10.45f + .001f ||
                        Mathf.Abs(cell.Position.z) > 10.45f + .001f ||
                        Mathf.Abs(cell.Position.y) > .001f)
                        throw new InvalidOperationException("Map centers must lie inside the authored +/-10.45 XZ bounds at Y=0.");
                if (nextModel.Columns > 1 && Mathf.Abs(nextModel.GetCell(0, 1).Position.x -
                    nextModel.GetCell(0, 0).Position.x - 1.1f) > .001f ||
                    nextModel.Rows > 1 && Mathf.Abs(nextModel.GetCell(0, 0).Position.z -
                    nextModel.GetCell(1, 0).Position.z - 1.1f) > .001f)
                    throw new InvalidOperationException("Fixed portrait demo cell spacing must be 1.1.");
            }

            // Prepare offscreen; only replace the current map after all cells are ready.
            var nextRoot = new GameObject("Generated Map");
            nextRoot.SetActive(false);
            // JSON positions are local to the fixed map root. Preserve the prefab cell scale.
            nextRoot.transform.SetParent(mapRoot, false);
            int count = 0;
            try
            {
                foreach (var cell in nextModel.EnumerateCells())
                {
                    if (cell.IsEmpty) continue;
                    var instance = Instantiate(cellPrefab, nextRoot.transform, false);
                    instance.transform.localPosition = cell.Position;
                    instance.transform.localRotation = Quaternion.identity;
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
            foreach (var view in nextRoot.GetComponentsInChildren<CellView>(true))
                cellViews.Add(view.Cell, view);
            spawnedRoot.SetActive(true);
        }

        [ContextMenu("Clear Map")]
        public void Clear()
        {
            if (spawnedRoot != null) DisposeRoot(spawnedRoot);
            spawnedRoot = null;
            cellViews.Clear();
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
