using System;
using System.Collections.Generic;
using UnityEngine;
using ColonyFlow.Core.Tweening;
using ColonyFlow.Core.Pooling;

namespace ColonyFlow.Gameplay
{
    public sealed partial class AntGameplay : MonoBehaviour
    {
        [SerializeField] private MapView mapView;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private BoxActor boxPrefab;
        [SerializeField] private AntActor antPrefab;
        [SerializeField] private Transform[] boxAnchors;
        [SerializeField] private Transform[] antSpawnPoints;
        [SerializeField] private Transform[] perimeterEntries;
        [SerializeField] private Renderer[] slotSurfaces;
        [SerializeField] private Transform[] queueAnchors;
        [SerializeField] private Transform holeReturn;
        [SerializeField] private Transform holeJump;
        [SerializeField] private Transform holeExit;
        [SerializeField] private Collider holeObstacle;
        [SerializeField] private Renderer mapCardSurface;
        [SerializeField] private Material colorMaterialTemplate;
        [SerializeField] private float spawnInterval = .22f;
        [SerializeField] private float antSpeed = 8;
        [SerializeField, Min(.001f)] private float layoutUnit = 1;
        [SerializeField, Min(.1f)] private float pickupApproachDistance = 1.6f;
        [SerializeField] private float queueColumnStep = 1.57f;
        [SerializeField] private float queueRowStep = 1.86f;
        [SerializeField, Min(.01f)] private float holeApproachDistance = 12;
        [SerializeField, Min(.01f)] private float holeJumpDistance = 3;
        private readonly List<List<BoxActor>> queues = new List<List<BoxActor>>();
        private readonly List<AntActor> active = new List<AntActor>();
        private readonly Dictionary<Cell, long> reserves = new Dictionary<Cell, long>();
        private readonly Dictionary<Collider, BoxActor> colliderBoxes = new Dictionary<Collider, BoxActor>();
        private readonly List<AntActor> pickups = new List<AntActor>();
        private readonly List<BoxActor> pendingBoxes = new List<BoxActor>();
        private ObjectPoolManager pool;
        private SharedColorMaterialCache colorMaterials;
        private SharedColorMaterialCache boxColorMaterials;
        private SharedColorMaterialCache boxBodyMaterials;
        private BoxActor[] slots;
        private Transform runtimeRoot;
        private GridNavigation navigation;
        private MapPerimeter perimeter;
        private Bounds cardBounds;
        private long nextTaskId;
        private float dispatchBudget;
        private bool initialized;
        private bool resultReported;
        private bool initializeWhenMapEnabled;
        private bool HasCurrentSession => initialized && mapView != null && mapView.isActiveAndEnabled &&
            mapView.Model != null && navigation != null && navigation.Map == mapView.Model && pool != null;
        public bool IsBoardCleared { get; private set; }
        public bool IsLevelComplete { get; private set; }
        public bool IsDeadlocked { get; private set; }
        public bool Paused { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1;
        public int ActiveTripCount => active.Count;
        public int RemainingCellCount => mapView.Model?.ColoredCellCount ?? 0;
        public GridNavigation Navigation => navigation;
        public IReadOnlyList<AntActor> ActiveAnts => active;
        public IReadOnlyList<BoxActor> Slots => slots;
        public string StatusText => IsLevelComplete ? "WIN" : IsBoardCleared ? "Ants returning..." :
            IsDeadlocked ? "No reachable color" : Paused ? "Paused" : "Bricks: " + RemainingCellCount;
        public event Action LevelCompleted;
        public event Action LevelFailed;
        public event Action BoxSelected;
        public event Action AntPickupCompleted;
        public event Action BoosterUsed;

        public void Configure(MapView map, Camera camera, BoxActor box, AntActor ant,
            Transform[] anchors, Transform[] spawns, Transform[] entries, Transform[] queuePoints,
            Transform returning, Transform jumping, Transform exit)
        {
            mapView = map;
            gameplayCamera = camera;
            boxPrefab = box;
            antPrefab = ant;
            boxAnchors = anchors;
            antSpawnPoints = spawns;
            perimeterEntries = entries;
            queueAnchors = queuePoints;
            holeReturn = returning;
            holeJump = jumping;
            holeExit = exit;
            holeObstacle = FindHoleObstacle();
        }

        public void ConfigureLayoutUnit(float unit)
        {
            layoutUnit = unit;
            antSpeed = 8 * unit;
        }

        public void ConfigureBoxLayout(float columnStep, float rowStep)
        {
            queueColumnStep = columnStep;
            queueRowStep = rowStep;
        }

        public void ConfigureCard(Renderer surface) => mapCardSurface = surface;
        public void ConfigureSlotSurfaces(Renderer[] surfaces) => slotSurfaces = surfaces;
        public void ConfigureColorMaterial(Material material) => colorMaterialTemplate = material;

        private void Start()
        {
            if (!initialized && !initializeWhenMapEnabled) Initialize();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (mapView != null && mapView.isActiveAndEnabled) Initialize();
            else initializeWhenMapEnabled = true;
        }

        private void OnDisable()
        {
            Cleanup();
            pool?.Dispose();
            pool = null;
        }

        public bool Initialize()
        {
            Cleanup();
            if (Application.isPlaying && mapView != null && !mapView.isActiveAndEnabled)
            {
                initializeWhenMapEnabled = true;
                return false;
            }
            if (Application.isPlaying && !TweenRuntime.Initialize(new TweenSettings())) return false;
            if (!ResolveHoleObstacle()) return false;
            if (!HasReferences()) return false;
            if (mapView.Model == null && !mapView.LoadMap()) return false;
            if (mapView.Model.Queues.Count > queueAnchors.Length) return false;
            PreparePools();
            navigation = new GridNavigation(mapView.Model);
            CreatePerimeter();
            CreateRuntimeRoot();
            colorMaterials = new SharedColorMaterialCache(colorMaterialTemplate != null
                ? colorMaterialTemplate : antPrefab.ColorMaterialTemplate);
            boxColorMaterials = new SharedColorMaterialCache(boxPrefab.ColorMaterialTemplate != null
                ? boxPrefab.ColorMaterialTemplate : colorMaterialTemplate);
            boxBodyMaterials = new SharedColorMaterialCache(boxPrefab.BodyMaterialTemplate != null
                ? boxPrefab.BodyMaterialTemplate
                : boxPrefab.ColorMaterialTemplate != null ? boxPrefab.ColorMaterialTemplate : colorMaterialTemplate);
            CreateQueues();
            RefreshQueues();
            ResetSession();
            return true;
        }

        private void PreparePools()
        {
            if (pool == null) pool = new ObjectPoolManager(transform);
            pool.Load(antPrefab, 32);
            int count = 0;
            foreach (var queue in mapView.Model.Queues) count += queue.boxes.Length;
            pool.Load(boxPrefab, count);
        }

        private bool HasReferences() =>
            mapView != null && gameplayCamera != null && boxPrefab != null && antPrefab != null &&
            mapCardSurface != null && holeReturn != null && holeJump != null && holeExit != null && holeObstacle != null &&
            boxAnchors != null && antSpawnPoints != null && perimeterEntries != null && queueAnchors != null &&
            boxAnchors.Length == antSpawnPoints.Length && boxAnchors.Length == perimeterEntries.Length &&
            Array.TrueForAll(boxAnchors, p => p != null) && Array.TrueForAll(antSpawnPoints, p => p != null) &&
            Array.TrueForAll(perimeterEntries, p => p != null) && Array.TrueForAll(queueAnchors, p => p != null);

        private void CreatePerimeter()
        {
            cardBounds = MapPerimeter.CardBounds(mapView.Root, mapCardSurface);
            perimeter = new MapPerimeter(mapView.Root, mapView.Model, cardBounds);
            RefreshEntries();
        }

        private void RefreshEntries()
        {
            for (int i = 0; i < perimeterEntries.Length; i++)
                perimeterEntries[i].position = perimeter.BottomEntry(mapView.Root.InverseTransformPoint(boxAnchors[i].position).x);
            holeExit.position = perimeter.BottomEntry(0);
        }

        private void CreateRuntimeRoot()
        {
            slots = new BoxActor[boxAnchors.Length];
            runtimeRoot = new GameObject("Runtime Boxes and Ants").transform;
            runtimeRoot.SetParent(transform, false);
        }

        private void CreateQueues()
        {
            for (int q = 0; q < mapView.Model.Queues.Count; q++)
            {
                var queue = new List<BoxActor>();
                queues.Add(queue);
                foreach (var data in mapView.Model.Queues[q].boxes) queue.Add(CreateBox(q, data));
            }
        }

        private BoxActor CreateBox(int queueIndex, BoxData data)
        {
            var box = pool.Spawn(boxPrefab, parent: runtimeRoot, spawnInWorldSpace: false);
            Material colorMaterial = boxColorMaterials.Get(data.colorId,
                mapView.Model.GetColor(data.colorId));
            Material bodyMaterial = boxBodyMaterials.Get(data.colorId,
                mapView.Model.GetColor(data.colorId));
            box.Initialize(data.colorId, data.antCount, queueIndex,
                colorMaterial, bodyMaterial, gameplayCamera, data.kind);
            FitBoxToLayout(box);
            colliderBoxes.Add(box.HitCollider, box);
            return box;
        }

        private void ResetSession()
        {
            initialized = true;
            Paused = false;
            SpeedMultiplier = 1;
            nextTaskId = 0;
            dispatchBudget = 1;
            resultReported = false;
            IsBoardCleared = RemainingCellCount == 0;
            IsLevelComplete = IsBoardCleared;
            IsDeadlocked = false;
        }

        [ContextMenu("Restart Level")]
        public void Restart()
        {
            Cleanup();
            if (mapView.LoadMap()) Initialize();
        }

        public bool PickQueue(int queueIndex)
        {
            if (!CanPickQueue(queueIndex)) return false;
            int slot = Array.FindIndex(slots, x => x == null);
            if (slot < 0) return false;
            MoveBoxToSlot(queueIndex, 0, slot);
            RefreshQueues(true);
            BoxSelected?.Invoke();
            return true;
        }

        private bool CanPickQueue(int index) =>
            HasCurrentSession && !Paused && !IsBoardCleared && !IsDeadlocked &&
            index >= 0 && index < queues.Count && queues[index].Count > 0;

        private void MoveBoxToSlot(int queueIndex, int boxIndex, int slot)
        {
            var box = queues[queueIndex][boxIndex];
            queues[queueIndex].RemoveAt(boxIndex);
            box.SlotIndex = slot;
            box.Timer = spawnInterval;
            box.gameObject.SetActive(true);
            slots[slot] = box;
            box.JumpToSlot(SlotLandingPosition(box, slot), layoutUnit);
        }

        private void RefreshQueues(bool animate = false)
        {
            int nonempty = 0;
            foreach (var queue in queues) if (queue.Count > 0) nonempty++;
            int visible = 0;
            for (int q = 0; q < queues.Count; q++)
            {
                if (queues[q].Count == 0) continue;
                float x = (visible++ - (nonempty - 1) * .5f) * queueColumnStep;
                PositionQueue(q, mapView.Root.position.x + x, animate);
            }
        }

        private void PositionQueue(int index, float x, bool animate)
        {
            for (int i = 0; i < queues[index].Count; i++)
            {
                var point = queueAnchors[index].position;
                point.x = x;
                point.z -= i * queueRowStep;
                var box = queues[index][i];
                bool visible = i < 3;
                box.gameObject.SetActive(visible);
                box.PlaceInQueue(point, i == 0, animate && visible);
            }
        }

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float delta)
        {
            if (initializeWhenMapEnabled && mapView != null && mapView.isActiveAndEnabled)
            {
                initializeWhenMapEnabled = false;
                Initialize();
            }
            if (!initialized || !mapView.isActiveAndEnabled)
            {
                CancelBoosterSelection();
                return;
            }
            if (navigation == null || navigation.Map != mapView.Model) { Initialize(); return; }
            if (Paused || IsLevelComplete || delta <= 0) return;
            RefreshCardPerimeter();
            float dt = delta * SpeedMultiplier;
            AdvanceQueues(dt);
            AdvanceAnts(delta, SpeedMultiplier);
            ResolvePickups();
            RemoveFinishedAnts();
            AdvanceSlots(dt);
            ResolvePendingBoxes(dt);
            if (RemainingCellCount > 0) DispatchReadyBoxes(dt);
            RefreshLevelState();
        }

