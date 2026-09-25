using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    // UIParticle renders ParticleSystems, not standalone TrailRenderers.
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class CanvasTrailGraphic : MaskableGraphic
    {
        private TrailRenderer trail;
        private Vector3[] positions = new Vector3[64];
        private bool previousForceRenderingOff;

        protected override void OnEnable()
        {
            base.OnEnable();
            trail = GetComponent<TrailRenderer>();
            previousForceRenderingOff = trail.forceRenderingOff;
            trail.forceRenderingOff = true;
            raycastTarget = false;
        }

        protected override void OnDisable()
        {
            if (trail != null) trail.forceRenderingOff = previousForceRenderingOff;
            base.OnDisable();
        }

        private void LateUpdate() => SetVerticesDirty();

        public override Texture mainTexture => material != null && material.mainTexture != null
            ? material.mainTexture : Texture2D.whiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (trail == null || trail.positionCount < 2) return;
            if (positions.Length < trail.positionCount)
                System.Array.Resize(ref positions, Mathf.NextPowerOfTwo(trail.positionCount));
            int count = trail.GetPositions(positions);
            for (int i = 0; i < count; i++) positions[i] = rectTransform.InverseTransformPoint(positions[i]);
            float scale = Mathf.Max(.0001f, Mathf.Abs(rectTransform.lossyScale.x));
            for (int i = 0; i < count; i++)
            {
                float age = (float)i / (count - 1);
                Vector3 direction = positions[Mathf.Min(i + 1, count - 1)] - positions[Mathf.Max(i - 1, 0)];
                Vector3 offset = new Vector3(-direction.y, direction.x, 0).normalized *
                    (trail.widthMultiplier * trail.widthCurve.Evaluate(age) / scale * .5f);
                Color32 tint = trail.colorGradient.Evaluate(age) * color;
                vh.AddVert(positions[i] - offset, tint, new Vector2(age, 0));
                vh.AddVert(positions[i] + offset, tint, new Vector2(age, 1));
                if (i == 0) continue;
                int index = i * 2;
                vh.AddTriangle(index - 2, index - 1, index);
                vh.AddTriangle(index, index - 1, index + 1);
            }
        }
    }
}
