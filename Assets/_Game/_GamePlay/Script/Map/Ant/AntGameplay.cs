using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    [Serializable]
    public struct DemoBoxData
    {
        public int queueIndex;
        public int colorId;
        public int antCount;
    }

    public sealed class AntGameplay : MonoBehaviour
    {
        [SerializeField] private MapView mapView;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private BoxActor boxPrefab;
        [SerializeField] private AntActor antPrefab;
        [SerializeField] private Transform[] boxAnchors;
        [SerializeField] private Transform[] perimeterEntries;
        [SerializeField] private Transform[] queueAnchors;
        [SerializeField] private Transform holeReturn;
        [SerializeField] private Transform holeJump;
        [SerializeField] private Transform holeExit;
        [SerializeField] private TextAsset boxQueuesJson;
        [SerializeField] private Renderer mapCardSurface;
        [SerializeField] private DemoBoxData[] boxes; // Legacy scene fallback.
        [SerializeField] private float spawnInterval = .22f;
        [SerializeField] private float antSpeed = 8;
        private readonly List<List<BoxActor>> queues = new List<List<BoxActor>>();
        private readonly List<AntActor> active = new List<AntActor>();
        private readonly Dictionary<Cell, long> reserves = new Dictionary<Cell, long>();
        private readonly List<AntActor> pickups = new List<AntActor>();
        private BoxActor[] slots;
        private Transform runtimeRoot;
        private GridNavigation navigation;
        private MapPerimeter perimeter;
        private Bounds cardBounds;
        private long nextTaskId;
        private bool initialized;
        public bool IsBoardCleared { get; private set; }
        public bool IsLevelComplete { get; private set; }
        public bool IsDeadlocked { get; private set; }
        public bool Paused { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1;
        public int ActiveTripCount => active.Count;
        public int RemainingCellCount { get; private set; }
        public GridNavigation Navigation => navigation;
        public IReadOnlyList<AntActor> ActiveAnts => active;
        public IReadOnlyList<BoxActor> Slots => slots;

        public void Configure(MapView map, Camera camera, BoxActor box, AntActor ant,
            Transform[] anchors, Transform[] entries, Transform[] queuePoints,
            Transform returning, Transform jumping, Transform exit, DemoBoxData[] data)
        {
            mapView = map; gameplayCamera = camera; boxPrefab = box; antPrefab = ant;
            boxAnchors = anchors; perimeterEntries = entries; queueAnchors = queuePoints;
            holeReturn = returning; holeJump = jumping; holeExit = exit; boxes = data;
        }
        public void ConfigureCard(Renderer surface) => mapCardSurface = surface;
        public void ConfigureQueues(TextAsset json) => boxQueuesJson = json;
        private void Start() { if (!initialized) Initialize(); }
        public void Initialize()
        {
            if (mapView.Model == null) mapView.LoadMap();
            if (mapView.Model == null) throw new InvalidOperationException("Map did not load.");
            var layout = boxQueuesJson != null
                ? BoxQueueJsonLoader.Load(boxQueuesJson.text, mapView.Model, queueAnchors.Length) : boxes;
            Cleanup();
            navigation = new GridNavigation(mapView.Model);
            if (mapCardSurface == null) throw new InvalidOperationException("Map card surface is required.");
            cardBounds = MapPerimeter.CardBounds(mapView.Root, mapCardSurface);
            perimeter = new MapPerimeter(mapView.Root, mapView.Model, cardBounds);
            // Resolve legacy anchors once on load; new scenes bake these exact positions in the Editor.
            for (int i = 0; i < perimeterEntries.Length; i++)
                perimeterEntries[i].position = perimeter.BottomEntry(mapView.Root.InverseTransformPoint(boxAnchors[i].position).x);
            holeExit.position = perimeter.BottomEntry(0);
            RemainingCellCount = 0;
            foreach (var cell in mapView.Model.EnumerateCells()) if (!cell.IsEmpty) RemainingCellCount++;
            slots = new BoxActor[boxAnchors.Length];
            runtimeRoot = new GameObject("Runtime Boxes and Ants").transform;
            runtimeRoot.SetParent(transform, false);
            for (int q = 0; q < queueAnchors.Length; q++) queues.Add(new List<BoxActor>());
            foreach (var data in layout)
            {
                if (data.queueIndex < 0 || data.queueIndex >= queues.Count || data.antCount <= 0)
                    throw new InvalidOperationException("Invalid demo Box configuration.");
                var actor = Instantiate(boxPrefab, runtimeRoot, false);
                actor.Initialize(data.colorId, data.antCount, data.queueIndex, mapView.Model.GetColor(data.colorId));
                queues[data.queueIndex].Add(actor);
            }
            RefreshQueues();
            initialized = true; Paused = false; SpeedMultiplier = 1; nextTaskId = 0;
            IsBoardCleared = RemainingCellCount == 0; IsLevelComplete = IsBoardCleared;
            IsDeadlocked = false;
        }
        [ContextMenu("Restart Ant Demo")]
        public void Restart()
        {
            Cleanup(); mapView.LoadMap(); Initialize();
        }
        public bool PickQueue(int queueIndex)
        {
            if (!initialized || Paused || IsBoardCleared || IsDeadlocked ||
                queueIndex < 0 || queueIndex >= queues.Count || queues[queueIndex].Count == 0) return false;
            int slot = Array.FindIndex(slots, x => x == null);
            if (slot < 0) return false;
            var box = queues[queueIndex][0];
            queues[queueIndex].RemoveAt(0);
            box.SlotIndex = slot; box.Timer = spawnInterval;
            box.transform.position = boxAnchors[slot].position;
            slots[slot] = box; RefreshQueues();
            return true;
        }
        private void RefreshQueues()
        {
            int nonempty = 0;
            foreach (var queue in queues) if (queue.Count > 0) nonempty++;
            int visible = 0;
            for (int q = 0; q < queues.Count; q++)
            {
                if (queues[q].Count == 0) continue;
                float x = (visible++ - (nonempty - 1) * .5f) * 5.7f;
                for (int i = 0; i < queues[q].Count; i++)
                {
                    var point = queueAnchors[q].position;
                    point.x = mapView.Root.position.x + x;
                    point.z -= i * 4.8f;
                    queues[q][i].transform.position = point;
                    // Keep deep queue rows hidden below the display, without losing their data.
                    queues[q][i].gameObject.SetActive(i < 3);
                }
            }
        }
        private void Update() { if (initialized) Advance(Time.deltaTime); }
        public void Advance(float delta)
        {
            if (!initialized || Paused || IsLevelComplete || delta <= 0) return;
            if (navigation.Map != mapView.Model) { Initialize(); return; }
            RefreshCardPerimeter();
            float dt = delta * SpeedMultiplier;
            pickups.Clear();
            foreach (var ant in active)
            {
                ant.Advance(dt, antSpeed);
                if (ant.State == AntTripState.WaitingPickup) pickups.Add(ant);
            }
            // Tween/callback order must not decide collection or newly exposed cells.
            pickups.Sort((a, b) => a.TaskId.CompareTo(b.TaskId));
            foreach (var ant in pickups)
            {
                if (!reserves.TryGetValue(ant.Target, out var owner) || owner != ant.TaskId ||
                    !mapView.Collect(ant.Target))
                {
                    if (ant.Source != null) ant.Source.Resolve(false);
                    reserves.Remove(ant.Target);
                    ant.Cancel();
                    continue;
                }
                reserves.Remove(ant.Target);
                ant.Source.Resolve(true);
                RemainingCellCount--;
                ant.ConfirmPickup();
            }
            if (pickups.Count > 0) navigation.Rebuild();
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i].State == AntTripState.Inactive)
                {
                    var finished = active[i];
                    active.RemoveAt(i);
                    if (Application.isPlaying) Destroy(finished.gameObject);
                    else DestroyImmediate(finished.gameObject);
                }
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                if (slots[i].AntCount == 0 && slots[i].OutgoingCount == 0)
                {
                    slots[i].gameObject.SetActive(false);
                    slots[i] = null;
                }
                else slots[i].Timer = Mathf.Min(spawnInterval, slots[i].Timer + dt);
            }
            IsBoardCleared = RemainingCellCount == 0;
            if (!IsBoardCleared) DispatchReadyBoxes();
            IsLevelComplete = IsBoardCleared && active.Count == 0;
            bool anyQueue = false;
            foreach (var queue in queues) if (queue.Count > 0) anyQueue = true;
            if (!anyQueue) SpeedMultiplier = 2;
            IsDeadlocked = !IsBoardCleared && Array.TrueForAll(slots, b => b != null) &&
                active.Count == 0 && !HasReachableSlotTarget();
        }
        private void RefreshCardPerimeter()
        {
            var next = MapPerimeter.CardBounds(mapView.Root, mapCardSurface);
            if ((next.center - cardBounds.center).sqrMagnitude < .000001f &&
                (next.size - cardBounds.size).sqrMagnitude < .000001f) return;
            var old = cardBounds;
            Vector3 Remap(Vector3 world)
            {
                var p = mapView.Root.InverseTransformPoint(world);
                bool horizontal = Mathf.Abs(p.z - old.min.z) < .01f || Mathf.Abs(p.z - old.max.z) < .01f;
                bool vertical = Mathf.Abs(p.x - old.min.x) < .01f || Mathf.Abs(p.x - old.max.x) < .01f;
                if (horizontal && p.x >= old.min.x - .01f && p.x <= old.max.x + .01f ||
                    vertical && p.z >= old.min.z - .01f && p.z <= old.max.z + .01f)
                {
                    p.x = vertical ? (Mathf.Abs(p.x - old.min.x) < .01f ? next.min.x : next.max.x)
                        : Mathf.Clamp(p.x, next.min.x, next.max.x);
                    p.z = horizontal ? (Mathf.Abs(p.z - old.min.z) < .01f ? next.min.z : next.max.z)
                        : Mathf.Clamp(p.z, next.min.z, next.max.z);
                    return mapView.Root.TransformPoint(p);
                }
                return world;
            }
            foreach (var ant in active) ant.RemapCardRoute(Remap);
            cardBounds = next;
            perimeter = new MapPerimeter(mapView.Root, mapView.Model, next);
            for (int i = 0; i < perimeterEntries.Length; i++)
                perimeterEntries[i].position = perimeter.BottomEntry(mapView.Root.InverseTransformPoint(boxAnchors[i].position).x);
            holeExit.position = perimeter.BottomEntry(0);
        }
        private bool Ready(BoxActor box) => box != null && box.SpawnableCount > 0 && box.Timer >= spawnInterval;
        private void DispatchReadyBoxes()
        {
            for (int dispatch = 0; dispatch < slots.Length; dispatch++)
            {
                TargetCandidate best = null;
                foreach (var cell in mapView.Model.EnumerateCells())
                {
                    if (cell.IsEmpty || reserves.ContainsKey(cell)) continue;
                    bool readyColor = false;
                    foreach (var box in slots) if (Ready(box) && box.ColorId == cell.ColorId) readyColor = true;
                    if (!readyColor) continue;
                    var candidate = navigation.Evaluate(cell);
                    if (candidate != null && (best == null || candidate.AccessWeight < best.AccessWeight))
                        best = candidate; // Enumeration already breaks ties by row then column.
                }
                if (best == null) return;
                int index = -1; float shortest = float.PositiveInfinity;
                TargetCandidate route = null;
                for (int i = 0; i < slots.Length; i++)
                {
                    var box = slots[i];
                    if (!Ready(box) || box.ColorId != best.Target.ColorId) continue;
                    var candidate = navigation.EvaluateFrom(best.Target, perimeter, perimeterEntries[i].position);
                    if (candidate == null) continue;
                    float distance = Vector3.Distance(Walking(boxAnchors[i].position), Walking(perimeterEntries[i].position))
                        + candidate.TravelDistance;
                    if (distance < shortest - .0001f) { shortest = distance; index = i; route = candidate; }
                }
                if (route == null) return;
                Spawn(index, route, route.EntryPoint);
            }
        }
        private Vector3 Walking(Vector3 point) { point.y = mapView.Root.position.y + .35f; return point; }
        private void Spawn(int index, TargetCandidate candidate, Vector3 border)
        {
            var box = slots[index];
            long id = ++nextTaskId;
            if (!box.Allocate()) return;
            reserves.Add(candidate.Target, id);
            try
            {
                var outbound = new List<Vector3> { Walking(perimeterEntries[index].position) };
                foreach (var p in perimeter.Route(perimeterEntries[index].position, border)) outbound.Add(Walking(p));
                var interior = navigation.BuildInteriorRoute(candidate);
                foreach (var cell in interior) outbound.Add(Walking(mapView.Root.TransformPoint(cell.Position)));
                // The card edge is outside the grid. A colored boundary cell still
                // needs an approach segment before the ant can pick it up.
                if (candidate.DirectBorderAccess)
                {
                    var target = Walking(mapView.Root.TransformPoint(candidate.Target.Position));
                    var outward = Walking(border) - target;
                    outbound.Add(target + outward.normalized * 1.1f);
                }
                var returning = new List<Vector3>();
                if (candidate.DirectBorderAccess) returning.Add(outbound[outbound.Count - 1]);
                for (int i = interior.Count - 1; i >= 0; i--)
                    returning.Add(Walking(mapView.Root.TransformPoint(interior[i].Position)));
                returning.Add(Walking(border));
                foreach (var p in perimeter.Route(border, holeExit.position)) returning.Add(Walking(p));
                returning.Add(Walking(holeReturn.position));
                var ant = Instantiate(antPrefab, runtimeRoot, false);
                ant.Begin(id, box, candidate.Target, Walking(boxAnchors[index].position), outbound,
                    returning, holeJump.position, mapView.Model.GetColor(candidate.Target.ColorId));
                active.Add(ant);
                box.Timer = 0;
            }
            catch
            {
                box.Resolve(false); reserves.Remove(candidate.Target); throw;
            }
        }
        private bool HasReachableSlotTarget()
        {
            foreach (var cell in mapView.Model.EnumerateCells())
            {
                if (cell.IsEmpty || reserves.ContainsKey(cell) || navigation.Evaluate(cell) == null) continue;
                foreach (var box in slots)
                    if (box != null && box.SpawnableCount > 0 && box.ColorId == cell.ColorId) return true;
            }
            return false;
        }
        public void TogglePause() => Paused = !Paused;
        public void ToggleSpeed() => SpeedMultiplier = SpeedMultiplier == 1 ? 2 : 1;
        // IMGUI pointer events work without legacy Input polling or changing the project's input backend.
        private void OnGUI()
        {
            if (!initialized || gameplayCamera == null) return;
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var screen = new Vector3(e.mousePosition.x, Screen.height - e.mousePosition.y, 0);
                if (HandlePointer(screen)) e.Use();
            }
            string status = IsLevelComplete ? "WIN" : IsBoardCleared ? "Ants returning..." :
                IsDeadlocked ? "No reachable color - Restart" : Paused ? "Paused" : $"Bricks: {RemainingCellCount} | x{SpeedMultiplier}";
            GUI.Label(new Rect(12, Screen.height - 30, Screen.width - 110, 24), status);
            if (GUI.Button(new Rect(Screen.width - 90, Screen.height - 32, 80, 26), "Restart")) Restart();
        }
        public bool HandlePointer(Vector2 screen)
        {
            if (!initialized || gameplayCamera == null) return false;
            var vp = gameplayCamera.ScreenToViewportPoint(screen);
            if (vp.y > .9f && vp.x < .12f) { TogglePause(); return true; }
            if (vp.y > .9f && vp.x > .77f) { ToggleSpeed(); return true; }
            if (vp.y <= .11f || vp.y >= .32f) return false;
            Physics.SyncTransforms();
            if (!Physics.Raycast(gameplayCamera.ScreenPointToRay(screen), out var hit)) return false;
            var actor = hit.collider.GetComponentInParent<BoxActor>();
            if (actor == null || actor.SlotIndex >= 0 || actor.QueueIndex < 0 ||
                actor.QueueIndex >= queues.Count || queues[actor.QueueIndex].Count == 0 ||
                queues[actor.QueueIndex][0] != actor) return false;
            return PickQueue(actor.QueueIndex);
        }
        private void Cleanup()
        {
            initialized = false;
            queues.Clear(); active.Clear(); reserves.Clear(); pickups.Clear();
            if (runtimeRoot != null)
            {
                runtimeRoot.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(runtimeRoot.gameObject);
                else DestroyImmediate(runtimeRoot.gameObject);
            }
            runtimeRoot = null;
        }
        private void OnDestroy() => Cleanup();
    }
}