        private void AdvanceAnts(float animationDelta, float speedMultiplier)
        {
            pickups.Clear();
            foreach (var ant in active)
            {
                ant.Advance(animationDelta, animationDelta * antSpeed * speedMultiplier);
                if (ant.State == AntTripState.WaitingPickup) pickups.Add(ant);
            }
            // Collection order is independent of movement callbacks.
            pickups.Sort((a, b) => a.TaskId.CompareTo(b.TaskId));
        }

        private void AdvanceQueues(float delta)
        {
            foreach (var queue in queues)
                foreach (var box in queue)
                    if (box.gameObject.activeSelf) box.AdvanceAnimation(delta);
        }

        private void ResolvePickups()
        {
            foreach (var ant in pickups) ResolvePickup(ant);
            if (pickups.Count > 0) navigation.Rebuild();
        }

        private void ResolvePickup(AntActor ant)
        {
            if (!reserves.TryGetValue(ant.Target, out var owner) || owner != ant.TaskId ||
                !mapView.Collect(ant.Target))
            {
                CancelPickup(ant);
                return;
            }
            reserves.Remove(ant.Target);
            if (ant.Source != null) ant.Source.Resolve(true);
            ant.DetachSource();
            ant.ConfirmPickup(mapView.GetCellVisualPosition(ant.Target),
                Vector3.Scale(mapView.Model.CellScale, mapView.Root.lossyScale));
            AntPickupCompleted?.Invoke();
        }

