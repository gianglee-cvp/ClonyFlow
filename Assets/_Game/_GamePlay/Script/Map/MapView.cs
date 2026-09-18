using System.Collections.Generic;
using UnityEngine;
using ColonyFlow.Core.Pooling;

namespace ColonyFlow.Gameplay
{
    public sealed class MapView : MonoBehaviour
    {
        [SerializeField] private TextAsset mapJson;
        [SerializeField] private CellView cellPrefab;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private Transform mapRoot;
        [SerializeField] private Renderer mapCardSurface;
        [SerializeField, Min(0)] private float mapPadding = .35f;
        [SerializeField, Min(1)] private float cellHeightMultiplier = 2;
        [SerializeField, Min(0)] private float cellGroundClearance = .2f;
        [SerializeField] private bool loadOnStart = true;
        private readonly Dictionary<Cell, CellView> cellViews = new Dictionary<Cell, CellView>();
        private GameObject spawnedRoot;
        private ObjectPoolManager pool;
        private readonly Dictionary<Cell, Vector3> collectedPositions = new Dictionary<Cell, Vector3>();
        public Transform Root => mapRoot;
        public MapModel Model { get; private set; }
        public int SpawnedCellCount => cellViews.Count;

        public void Configure(CellView prefab, Camera camera, Transform root, TextAsset json)
        {
            cellPrefab = prefab;
            mapCamera = camera;
            mapRoot = root;
            mapJson = json;
        }

        public void ConfigureCellPrefab(CellView prefab) => cellPrefab = prefab;
        public void ConfigureCellSurface(Renderer surface) => mapCardSurface = surface;

        private void Start()
        {
            if (loadOnStart && Model == null) LoadMap();
        }

        [ContextMenu("Load Level JSON")]
        public bool LoadMap() => mapJson != null && LoadJson(mapJson.text);

        public bool LoadJson(string json)
        {
            if (cellPrefab == null || mapCamera == null || mapRoot == null) return false;
            var model = MapJsonLoader.Load(json);
            if (model == null || !FitToCard(model)) return false;
            Clear();
            if (pool == null) pool = new ObjectPoolManager(transform);
            pool.Load(cellPrefab, model.ColoredCellCount);
            Model = model;
            CreateMapRoot();
            SpawnCells();
            spawnedRoot.SetActive(true);
            return true;
        }

        private bool FitToCard(MapModel model)
        {
            if (mapCardSurface == null) return true;
            var bounds = MapPerimeter.CardBounds(mapRoot, mapCardSurface);
            float maxWidth = (bounds.size.x - mapPadding * 2) / model.Columns;
            float maxHeight = maxWidth * model.CellScale.y / model.CellScale.x * cellHeightMultiplier;
            float projectionRatio = Mathf.Abs(Vector3.Dot(mapCamera.transform.up, mapRoot.up) /
                Vector3.Dot(mapCamera.transform.up, mapRoot.forward));
            float topReserve = (cellGroundClearance + maxHeight) * projectionRatio;
            return model.LayoutToCard(bounds, mapPadding, cellHeightMultiplier, topReserve);
        }

        private void CreateMapRoot()
        {
            spawnedRoot = new GameObject("Generated Map");
            spawnedRoot.SetActive(false);
            spawnedRoot.transform.SetParent(mapRoot, false);
        }

        private void SpawnCells()
        {
            float groundTop = mapCardSurface != null ? mapCardSurface.bounds.max.y : mapRoot.position.y;
            foreach (var cell in Model.EnumerateCells())
                if (!cell.IsEmpty) SpawnCell(cell, groundTop);
        }

        private void SpawnCell(Cell cell, float groundTop)
        {
            var view = pool.Spawn(cellPrefab, parent: spawnedRoot.transform, spawnInWorldSpace: false);
            view.transform.localPosition = cell.Position;
            view.transform.localRotation = Quaternion.identity;
            view.transform.localScale = Model.CellScale;
            view.transform.position += Vector3.up * (groundTop + cellGroundClearance - view.WorldBottom());
            view.name = $"Cell [{cell.Row},{cell.Column}] Color {cell.ColorId}";
            view.Initialize(cell, Model.GetColor(cell.ColorId));
            view.gameObject.SetActive(true);
            cellViews.Add(cell, view);
        }

        public Vector3 GetCellVisualPosition(Cell cell) => cellViews.TryGetValue(cell, out var view)
            ? view.transform.position : collectedPositions[cell];

        public bool Collect(Cell cell)
        {
            if (Model == null || !Model.TryCollect(cell)) return false;
            var view = cellViews[cell];
            collectedPositions[cell] = view.transform.position;
            cellViews.Remove(cell);
            pool.Recycle(view);
            return true;
        }

        [ContextMenu("Clear Map")]
        public void Clear()
        {
            foreach (var view in cellViews.Values) pool?.Recycle(view);
            DisposeRoot();
            cellViews.Clear();
            collectedPositions.Clear();
            Model = null;
        }

        private void DisposeRoot()
        {
            if (spawnedRoot == null) return;
            spawnedRoot.SetActive(false);
            if (Application.isPlaying) Destroy(spawnedRoot);
            else DestroyImmediate(spawnedRoot);
            spawnedRoot = null;
        }

        private void OnDestroy()
        {
            Clear();
            pool?.Dispose();
        }
    }
}
