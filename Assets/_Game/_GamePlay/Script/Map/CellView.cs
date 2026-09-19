using UnityEngine;
using ColonyFlow.Core.Pooling;

namespace ColonyFlow.Gameplay
{
    public sealed class CellView : MonoBehaviour, IPoolable
    {
        [SerializeField] private Renderer[] renderers;
        public Cell Cell { get; private set; }
        public Material MaterialTemplate => renderers != null && renderers.Length > 0 ? renderers[0].sharedMaterial : null;
        public void OnPoolSpawned() => Cell = null;
        public void OnPoolRecycled() => Cell = null;

        public void Configure(Renderer[] targets) => renderers = targets;

        public void Initialize(Cell cell, Material colorMaterial)
        {
            Cell = cell;
            foreach (var renderer in renderers)
                if (colorMaterial != null) renderer.sharedMaterial = colorMaterial;
        }

        public float WorldBottom()
        {
            float bottom = float.PositiveInfinity;
            foreach (var renderer in renderers)
            {
                var bounds = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = new Vector3(
                        (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                    bottom = Mathf.Min(bottom, renderer.transform.TransformPoint(point).y);
                }
            }
            return bottom;
        }
    }
}
