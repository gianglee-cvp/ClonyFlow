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
        [SerializeField] private Renderer mapCardSurface;
        [SerializeField, Min(0)] private float mapPadding = .35f;
        [SerializeField, Min(1)] private float cellHeightMultiplier = 2;
        [SerializeField, Min(0)] private float cellGroundClearance = .2f;
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool enforceDemoBounds;
        private readonly Dictionary<Cell, CellView> cellViews = new Dictionary<Cell, CellView>();
        private GameObject spawnedRoot;
        public Transform Root => mapRoot;
        public float MapPadding => mapPadding;
        public void ConfigureCellPrefab(GameObject prefab) => cellPrefab = prefab;
        public void ConfigureCellSurface(Renderer surface) => mapCardSurface = surface;
        public Vector3 GetCellVisualPosition(Cell cell) =>
            cellViews.TryGetValue(cell, out var view) ? view.transform.position : mapRoot.TransformPoint(cell.Position);
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
            if (mapCardSurface == null)
                mapCardSurface = GameObject.Find("MapCard")?.transform.Find("Surface")?.GetComponent<Renderer>();
            if (mapCardSurface != null)
            {
                var cardBounds = MapPerimeter.CardBounds(mapRoot, mapCardSurface);
                float maxCellWidth = (cardBounds.size.x - mapPadding * 2) / nextModel.Columns;
                float maxHeight = maxCellWidth * nextModel.CellScale.y / nextModel.CellScale.x * cellHeightMultiplier;
                // Reserve screen space for the raised cell tops under the tilted camera.
                float projectionRatio = Mathf.Abs(Vector3.Dot(mapCamera.transform.up, mapRoot.up) /
                    Vector3.Dot(mapCamera.transform.up, mapRoot.forward));
                float topReserve = (cellGroundClearance + maxHeight) * projectionRatio;
                nextModel.LayoutToCard(cardBounds, mapPadding, cellHeightMultiplier, topReserve);
                if (enforceDemoBounds && nextModel.Rows * nextModel.CellSpacing.y > cardBounds.size.z - mapPadding * 2 + .001f)
                    throw new InvalidOperationException("Map height exceeds the card. Apply Fixed Portrait Layout for this JSON.");
            }
            // Prepare offscreen; only replace the current map after all cells are ready.
            var nextRoot = new GameObject("Generated Map");
            nextRoot.SetActive(false);
            // JSON positions are local to the fixed map root. Preserve the prefab cell scale.
            nextRoot.transform.SetParent(mapRoot, false);
            if (mapCardSurface == null)
                mapCardSurface = GameObject.Find("MapCard")?.transform.Find("Surface")?.GetComponent<Renderer>();
            float groundTop = mapCardSurface != null ? mapCardSurface.bounds.max.y : mapRoot.position.y;
            int count = 0;
            try
            {
                foreach (var cell in nextModel.EnumerateCells())
                {
                    if (cell.IsEmpty) continue;
                    var instance = Instantiate(cellPrefab, nextRoot.transform, false);
                    instance.transform.localPosition = cell.Position;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = nextModel.CellScale;
                    // Offset only the visual mesh: grid/navigation positions stay unchanged.
                    float bottom = float.PositiveInfinity;
                    foreach (var meshRenderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        var bounds = meshRenderer.localBounds;
                        for (int corner = 0; corner < 8; corner++)
                        {
                            var point = new Vector3(
                                (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                                (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                                (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                            bottom = Mathf.Min(bottom, meshRenderer.transform.TransformPoint(point).y);
                        }
                    }
                    instance.transform.position += Vector3.up * (groundTop + cellGroundClearance - bottom);
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
