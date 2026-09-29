using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class TargetCandidate
    {
        public Cell Target { get; internal set; }
        public Cell AccessCell { get; internal set; }
        public bool DirectBorderAccess => AccessCell == null;
        internal List<Cell> InteriorRoute;
        public Vector3 EntryPoint { get; internal set; }
        public float InteriorDistance { get; internal set; }
        public float TravelDistance { get; internal set; }
    }

    public sealed class GridNavigation
    {
        private const float Epsilon = .0001f;
        private readonly MapModel map;
        private readonly int[,] weights;
        private readonly int[] dr = { 1, 0, 0, -1 };
        private readonly int[] dc = { 0, -1, 1, 0 };
        private readonly Dictionary<Vector3, Search> searches = new Dictionary<Vector3, Search>();
        private MapPerimeter searchPerimeter;
        public MapModel Map => map;

        private sealed class Search
        {
            public readonly float[] Distance;
            public readonly float[] PerimeterDistance;
            public readonly int[] Previous;
            public readonly int[] Steps;
            public readonly Vector3[] Edge;
            public readonly bool[] Visited;
            public Search(int count)
            {
                Distance = new float[count];
                PerimeterDistance = new float[count];
                Previous = new int[count];
                Steps = new int[count];
                Edge = new Vector3[count];
                Visited = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    Distance[i] = float.PositiveInfinity;
                    PerimeterDistance[i] = float.PositiveInfinity;
                    Previous[i] = -1;
                    Steps[i] = int.MaxValue;
                }
            }
        }

        public GridNavigation(MapModel model)
        {
            map = model;
            weights = new int[map.Rows, map.Columns];
            Rebuild();
        }

        public bool IsBorder(Cell cell) => cell.Row == 0 || cell.Column == 0 ||
            cell.Row == map.Rows - 1 || cell.Column == map.Columns - 1;
        public int NavigationWeight(Cell cell) => cell.IsEmpty ? weights[cell.Row, cell.Column] : -1;

        public void Rebuild()
        {
            searches.Clear();
            var queue = SeedReachability();
            while (queue.Count > 0) ExpandReachability(queue.Dequeue(), queue);
        }

        private Queue<Cell> SeedReachability()
        {
            var queue = new Queue<Cell>();
            foreach (var cell in map.EnumerateCells())
            {
                bool entry = cell.IsEmpty && IsBorder(cell);
                weights[cell.Row, cell.Column] = entry ? 1 : -1;
                if (entry) queue.Enqueue(cell);
            }
            return queue;
        }

        private void ExpandReachability(Cell cell, Queue<Cell> queue)
        {
            for (int d = 0; d < 4; d++)
            {
                var next = Neighbor(cell, d);
                if (next == null || !next.IsEmpty || NavigationWeight(next) >= 0) continue;
                weights[next.Row, next.Column] = NavigationWeight(cell) + 1;
                queue.Enqueue(next);
            }
        }

        private Cell Neighbor(Cell cell, int direction) =>
            map.GetCell(cell.Row + dr[direction], cell.Column + dc[direction]);

        public bool CanReach(Cell target)
        {
            if (target == null || target.IsEmpty) return false;
            if (IsBorder(target)) return true;
            for (int d = 0; d < 4; d++)
            {
                var next = Neighbor(target, d);
                if (next != null && NavigationWeight(next) > 0) return true;
            }
            return false;
        }

        // Minimize the trip from the card edge to the brick, then the perimeter trip.
        public TargetCandidate EvaluateFrom(Cell target, MapPerimeter perimeter, Vector3 entry, float pickupDistance = 1.1f)
        {
            if (!CanReach(target)) return null;
            var straight = StraightApproach(target, perimeter, entry, pickupDistance);
            var throughEmptyCells = FindAccess(target, SearchFrom(perimeter, entry), perimeter, entry, pickupDistance);
            return Better(throughEmptyCells, straight) ? throughEmptyCells : straight;
        }

        private static bool Better(TargetCandidate candidate, TargetCandidate current) =>
            candidate != null && (current == null ||
                candidate.InteriorDistance < current.InteriorDistance - Epsilon ||
                Mathf.Abs(candidate.InteriorDistance - current.InteriorDistance) <= Epsilon &&
                candidate.TravelDistance < current.TravelDistance - Epsilon);

        private TargetCandidate StraightApproach(Cell target, MapPerimeter perimeter, Vector3 entry, float pickupDistance)
        {
            var best = DirectApproach(target, perimeter, entry, pickupDistance);
            for (int d = 0; d < 4; d++)
            {
                var candidate = StraightRay(target, d, perimeter, entry, pickupDistance);
                if (Better(candidate, best)) best = candidate;
            }
            return best;
        }

        private TargetCandidate DirectApproach(Cell target, MapPerimeter perimeter, Vector3 entry, float pickupDistance)
        {
            if (!IsBorder(target)) return null;
            TargetCandidate best = null;
            foreach (var point in perimeter.BorderPoints(target, map.Columns))
            {
                // Match the actual endpoint even when the pickup offset lies outside the card.
                float inside = Mathf.Abs(Vector3.Distance(point, perimeter.CellPoint(target)) - pickupDistance);
                var candidate = CreateCandidate(target, null, point, inside,
                    perimeter.Distance(entry, point), new List<Cell>());
                if (Better(candidate, best)) best = candidate;
            }
            return best;
        }

        private TargetCandidate StraightRay(Cell target, int direction, MapPerimeter perimeter,
            Vector3 entry, float pickupDistance)
        {
            var access = Neighbor(target, direction);
            if (access == null || !access.IsEmpty) return null;
            var route = new List<Cell>();
            var cell = access;
            while (cell != null && cell.IsEmpty)
            {
                route.Add(cell);
                var next = Neighbor(cell, direction);
                if (next == null) break;
                cell = next;
            }
            if (cell == null || !cell.IsEmpty || Neighbor(cell, direction) != null) return null;
            var outward = (perimeter.CellPoint(access) - perimeter.CellPoint(target)).normalized;
            TargetCandidate best = null;
            foreach (var point in perimeter.BorderPoints(cell, map.Columns))
            {
                var delta = point - perimeter.CellPoint(cell);
                if (Vector3.Dot(delta.normalized, outward) < .999f) continue;
                float inside = Vector3.Distance(point, perimeter.CellPoint(cell)) +
                    Vector3.Distance(perimeter.CellPoint(cell), perimeter.CellPoint(access)) +
                    Mathf.Max(0f, Vector3.Distance(perimeter.CellPoint(access), perimeter.CellPoint(target)) - pickupDistance);
                var interior = new List<Cell>(route);
                interior.Reverse();
                var candidate = CreateCandidate(target, access, point, inside,
                    perimeter.Distance(entry, point), interior);
                if (Better(candidate, best)) best = candidate;
            }
            return best;
        }

        private Search SearchFrom(MapPerimeter perimeter, Vector3 entry)
        {
            if (searchPerimeter != perimeter)
            {
                searches.Clear();
                searchPerimeter = perimeter;
            }
            if (searches.TryGetValue(entry, out var cached)) return cached;
            var search = new Search(map.CellCount);
            SeedDistances(search, perimeter, entry);
            for (int step = 0; step < map.CellCount; step++)
            {
                int current = NextCell(search);
                if (current < 0) break;
                search.Visited[current] = true;
                RelaxNeighbors(search, current, perimeter);
            }
            searches.Add(entry, search);
            return search;
        }

        private int CellId(Cell cell) => cell.Row * map.Columns + cell.Column;

        private void SeedDistances(Search search, MapPerimeter perimeter, Vector3 entry)
        {
            foreach (var cell in map.EnumerateCells())
            {
                if (!cell.IsEmpty || !IsBorder(cell)) continue;
                int id = CellId(cell);
                foreach (var point in perimeter.BorderPoints(cell, map.Columns))
                {
                    float distance = Vector3.Distance(point, perimeter.CellPoint(cell));
                    float perimeterDistance = perimeter.Distance(entry, point);
                    if (distance > search.Distance[id] + Epsilon ||
                        Mathf.Abs(distance - search.Distance[id]) <= Epsilon &&
                        perimeterDistance >= search.PerimeterDistance[id] - Epsilon) continue;
                    search.Distance[id] = distance;
                    search.PerimeterDistance[id] = perimeterDistance;
                    search.Edge[id] = point;
                    search.Steps[id] = 1;
                }
            }
        }

        private static int NextCell(Search search)
        {
            int best = -1;
            float smallest = float.PositiveInfinity;
            for (int i = 0; i < search.Distance.Length; i++)
            {
                if (search.Visited[i] || float.IsPositiveInfinity(search.Distance[i])) continue;
                if (search.Distance[i] < smallest - Epsilon ||
                    Mathf.Abs(search.Distance[i] - smallest) <= Epsilon &&
                    (best < 0 || search.PerimeterDistance[i] < search.PerimeterDistance[best] - Epsilon ||
                     Mathf.Abs(search.PerimeterDistance[i] - search.PerimeterDistance[best]) <= Epsilon &&
                     search.Steps[i] < search.Steps[best]))
                {
                    smallest = search.Distance[i];
                    best = i;
                }
            }
            return best;
        }

        private void RelaxNeighbors(Search search, int current, MapPerimeter perimeter)
        {
            var cell = map.GetCell(current / map.Columns, current % map.Columns);
            for (int d = 0; d < 4; d++)
            {
                var next = Neighbor(cell, d);
                if (next == null || !next.IsEmpty) continue;
                int id = CellId(next);
                float cost = search.Distance[current] + Vector3.Distance(perimeter.CellPoint(cell), perimeter.CellPoint(next));
                int steps = search.Steps[current] + 1;
                if (cost > search.Distance[id] + Epsilon ||
                    Mathf.Abs(cost - search.Distance[id]) <= Epsilon &&
                    (search.PerimeterDistance[current] > search.PerimeterDistance[id] + Epsilon ||
                     Mathf.Abs(search.PerimeterDistance[current] - search.PerimeterDistance[id]) <= Epsilon &&
                     steps >= search.Steps[id])) continue;
                search.Distance[id] = cost;
                search.PerimeterDistance[id] = search.PerimeterDistance[current];
                search.Previous[id] = current;
                search.Edge[id] = search.Edge[current];
                search.Steps[id] = steps;
            }
        }

        private TargetCandidate FindAccess(Cell target, Search search, MapPerimeter perimeter,
            Vector3 entry, float pickupDistance)
        {
            TargetCandidate best = null;
            for (int d = 0; d < 4; d++)
            {
                var access = Neighbor(target, d);
                if (access == null || !access.IsEmpty) continue;
                int id = CellId(access);
                if (float.IsPositiveInfinity(search.Distance[id])) continue;
                float inside = search.Distance[id] +
                    Mathf.Max(0f, Vector3.Distance(perimeter.CellPoint(access), perimeter.CellPoint(target)) - pickupDistance);
                var candidate = CreateCandidate(target, access, search.Edge[id], inside,
                    perimeter.Distance(entry, search.Edge[id]), Trace(search, id));
                if (Better(candidate, best)) best = candidate;
            }
            return best;
        }

        private List<Cell> Trace(Search search, int id)
        {
            var route = new List<Cell>();
            for (int current = id; current >= 0; current = search.Previous[current])
                route.Add(map.GetCell(current / map.Columns, current % map.Columns));
            route.Reverse();
            return route;
        }

        private static TargetCandidate CreateCandidate(Cell target, Cell access, Vector3 entry,
            float inside, float perimeterDistance, List<Cell> route) =>
            new TargetCandidate { Target = target, AccessCell = access, EntryPoint = entry,
                InteriorDistance = inside, TravelDistance = inside + perimeterDistance, InteriorRoute = route };

        public List<Cell> BuildInteriorRoute(TargetCandidate candidate) => new List<Cell>(candidate.InteriorRoute);
    }
}