        private void CancelPickup(AntActor ant)
        {
            if (ant.Source != null) ant.Source.Resolve(false);
            if (reserves.TryGetValue(ant.Target, out var owner) && owner == ant.TaskId) reserves.Remove(ant.Target);
            ant.Cancel();
        }

        private void RemoveFinishedAnts()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].State != AntTripState.Inactive) continue;
                var finished = active[i];
                active.RemoveAt(i);
                pool.Recycle(finished);
            }
        }

        private void AdvanceSlots(float delta)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var box = slots[i];
                if (box == null) continue;
                box.AdvanceAnimation(delta);
                if (box.IsLanding) continue;
                if (box.AntCount == 0 && box.OutgoingCount == 0) ReleaseBox(i);
                else box.Timer = Mathf.Min(spawnInterval, box.Timer + delta);
            }
        }

        private void RefreshLevelState()
        {
            IsBoardCleared = RemainingCellCount == 0;
            IsLevelComplete = IsBoardCleared && active.Count == 0;
            if (!HasQueuedBoxes()) SpeedMultiplier = 2;
            IsDeadlocked = !IsBoardCleared && active.Count == 0 &&
                (!HasBudgetSupply() || Array.TrueForAll(slots, b => b != null) && !HasReachableSlotTarget());
            ReportResult();
        }

        private void ReportResult()
        {
            if (resultReported) return;
            if (IsLevelComplete)
            {
                resultReported = true;
                LevelCompleted?.Invoke();
            }
            else if (IsDeadlocked)
            {
                resultReported = true;
                LevelFailed?.Invoke();
            }
        }

        private bool HasBudgetSupply()
        {
            foreach (var queue in queues) foreach (var box in queue) if (box.AntCount > 0) return true;
            foreach (var box in slots) if (box != null && box.AntCount > 0) return true;
            foreach (var box in pendingBoxes) if (box.AntCount > 0) return true;
            return false;
        }

        private bool HasQueuedBoxes()
        {
            foreach (var queue in queues) if (queue.Count > 0) return true;
            return false;
        }

        private void RefreshCardPerimeter()
        {
            var next = MapPerimeter.CardBounds(mapView.Root, mapCardSurface);
            if ((next.center - cardBounds.center).sqrMagnitude < .000001f &&
                (next.size - cardBounds.size).sqrMagnitude < .000001f) return;
            var old = cardBounds;
            cardBounds = next;
            perimeter = new MapPerimeter(mapView.Root, mapView.Model, next);
            foreach (var ant in active) ant.RemapCardRoute(world => perimeter.RemapFrom(world, old));
            RefreshEntries();
        }

        private bool Ready(BoxActor box) => box != null && !box.IsLanding && box.AntCount > 0 && box.Timer >= spawnInterval;

        private void DispatchReadyBoxes(float delta)
        {
            dispatchBudget = AccumulateDispatchBudget(dispatchBudget, delta, CountDispatchSources(), spawnInterval);
            if (dispatchBudget < 1) return;
            var route = FindNearestTarget(out int index);
            if (route == null) return;
            Spawn(index, route);
            dispatchBudget = 0;
        }

        private int CountDispatchSources()
        {
            int count = 0;
            foreach (var box in slots)
                if (box != null && box.AntCount > 0) count++;
            return count;
        }

        private static float AccumulateDispatchBudget(float current, float delta, int sourceCount, float interval)
        {
            if (delta <= 0 || sourceCount <= 0 || interval <= 0) return Mathf.Clamp01(current);
            return Mathf.Min(1, current + delta * sourceCount / interval);
        }

        private sealed class TargetAssignment
        {
            public int SlotIndex;
            public TargetCandidate Candidate;
            public float Distance;
        }

        private TargetCandidate FindNearestTarget(out int index)
        {
            index = -1;
            var assignments = new List<TargetAssignment>();
            foreach (var cell in mapView.Model.EnumerateCells())
            {
                if (cell.IsEmpty || reserves.ContainsKey(cell) || !navigation.CanReach(cell)) continue;
                for (int i = 0; i < slots.Length; i++)
                {
                    var box = slots[i];
                    if (box == null || box.AntCount <= 0 || box.ColorId != cell.ColorId) continue;
                    var candidate = navigation.EvaluateFrom(cell, perimeter, perimeterEntries[i].position, pickupApproachDistance * layoutUnit);
                    if (candidate == null) continue;
                    float distance = Vector3.Distance(SpawnPosition(i), Walking(perimeterEntries[i].position)) + candidate.TravelDistance;
                    assignments.Add(new TargetAssignment { SlotIndex = i, Candidate = candidate, Distance = distance });
                }
            }

            assignments.Sort(CompareAssignments);
            var assignedSlots = new bool[slots.Length];
            var assignedTargets = new HashSet<Cell>();
            foreach (var assignment in assignments)
            {
                if (assignedSlots[assignment.SlotIndex] || assignedTargets.Contains(assignment.Candidate.Target)) continue;
                assignedSlots[assignment.SlotIndex] = true;
                assignedTargets.Add(assignment.Candidate.Target);
                if (!Ready(slots[assignment.SlotIndex])) continue;
                index = assignment.SlotIndex;
                return assignment.Candidate;
            }
            return null;
        }

        private static int CompareAssignments(TargetAssignment a, TargetAssignment b)
        {
            if (Mathf.Abs(a.Distance - b.Distance) > .0001f) return a.Distance.CompareTo(b.Distance);
            int slot = a.SlotIndex.CompareTo(b.SlotIndex);
            if (slot != 0) return slot;
            int row = b.Candidate.Target.Row.CompareTo(a.Candidate.Target.Row);
            return row != 0 ? row : a.Candidate.Target.Column.CompareTo(b.Candidate.Target.Column);
        }

        private Vector3 Walking(Vector3 point)
        {
            point.y = mapView.Root.position.y + .35f * layoutUnit;
            return point;
        }

        private Vector3 SpawnPosition(int index) => Walking(antSpawnPoints[index].position);

        private void Spawn(int index, TargetCandidate candidate)
        {
            var box = slots[index];
            if (!box.Allocate()) return;
            long id = ++nextTaskId;
            reserves.Add(candidate.Target, id);
            var interior = navigation.BuildInteriorRoute(candidate);
            var outbound = BuildOutbound(index, candidate, interior);
            var returning = BuildReturn(candidate, interior, outbound[outbound.Count - 1]);
            var ant = pool.Spawn(antPrefab, parent: runtimeRoot, spawnInWorldSpace: false);
            ant.Begin(id, box, candidate.Target, SpawnPosition(index), outbound, returning,
                Walking(perimeter.CellPoint(candidate.Target)),
                holeJump.position, colorMaterials.Get(candidate.Target.ColorId,
                    mapView.Model.GetColor(candidate.Target.ColorId)));
            active.Add(ant);
            box.Timer = 0;
            if (box.AntCount == 0) ReleaseBox(index);
        }

        private List<Vector3> BuildOutbound(int index, TargetCandidate candidate, List<Cell> interior)
        {
            Vector3 spawn = SpawnPosition(index);
            Vector3 directEntry = Walking(perimeterEntries[index].position);
            var points = new List<Vector3>();
            Vector3 perimeterStart;
            if (TryGetHoleAvoidancePoints(spawn, directEntry, out var approach, out var avoidance))
            {
                points.Add(approach);
                points.Add(avoidance);
                perimeterStart = StraightPerimeterEntry(avoidance);
                points.Add(perimeterStart);
            }
            else
            {
                perimeterStart = directEntry;
                points.Add(perimeterStart);
            }
            AppendPerimeter(points, perimeterStart, candidate.EntryPoint);
            foreach (var cell in interior) points.Add(Walking(perimeter.CellPoint(cell)));
            Vector3 approachFrom = candidate.DirectBorderAccess
                ? Walking(candidate.EntryPoint)
                : points[points.Count - 1];
            points.Add(PickupApproach(candidate, approachFrom));
            return points;
        }

        private bool TryGetHoleAvoidancePoints(Vector3 from, Vector3 to,
            out Vector3 approach, out Vector3 avoidance)
        {
            approach = default;
            avoidance = default;
            if (holeObstacle == null || !holeObstacle.enabled) return false;
            var bounds = holeObstacle.bounds;
            var rayStart = new Vector3(from.x, bounds.center.y, from.z);
            var rayEnd = new Vector3(to.x, bounds.center.y, to.z);
            var direction = rayEnd - rayStart;
            float distance = direction.magnitude;
            if (distance <= .0001f) return false;
            var rayDirection = direction / distance;
            if (!holeObstacle.Raycast(new Ray(rayStart, rayDirection), out var hit, distance))
                return false;

            var localBounds = HoleBoundsInMapSpace(bounds);
            float clearance = antPrefab.HoleAvoidanceOffset * layoutUnit;
            float turnLead = antPrefab.HoleTurnLeadDistance * layoutUnit;
            approach = Walking(hit.point - rayDirection * turnLead);
            var localApproach = mapView.Root.InverseTransformPoint(approach);
            var localDirection = mapView.Root.InverseTransformDirection(rayDirection);
            var localRight = Vector3.Cross(Vector3.up,
                new Vector3(localDirection.x, 0, localDirection.z)).normalized;
            float localOffset = clearance / Mathf.Abs(mapView.Root.lossyScale.x);
            localApproach.x = localRight.x >= 0
                ? localBounds.max.x + localOffset
                : localBounds.min.x - localOffset;
            avoidance = Walking(mapView.Root.TransformPoint(localApproach));
            return true;
        }

        private Bounds HoleBoundsInMapSpace(Bounds worldBounds)
        {
            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 8; i++)
            {
                var corner = worldBounds.center + Vector3.Scale(worldBounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var local = mapView.Root.InverseTransformPoint(corner);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
            return new Bounds((min + max) * .5f, max - min);
        }

        private Vector3 StraightPerimeterEntry(Vector3 from)
        {
            float localX = mapView.Root.InverseTransformPoint(from).x;
            return Walking(perimeter.BottomEntry(localX));
        }

        private Collider FindHoleObstacle()
        {
            if (holeReturn == null || holeReturn.parent == null) return null;
            var rim = holeReturn.parent.Find("Hole Rim");
            return rim != null ? rim.GetComponent<Collider>() : null;
        }

        private bool ResolveHoleObstacle()
        {
            if (holeObstacle != null) return true;
            if (holeReturn == null || holeReturn.parent == null) return false;
            var rim = holeReturn.parent.Find("Hole Rim");
            if (rim == null) return false;
            holeObstacle = rim.GetComponent<Collider>();
            if (holeObstacle != null) return true;
            var filter = rim.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return false;
            var meshCollider = rim.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = filter.sharedMesh;
            holeObstacle = meshCollider;
            return true;
        }

        private Vector3 PickupApproach(TargetCandidate candidate, Vector3 approachFrom)
        {
            var target = Walking(perimeter.CellPoint(candidate.Target));
            var outward = approachFrom - target;
            float availableDistance = outward.magnitude;
            if (availableDistance <= .0001f) return approachFrom;
            // Never place the pickup endpoint beyond the incoming waypoint. Doing so makes an ant
            // walk away from the brick and immediately turn back to face it.
            float distance = Mathf.Min(pickupApproachDistance * layoutUnit, availableDistance);
            return target + outward / availableDistance * distance;
        }

        private List<Vector3> BuildReturn(TargetCandidate candidate, List<Cell> interior, Vector3 pickup)
        {
            var points = new List<Vector3>();
            if (candidate.DirectBorderAccess) points.Add(pickup);
            for (int i = interior.Count - 1; i >= 0; i--) points.Add(Walking(perimeter.CellPoint(interior[i])));
            points.Add(Walking(candidate.EntryPoint));
            var approach = candidate.EntryPoint;
            if (!perimeter.IsBottomEdge(candidate.EntryPoint))
            {
                approach = HoleApproachPoint(candidate.EntryPoint);
                AppendPerimeter(points, candidate.EntryPoint, approach);
            }
            var hole = Walking(holeReturn.position);
            points.Add(hole + (Walking(approach) - hole).normalized * (holeJumpDistance * layoutUnit));
            return points;
        }

        private Vector3 HoleApproachPoint(Vector3 entry)
        {
            var hole = Walking(holeReturn.position);
            var localHole = mapView.Root.InverseTransformPoint(hole);
            var exit = Walking(perimeter.BottomEntry(localHole.x));
            float gap = Vector3.Distance(exit, hole);
            float radius = holeApproachDistance * layoutUnit;
            float offset = Mathf.Sqrt(Mathf.Max(layoutUnit * layoutUnit, radius * radius - gap * gap));
            float side = mapView.Root.InverseTransformPoint(entry).x < localHole.x ? -1 : 1;
            return Walking(perimeter.BottomEntry(localHole.x + side * offset / mapView.Root.lossyScale.x));
        }

        private void AppendPerimeter(List<Vector3> points, Vector3 from, Vector3 to)
        {
            foreach (var point in perimeter.Route(from, to)) points.Add(Walking(point));
        }

        private void ReleaseBox(int index)
        {
            var box = slots[index];
            slots[index] = null;
            colliderBoxes.Remove(box.HitCollider);
            box.SlotIndex = -1;
            box.Disappear();
            pendingBoxes.Add(box);
        }

        private void ResolvePendingBoxes(float delta = 0f)
        {
            for (int i = pendingBoxes.Count - 1; i >= 0; i--)
            {
                var box = pendingBoxes[i];
                box.AdvanceAnimation(delta);
                if (box.IsDisappearing) continue;
                if (box.AntCount > 0)
                {
                    int slot = Array.FindIndex(slots, b => b == null);
                    if (slot < 0) continue;
                    pendingBoxes.RemoveAt(i);
                    box.SlotIndex = slot;
                    box.Timer = spawnInterval;
                    slots[slot] = box;
                    colliderBoxes.Add(box.HitCollider, box);
                    box.gameObject.SetActive(true);
                    box.JumpToSlot(SlotLandingPosition(box, slot), layoutUnit);
                }
                else if (box.OutgoingCount == 0)
                {
                    pendingBoxes.RemoveAt(i);
                    pool.Recycle(box);
                }
            }
        }

        private bool HasReachableSlotTarget()
        {
            foreach (var cell in mapView.Model.EnumerateCells())
            {
                if (cell.IsEmpty || reserves.ContainsKey(cell) || !navigation.CanReach(cell)) continue;
                foreach (var box in slots)
                    if (box != null && box.AntCount > 0 && box.ColorId == cell.ColorId) return true;
            }
            return false;
        }

        public void TogglePause() => Paused = !Paused;
        public void ToggleSpeed() => SpeedMultiplier = SpeedMultiplier == 1 ? 2 : 1;

        public bool HandlePointer(Vector2 screen)
        {
            if (!HasCurrentSession) return false;
            var vp = gameplayCamera.ScreenToViewportPoint(screen);
            if (IsSelectingBlow) return false;
            if (vp.y <= .045f || vp.y >= .875f) return false;
            return PickBoxAt(screen);
        }

        private bool PickBoxAt(Vector2 screen)
        {
            Physics.SyncTransforms();
            if (!Physics.Raycast(gameplayCamera.ScreenPointToRay(screen), out var hit) ||
                !colliderBoxes.TryGetValue(hit.collider, out var actor)) return false;
            if (IsSelectingPickup) return PickupBox(actor);
            if (actor.SlotIndex >= 0 || !CanPickQueue(actor.QueueIndex) || queues[actor.QueueIndex][0] != actor) return false;
            if (Array.FindIndex(slots, item => item == null) < 0)
            {
                actor.PlayBlockedPickFeedback();
                return false;
            }
            return PickQueue(actor.QueueIndex);
        }

        private void Cleanup()
        {
            initialized = false;
            initializeWhenMapEnabled = false;
            CancelBoosterSelection();
            RecycleSession();
            queues.Clear();
            active.Clear();
            reserves.Clear();
            colliderBoxes.Clear();
            pickups.Clear();
            pendingBoxes.Clear();
            RestoreSlotLayout();
            if (runtimeRoot != null) DisposeObject(runtimeRoot.gameObject);
            runtimeRoot = null;
            slots = null;
            navigation = null;
            perimeter = null;
            colorMaterials?.Dispose();
            colorMaterials = null;
            boxColorMaterials?.Dispose();
            boxColorMaterials = null;
            boxBodyMaterials?.Dispose();
            boxBodyMaterials = null;
        }

        private void RecycleSession()
        {
            if (pool == null) return;
            foreach (var ant in active) pool.Recycle(ant);
            foreach (var queue in queues) foreach (var box in queue) pool.Recycle(box);
            if (slots != null) foreach (var box in slots) pool.Recycle(box);
            foreach (var box in pendingBoxes) pool.Recycle(box);
        }

        private static void DisposeObject(GameObject obj)
        {
            obj.SetActive(false);
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private void OnDestroy()
        {
            Cleanup();
            pool?.Dispose();
        }
    }
}
