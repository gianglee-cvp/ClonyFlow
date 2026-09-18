using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        public Cell Cell { get; private set; }

        public void Configure(Renderer[] targets) => renderers = targets;

        public void Initialize(Cell cell, Color color)
        {
            Cell = cell;
            ApplyColor(color);
        }

        private void ApplyColor(Color color)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    propertyBlock.Clear();
                    renderer.GetPropertyBlock(propertyBlock, i);
                    propertyBlock.SetColor(BaseColor, color);
                    propertyBlock.SetColor(ColorProperty, color);
                    renderer.SetPropertyBlock(propertyBlock, i);
                }
            }
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
