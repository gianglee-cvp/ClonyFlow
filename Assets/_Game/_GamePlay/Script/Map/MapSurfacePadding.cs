using UnityEngine;

namespace ColonyFlow.Gameplay
{
    // Insets the playable map area inside the visible Surface. Values are fractions
    // of the Surface width and depth, measured from each corresponding edge.
    [DisallowMultipleComponent]
    public sealed class MapSurfacePadding : MonoBehaviour
    {
        [SerializeField, Tooltip("Fraction of Surface width (X) and depth (Z) reserved on each side.")]
        private Vector2 paddingRatio = new Vector2(.08f, .08f);

        public Vector2 PaddingRatio => new Vector2(
            Mathf.Clamp(paddingRatio.x, 0f, .49f),
            Mathf.Clamp(paddingRatio.y, 0f, .49f));

        public Bounds InnerBounds(Bounds surfaceBounds)
        {
            Vector2 ratio = PaddingRatio;
            var size = surfaceBounds.size;
            size.x *= 1f - ratio.x * 2f;
            size.z *= 1f - ratio.y * 2f;
            return new Bounds(surfaceBounds.center, size);
        }

        private void OnValidate() => paddingRatio = PaddingRatio;
    }
}
