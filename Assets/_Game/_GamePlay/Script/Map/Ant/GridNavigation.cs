using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class TargetCandidate
    {
        public Cell Target { get; internal set; }
        public Cell AccessCell { get; internal set; }
        public Cell BorderCell { get; internal set; }
        public int AccessWeight { get; internal set; }
        public bool DirectBorderAccess => AccessCell == null;
        internal List<Cell> InteriorRoute;
        public Vector3 EntryPoint { get; internal set; }
        public float TravelDistance { get; internal set; }
    }

    // Unit-distance BFS from every empty border cell: nearest reachable edge, not bottom-center.
    public sealed class GridNavigation
    {
        private readonly MapModel map;
        private readonly int[,] weights;
        private readonly int[,,] turns;
        private readonly int[] dr = { 1, 0, 0, -1 }; // down, left, right, up
        private readonly int[] dc = { 0, -1, 1, 0 };
        public MapModel Map => map;
        public GridNavigation(MapModel model)
        {
            map = model ?? throw new ArgumentNullException(nameof(model));
            weights = new int[map.Rows, map.Columns];
            turns = new int[map.Rows, map.Columns, 4];
            Rebuild();
        }
        public bool IsBorder(Cell cell) => cell.Row == 0 || cell.Column == 0 ||
            cell.Row == map.Rows - 1 || cell.Column == map.Columns - 1;
        public int NavigationWeight(Cell cell) => cell.IsEmpty ? weights[cell.Row, cell.Column] : -1;

        public void Rebuild()
        {
            var queue = new Queue<Cell>();
            foreach (var cell in map.EnumerateCells())
            {
                weights[cell.Row, cell.Column] = -1;
                for (int d = 0; d < 4; d++) turns[cell.Row, cell.Column, d] = -1;
                if (cell.IsEmpty && IsBorder(cell))
                {
                    weights[cell.Row, cell.Column] = 1;
                    queue.Enqueue(cell);
                }
            }
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    var n = Neighbor(current, d);
                    if (n == null || !n.IsEmpty || NavigationWeight(n) >= 0) continue;
                    weights[n.Row, n.Column] = NavigationWeight(current) + 1;
                    queue.Enqueue(n);
                }
            }
        }
        private Cell Neighbor(Cell cell, int direction)
        {
            int r = cell.Row + dr[direction], c = cell.Column + dc[direction];
            return r < 0 || r >= map.Rows || c < 0 || c >= map.Columns ? null : map.GetCell(r, c);
        }

        // Among equal-length paths, minimize direction changes using a strictly decreasing weight DAG.
        private int TurnCost(Cell cell, int heading)
        {
            if (IsBorder(cell)) return 0;
            int cached = turns[cell.Row, cell.Column, heading];
            if (cached >= 0) return cached;
            int best = int.MaxValue / 2;
            for (int d = 0; d < 4; d++)
            {
                var n = Neighbor(cell, d);
                if (n == null || !n.IsEmpty || NavigationWeight(n) != NavigationWeight(cell) - 1) continue;
                int cost = (d == heading ? 0 : 1) + TurnCost(n, d);
                if (cost < best) best = cost;
            }
            turns[cell.Row, cell.Column, heading] = best;
            return best;
        }
        private List<Cell> Trace(Cell access, int heading)
        {
            var route = new List<Cell> { access };
            var cell = access;
            while (!IsBorder(cell))
            {
                Cell best = null;
                int bestDirection = -1, min = int.MaxValue;
                for (int d = 0; d < 4; d++)
                {
                    var n = Neighbor(cell, d);
                    if (n == null || !n.IsEmpty || NavigationWeight(n) != NavigationWeight(cell) - 1) continue;
                    int cost = (d == heading ? 0 : 1) + TurnCost(n, d);
                    if (cost < min) { min = cost; best = n; bestDirection = d; }
                }
                if (best == null) throw new InvalidOperationException("Reachable cell has no decreasing path.");
                route.Add(best); cell = best; heading = bestDirection;
            }
            route.Reverse();
            return route;
        }
        public TargetCandidate Evaluate(Cell target)
        {
            if (target.IsEmpty) return null;
            if (IsBorder(target))
                return new TargetCandidate { Target = target, BorderCell = target, AccessWeight = 0,
                    InteriorRoute = new List<Cell>() };
            Cell access = null;
            int minWeight = int.MaxValue, minTurns = int.MaxValue, heading = -1;
            for (int d = 0; d < 4; d++)
            {
                var n = Neighbor(target, d);
                if (n == null || !n.IsEmpty) continue;
                int w = NavigationWeight(n);
                if (w < 0) continue;
                int cost = TurnCost(n, d);
                if (w < minWeight || w == minWeight && cost < minTurns)
                {
                    access = n; minWeight = w; minTurns = cost; heading = d;
                }
            }
            if (access == null) return null;
            var route = Trace(access, heading);
            return new TargetCandidate { Target = target, AccessCell = access, AccessWeight = minWeight,
                BorderCell = route[0], InteriorRoute = route };
        }
        // Outside travel stays on the card. A clear approach ray is entered directly
        // instead of cutting across the empty area around the colored picture.
        private TargetCandidate StraightApproach(Cell target, MapPerimeter perimeter, Vector3 entry)
        {
            TargetCandidate best = null;
            if (IsBorder(target))
                foreach (var point in perimeter.BorderPoints(target, map.Columns))
                {
                    float cost = perimeter.Distance(entry, point)
                        + Mathf.Max(0, Vector3.Distance(point, perimeter.CellPoint(target)) - 1.1f);
                    if (best == null || cost < best.TravelDistance - .0001f)
                        best = new TargetCandidate { Target = target, BorderCell = target, AccessWeight = 0,
                            EntryPoint = point, TravelDistance = cost, InteriorRoute = new List<Cell>() };
                }
            for (int d = 0; d < 4; d++)
            {
                var access = Neighbor(target, d);
                if (access == null || !access.IsEmpty) continue;
                var route = new List<Cell>();
                var cell = access;
                while (cell != null && cell.IsEmpty)
                {
                    route.Add(cell);
                    var next = Neighbor(cell, d);
                    if (next == null) break;
                    cell = next;
                }
                if (cell == null || !cell.IsEmpty || Neighbor(cell, d) != null) continue;
                var outward = (perimeter.CellPoint(access) - perimeter.CellPoint(target)).normalized;
                foreach (var point in perimeter.BorderPoints(cell, map.Columns))
                {
                    var delta = point - perimeter.CellPoint(cell);
                    if (Vector3.Dot(delta.normalized, outward) < .999f) continue;
                    float cost = perimeter.Distance(entry, point) + Vector3.Distance(point, perimeter.CellPoint(access));
                    if (best != null && cost >= best.TravelDistance - .0001f) continue;
                    var interior = new List<Cell>(route); interior.Reverse();
                    best = new TargetCandidate { Target = target, AccessCell = access, BorderCell = cell,
                        AccessWeight = interior.Count, EntryPoint = point, TravelDistance = cost, InteriorRoute = interior };
                }
            }
            return best;
        }
        // Dijkstra seeds include the real card/perimeter approach cost for this Slot.
        // Colored cells are never traversed; the final point is adjacent to the target.
        public TargetCandidate EvaluateFrom(Cell target, MapPerimeter perimeter, Vector3 entry)
        {
            if (target.IsEmpty) return null;
            var straight = StraightApproach(target, perimeter, entry);
            if (straight != null) return straight;
            int count = map.Rows * map.Columns;
            var distance = new float[count];
            var previous = new int[count];
            var steps = new int[count];
            var edge = new Vector3[count];
            var visited = new bool[count];
            for (int i = 0; i < count; i++) { distance[i] = float.PositiveInfinity; previous[i] = -1; steps[i] = int.MaxValue; }
            foreach (var cell in map.EnumerateCells())
            {
                if (!cell.IsEmpty || !IsBorder(cell)) continue;
                int id = cell.Row * map.Columns + cell.Column;
                foreach (var point in perimeter.BorderPoints(cell, map.Columns))
                {
                    float cost = perimeter.Distance(entry, point) + Vector3.Distance(point, perimeter.CellPoint(cell));
                    if (cost < distance[id]) { distance[id] = cost; edge[id] = point; steps[id] = 1; }
                }
            }
            for (int step = 0; step < count; step++)
            {
                int current = -1;
                float smallest = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                    if (!visited[i] && !float.IsPositiveInfinity(distance[i]) && (distance[i] < smallest - .0001f ||
                        Mathf.Abs(distance[i] - smallest) <= .0001f && (current < 0 || steps[i] < steps[current]))) { smallest = distance[i]; current = i; }
                if (current < 0) break;
                visited[current] = true;
                var cell = map.GetCell(current / map.Columns, current % map.Columns);
                for (int d = 0; d < 4; d++)
                {
                    var next = Neighbor(cell, d);
                    if (next == null || !next.IsEmpty) continue;
                    int id = next.Row * map.Columns + next.Column;
                    float cost = smallest + Vector3.Distance(perimeter.CellPoint(cell), perimeter.CellPoint(next));
                    if (cost < distance[id] - .0001f ||
                        Mathf.Abs(cost - distance[id]) <= .0001f && steps[current] + 1 < steps[id])
                    {
                        distance[id] = cost; previous[id] = current; edge[id] = edge[current]; steps[id] = steps[current] + 1;
                    }
                }
            }
            TargetCandidate best = null;
            if (IsBorder(target))
                foreach (var point in perimeter.BorderPoints(target, map.Columns))
                {
                    float cost = perimeter.Distance(entry, point)
                        + Mathf.Max(0, Vector3.Distance(point, perimeter.CellPoint(target)) - 1.1f);
                    if (best == null || cost < best.TravelDistance - .0001f)
                        best = new TargetCandidate { Target = target, BorderCell = target, AccessWeight = 0,
                            EntryPoint = point, TravelDistance = cost, InteriorRoute = new List<Cell>() };
                }
            for (int d = 0; d < 4; d++)
            {
                var access = Neighbor(target, d);
                if (access == null || !access.IsEmpty) continue;
                int id = access.Row * map.Columns + access.Column;
                if (float.IsPositiveInfinity(distance[id]) ||
                    best != null && (distance[id] > best.TravelDistance + .0001f ||
                    Mathf.Abs(distance[id] - best.TravelDistance) <= .0001f && steps[id] >= best.InteriorRoute.Count)) continue;
                var route = new List<Cell>();
                for (int current = id; current >= 0; current = previous[current])
                    route.Add(map.GetCell(current / map.Columns, current % map.Columns));
                route.Reverse();
                best = new TargetCandidate { Target = target, AccessCell = access, BorderCell = route[0],
                    AccessWeight = route.Count, EntryPoint = edge[id], TravelDistance = distance[id], InteriorRoute = route };
            }
            return best;
        }
        public List<Cell> BuildInteriorRoute(TargetCandidate candidate)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            return new List<Cell>(candidate.InteriorRoute);
        }
    }
}
