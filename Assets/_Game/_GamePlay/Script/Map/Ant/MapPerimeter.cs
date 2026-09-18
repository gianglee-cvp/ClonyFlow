using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    // A fixed clockwise rectangle in XZ. Points must be authored on this rectangle.
    public sealed class MapPerimeter
    {
        private readonly float minX, maxX, minZ, maxZ, width, height, length;
        private readonly Transform root;
        private readonly int bottomRow;
        public MapPerimeter(Transform mapRoot, MapModel map, Bounds cardLocalBounds)
        {
            root = mapRoot; bottomRow = map.Rows - 1;
            minX = cardLocalBounds.min.x; maxX = cardLocalBounds.max.x;
            minZ = cardLocalBounds.min.z; maxZ = cardLocalBounds.max.z;
            width = maxX - minX; height = maxZ - minZ; length = 2 * (width + height);
        }
        public static Bounds CardBounds(Transform mapRoot, Renderer surface)
        {
            var bounds = surface.localBounds;
            var min = new Vector3(float.PositiveInfinity, 0, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, 0, float.NegativeInfinity);
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var point = mapRoot.InverseTransformPoint(surface.transform.TransformPoint(corner));
                min.x = Mathf.Min(min.x, point.x); min.z = Mathf.Min(min.z, point.z);
                max.x = Mathf.Max(max.x, point.x); max.z = Mathf.Max(max.z, point.z);
            }
            return new Bounds((min + max) * .5f, max - min);
        }
        public Vector3 BottomEntry(float localX) => root.TransformPoint(new Vector3(Mathf.Clamp(localX, minX, maxX), 0, minZ));
        public Vector3 CellPoint(Cell cell) => root.TransformPoint(cell.Position);
        public Vector3 RemapFrom(Vector3 world, Bounds old)
        {
            var p = root.InverseTransformPoint(world);
            bool horizontal = Mathf.Abs(p.z - old.min.z) < .01f || Mathf.Abs(p.z - old.max.z) < .01f;
            bool vertical = Mathf.Abs(p.x - old.min.x) < .01f || Mathf.Abs(p.x - old.max.x) < .01f;
            if (!(horizontal && p.x >= old.min.x - .01f && p.x <= old.max.x + .01f ||
                vertical && p.z >= old.min.z - .01f && p.z <= old.max.z + .01f)) return world;
            p.x = vertical ? (Mathf.Abs(p.x - old.min.x) < .01f ? minX : maxX) : Mathf.Clamp(p.x, minX, maxX);
            p.z = horizontal ? (Mathf.Abs(p.z - old.min.z) < .01f ? minZ : maxZ) : Mathf.Clamp(p.z, minZ, maxZ);
            return root.TransformPoint(p);
        }
        public IEnumerable<Vector3> BorderPoints(Cell cell, int columns)
        {
            var p = cell.Position; p.y = 0;
            if (cell.Row == bottomRow) yield return root.TransformPoint(new Vector3(p.x, 0, minZ));
            if (cell.Column == 0) yield return root.TransformPoint(new Vector3(minX, 0, p.z));
            if (cell.Column == columns - 1) yield return root.TransformPoint(new Vector3(maxX, 0, p.z));
            if (cell.Row == 0) yield return root.TransformPoint(new Vector3(p.x, 0, maxZ));
        }
        private float Parameter(Vector3 world)
        {
            var p = root.InverseTransformPoint(world);
            if (Mathf.Abs(p.z - minZ) < .02f) return maxX - p.x;
            if (Mathf.Abs(p.x - minX) < .02f) return width + p.z - minZ;
            if (Mathf.Abs(p.z - maxZ) < .02f) return width + height + p.x - minX;
            return 2 * width + height + maxZ - p.z;
        }
        private Vector3 At(float value)
        {
            value = Mathf.Repeat(value, length);
            Vector3 p;
            if (value <= width) p = new Vector3(maxX - value, 0, minZ);
            else if (value <= width + height) p = new Vector3(minX, 0, minZ + value - width);
            else if (value <= 2 * width + height) p = new Vector3(minX + value - width - height, 0, maxZ);
            else p = new Vector3(maxX, 0, maxZ - value + 2 * width + height);
            return root.TransformPoint(p);
        }
        public List<Vector3> Route(Vector3 from, Vector3 to)
        {
            float a = Parameter(from), b = Parameter(to);
            float cw = Mathf.Repeat(b - a, length), ccw = Mathf.Repeat(a - b, length);
            bool clockwise = cw <= ccw + .0001f;
            float distance = clockwise ? cw : ccw;
            var waypoints = new List<KeyValuePair<float, Vector3>>();
            foreach (float corner in new[] { 0f, width, width + height, 2 * width + height })
            {
                float d = Mathf.Repeat(clockwise ? corner - a : a - corner, length);
                if (d > .001f && d < distance - .001f) waypoints.Add(new KeyValuePair<float, Vector3>(d, At(corner)));
            }
            waypoints.Sort((x, y) => x.Key.CompareTo(y.Key));
            var route = new List<Vector3>();
            foreach (var pair in waypoints) route.Add(pair.Value);
            route.Add(to);
            return route;
        }
        public float Distance(Vector3 from, Vector3 to)
        {
            float d = Mathf.Abs(Parameter(from) - Parameter(to));
            return Mathf.Min(d, length - d) * root.lossyScale.x;
        }
    }
}
