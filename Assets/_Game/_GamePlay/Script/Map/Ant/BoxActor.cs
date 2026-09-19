using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ColonyFlow.Core.Pooling;

namespace ColonyFlow.Gameplay
{
    public sealed class BoxActor : MonoBehaviour, IPoolable
    {
        [SerializeField] private Text countLabel;
        [SerializeField] private Renderer face;
        [SerializeField] private RectTransform countCanvas;
        [SerializeField] private Collider hitCollider;
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField, Min(.01f)] private float slotJumpDuration = .35f;
        [SerializeField, Min(0)] private float slotJumpHeight = 1.4f;
        [SerializeField, Min(.01f)] private float queueMoveDuration = .25f;
        private ActorAnimation animation;
        public bool IsLanding { get; private set; }
        public int ColorId { get; private set; }
        public BoxKind Kind { get; private set; }
        public bool CanPickup => Kind == BoxKind.Normal && AntCount > 0;
        public int AntCount { get; private set; }
        public int OutgoingCount { get; private set; }
        public int QueueIndex { get; private set; }
        public int SlotIndex { get; internal set; } = -1;
        public float Timer { get; internal set; }
        public Collider HitCollider => hitCollider;
        public Material ColorMaterialTemplate => face != null ? face.sharedMaterial : null;

        public void OnPoolSpawned()
        {
            if (animation == null) animation = new ActorAnimation(gameObject);
            ResetBox();
        }

        public void OnPoolRecycled() => ResetBox();
        private void ResetBox()
        {
            animation?.Cancel();
            IsLanding = false;
            ColorId = AntCount = OutgoingCount = 0;
            Kind = BoxKind.Normal;
            QueueIndex = SlotIndex = -1;
            Timer = 0;
            RefreshLabel();
        }

        public void Configure(Text label, Renderer renderer, RectTransform canvas, Collider collider, Renderer[] body)
        {
            countLabel = label;
            face = renderer;
            countCanvas = canvas;
            hitCollider = collider;
            bodyRenderers = body;
        }

        public void Initialize(int colorId, int count, int queueIndex, Material colorMaterial, Camera camera,
            BoxKind kind = BoxKind.Normal)
        {
            if (animation == null) animation = new ActorAnimation(gameObject);
            animation.Cancel();
            IsLanding = false;
            ColorId = colorId;
            Kind = kind;
            AntCount = count;
            QueueIndex = queueIndex;
            OutgoingCount = 0;
            SlotIndex = -1;
            Timer = 0;
            if (colorMaterial != null) face.sharedMaterial = colorMaterial;
            RefreshLabel();
            AlignCount(camera);
        }

        public void JumpToSlot(Vector3 destination, float unit)
        {
            IsLanding = true;
            var jump = transform.DOJump(destination, slotJumpHeight * unit, 1, slotJumpDuration)
                .SetEase(Ease.Linear).OnComplete(FinishLanding);
            if (animation.Play(jump)) return;
            transform.position = destination;
            FinishLanding();
        }

        public void AdvanceAnimation(float delta) => animation?.Advance(delta);
        public void MoveInQueue(Vector3 destination)
        {
            var move = DOTween.Sequence().Append(transform.DOMove(destination, queueMoveDuration).SetEase(Ease.OutQuad));
            if (!animation.Play(move)) transform.position = destination;
        }
        private void FinishLanding() => IsLanding = false;

        private void OnDisable()
        {
            animation?.Cancel();
            IsLanding = false;
        }

        private void OnDestroy() => animation?.Dispose();

        public void AlignCount(Camera camera)
        {
            if (camera == null || countCanvas == null) return;
            var bounds = face.bounds;
            foreach (var renderer in bodyRenderers) bounds.Encapsulate(renderer.bounds);
            var forward = camera.transform.forward;
            float extent = Mathf.Abs(forward.x) * bounds.extents.x +
                Mathf.Abs(forward.y) * bounds.extents.y + Mathf.Abs(forward.z) * bounds.extents.z;
            var center = face.bounds.center;
            float distance = Vector3.Dot(forward, center - bounds.center) + extent + .04f;
            countCanvas.SetPositionAndRotation(center - forward * distance, camera.transform.rotation);
        }

        public bool Allocate()
        {
            if (AntCount <= 0) return false;
            AntCount--;
            OutgoingCount++;
            RefreshLabel();
            return true;
        }

        public bool Resolve(bool collected)
        {
            if (OutgoingCount <= 0) return false;
            OutgoingCount--;
            if (!collected) AntCount++;
            RefreshLabel();
            return true;
        }

        public void ClearBudget()
        {
            AntCount = 0;
            RefreshLabel();
        }

        public void DiscardOutgoing()
        {
            if (OutgoingCount > 0) OutgoingCount--;
        }

        private void RefreshLabel()
        {
            if (countLabel != null) countLabel.text = AntCount.ToString();
        }
    }
}
