using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class BoxActor : MonoBehaviour
    {
        [SerializeField] private Text countLabel;
        [SerializeField] private Renderer face;
        public int ColorId { get; private set; }
        public int AntCount { get; private set; }
        public int OutgoingCount { get; private set; }
        public int QueueIndex { get; private set; }
        public int SlotIndex { get; internal set; } = -1;
        public float Timer { get; internal set; }
        public int SpawnableCount => AntCount;
        public void Configure(Text label, Renderer renderer) { countLabel = label; face = renderer; }
        public void AlignCount(Camera camera)
        {
            if (camera == null || countLabel == null || face == null) return;
            var canvas = countLabel.GetComponentInParent<Canvas>(true);
            if (canvas == null) return;
            var bounds = face.bounds;
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
                bounds.Encapsulate(renderer.bounds);
            var forward = camera.transform.forward;
            float extent = Mathf.Abs(forward.x) * bounds.extents.x +
                Mathf.Abs(forward.y) * bounds.extents.y + Mathf.Abs(forward.z) * bounds.extents.z;
            var center = face.bounds.center;
            float distance = Vector3.Dot(forward, center - bounds.center) + extent + .04f;
            // Keep the whole label plane in front of the nearest mesh surface.
            canvas.transform.SetPositionAndRotation(center - forward * distance, camera.transform.rotation);
        }
        public void Initialize(int colorId, int count, int queueIndex, Color color)
        {
            ColorId = colorId; AntCount = count; QueueIndex = queueIndex; OutgoingCount = 0;
            SlotIndex = -1; Timer = 0;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            face.SetPropertyBlock(block);
            RefreshLabel();
            AlignCount(Camera.main);
        }
        public bool Allocate()
        {
            if (SpawnableCount <= 0) return false;
            AntCount--; OutgoingCount++; RefreshLabel();
            return true;
        }
        public void Resolve(bool collected)
        {
            if (OutgoingCount <= 0) throw new System.InvalidOperationException("Task resolved more than once.");
            OutgoingCount--;
            if (!collected) AntCount++; // Return the ant budget when its task is cancelled.
            RefreshLabel();
        }
        private void RefreshLabel()
        {
            if (countLabel != null) countLabel.text = AntCount.ToString();
        }
    }
}
