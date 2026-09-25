using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ColonyFlow.Core.Pooling;

namespace ColonyFlow.Gameplay
{
    public enum BoxVisualState
    {
        QueueBack,
        QueueFront,
        Slot
    }

    public sealed class BoxActor : MonoBehaviour, IPoolable
    {
        [SerializeField] private Text countLabel;
        [SerializeField] private Renderer face;
        [SerializeField] private Renderer border;
        [SerializeField] private Renderer side;
        [SerializeField] private RectTransform outlineCanvas;
        [SerializeField] private Image outlineImage;
        [SerializeField] private RectTransform countCanvas;
        [SerializeField] private Collider hitCollider;
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField, Min(.01f)] private float slotJumpDuration = .35f;
        [SerializeField, Min(0)] private float slotJumpHeight = 1.4f;
        [SerializeField, Range(1f, 1.5f)] private float slotJumpScale = 1.14f;
        [SerializeField, Min(.01f)] private float queueMoveDuration = .25f;
        [SerializeField, Min(.01f)] private float disappearBumpDuration = .08f;
        [SerializeField, Min(0f)] private float disappearBumpHeight = .1f;
        [SerializeField, Min(1f)] private float disappearBumpScale = 1.12f;
        [SerializeField, Min(.01f)] private float disappearDuration = .22f;
        [SerializeField, Min(0f)] private float disappearHeight = .45f;
        [SerializeField, Min(1f)] private float disappearScale = 1.2f;
        [SerializeField] private GameObject emptyBoxFirework;
        [SerializeField, Min(.01f)] private float fireworkScale = 1f;
        [SerializeField, HideInInspector] private Camera visualCamera;
        private new ActorAnimation animation;
        private BoxVisualState visualState;
        private Vector3 sideFullScale;
        private Vector3 borderFullScale;
        private Vector3 outlineCanvasFullScale;
        private bool visualScalesCaptured;
        public bool IsLanding { get; private set; }
        public bool IsDisappearing { get; private set; }
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
        public Material BodyMaterialTemplate => side != null ? side.sharedMaterial : null;
        internal static readonly System.Collections.Generic.HashSet<BoxActor> OutlineBoxes = new();
        internal static readonly System.Collections.Generic.HashSet<BoxActor> CountBoxes = new();
        internal Camera CountCamera => visualCamera;
        internal Vector3 CountPosition => face != null ? face.bounds.center : transform.position;
        private void OnEnable() => CountBoxes.Add(this);
        internal void CollectOutlineRenderers(System.Collections.Generic.List<Renderer> targets)
        {
            Add(face);
            Add(side);
            Add(border);
            void Add(Renderer renderer)
            {
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    targets.Add(renderer);
            }
        }

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
            IsDisappearing = false;
            ColorId = AntCount = OutgoingCount = 0;
            Kind = BoxKind.Normal;
            QueueIndex = SlotIndex = -1;
            Timer = 0;
            visualState = BoxVisualState.QueueBack;
            OutlineBoxes.Remove(this);
            SetRaisedVisible(false);
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

        public void Initialize(int colorId, int count, int queueIndex, Material colorMaterial,
            Material bodyMaterial, Camera camera,
            BoxKind kind = BoxKind.Normal)
        {
            visualCamera = camera;
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
            EnsureRaisedVisual();
            if (colorMaterial != null)
                face.sharedMaterial = colorMaterial;
            if (bodyMaterial != null) side.sharedMaterial = bodyMaterial;
            ApplyWhiteBorder();
            SetVisualState(BoxVisualState.QueueBack);
            RefreshLabel();
            AlignCount(camera);
        }

        public void FitToFootprint(float availableWidth, float availableDepth, float fill,
            float heightRatio, Camera camera)
        {
            EnsureRaisedVisual();
            if (availableWidth <= 0f || availableDepth <= 0f) return;
            ApplyHeightRatio(heightRatio);
            Bounds bounds = VisualBounds();
            if (bounds.size.x <= .0001f || bounds.size.z <= .0001f) return;

            fill = Mathf.Clamp(fill, .5f, 1f);
            float factor = Mathf.Min(availableWidth * fill / bounds.size.x,
                availableDepth * fill / bounds.size.z);
            if (factor > .0001f) transform.localScale *= factor;
            AlignCount(camera);
            CaptureVisualScales();
        }

        private void ApplyHeightRatio(float heightRatio)
        {
            if (side == null || border == null || face == null) return;
            heightRatio = Mathf.Clamp(heightRatio, .2f, .8f);
            float faceTop = face.transform.localPosition.y + face.transform.localScale.y * .5f;
            float sideTop = border.transform.localPosition.y + border.transform.localScale.y * .15f;
            float targetBottom = faceTop - border.transform.localScale.x * heightRatio;
            Vector3 scale = side.transform.localScale;
            scale.y = Mathf.Max(.05f, sideTop - targetBottom);
            side.transform.localScale = scale;
            Vector3 position = side.transform.localPosition;
            position.y = (sideTop + targetBottom) * .5f;
            side.transform.localPosition = position;
            if (hitCollider is BoxCollider boxCollider)
            {
                Vector3 size = boxCollider.size;
                size.y = faceTop - targetBottom;
                boxCollider.size = size;
                Vector3 center = boxCollider.center;
                center.y = (faceTop + targetBottom) * .5f;
                boxCollider.center = center;
            }
            visualScalesCaptured = false;
        }

        public void JumpToSlot(Vector3 destination, float unit)
        {
            SetVisualState(BoxVisualState.Slot);
            IsLanding = true;
            Vector3 restingScale = transform.localScale;
            float scaleUp = Mathf.Max(1.05f, slotJumpScale);
            var scale = DOTween.Sequence()
                .Append(transform.DOScale(restingScale * scaleUp, slotJumpDuration * .25f)
                    .SetEase(Ease.OutExpo))
                .Append(transform.DOScale(restingScale, slotJumpDuration * .75f)
                    .SetEase(Ease.OutCubic));
            var jump = DOTween.Sequence()
                .Append(transform.DOJump(destination, slotJumpHeight * unit, 1, slotJumpDuration)
                    .SetEase(Ease.OutCubic))
                .Join(scale)
                .OnComplete(FinishLanding);
            if (animation.Play(jump)) return;
            transform.position = destination;
            transform.localScale = restingScale;
            FinishLanding();
        }

        public void Disappear()
        {
            if (IsDisappearing) return;
            IsDisappearing = true;
            IsLanding = false;
            Vector3 restingScale = transform.localScale;
            var disappear = DOTween.Sequence()
                .Append(transform.DOScale(restingScale * disappearBumpScale, disappearBumpDuration)
                    .SetEase(Ease.OutQuad))
                .Join(transform.DOMoveY(transform.position.y + disappearBumpHeight, disappearBumpDuration)
                    .SetEase(Ease.OutQuad))
                .Append(transform.DOMoveY(transform.position.y + disappearHeight, disappearDuration)
                    .SetEase(Ease.OutSine))
                .Join(transform.DOScale(restingScale * disappearScale, disappearDuration)
                    .SetEase(Ease.OutSine))
                .OnComplete(() => FinishDisappear(restingScale));
            if (animation.Play(disappear)) return;
            FinishDisappear(restingScale);
        }

        public Vector3 RestOnSlot(Vector3 anchor, Renderer slotSurface)
        {
            Vector3 destination = anchor;
            if (slotSurface == null) return destination;
            float bottomOffsetFromRoot = VisualBounds().min.y - transform.position.y;
            destination.y = slotSurface.bounds.max.y - bottomOffsetFromRoot + .005f;
            return destination;
        }

        public void AdvanceAnimation(float delta) => animation?.Advance(delta);
        public void PlaceInQueue(Vector3 destination, bool isFront, bool animate)
        {
            BoxVisualState target = isFront ? BoxVisualState.QueueFront : BoxVisualState.QueueBack;
            if (!animate)
            {
                transform.position = destination;
                SetVisualState(target);
                return;
            }

            CaptureVisualScales();
            bool reveal = target == BoxVisualState.QueueFront && visualState == BoxVisualState.QueueBack;
            var move = DOTween.Sequence()
                .Append(transform.DOMove(destination, queueMoveDuration).SetEase(Ease.OutCubic));
            if (reveal)
            {
                OutlineBoxes.Add(this);
                SetRaisedVisible(true);
                side.transform.localScale = Compressed(sideFullScale);
                border.transform.localScale = Compressed(borderFullScale);
                if (outlineCanvas != null) outlineCanvas.localScale = outlineCanvasFullScale * .94f;
                move.Join(side.transform.DOScale(sideFullScale, queueMoveDuration).SetEase(Ease.OutCubic));
                move.Join(border.transform.DOScale(borderFullScale, queueMoveDuration).SetEase(Ease.OutCubic));
                if (outlineCanvas != null)
                    move.Join(outlineCanvas.DOScale(outlineCanvasFullScale, queueMoveDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                SetVisualState(target);
            }
            move.OnComplete(() => SetVisualState(target));
            if (animation.Play(move)) return;
            transform.position = destination;
            SetVisualState(target);
        }
        private void FinishLanding() => IsLanding = false;

        private void FinishDisappear(Vector3 restingScale)
        {
            if (AntCount == 0 && emptyBoxFirework != null)
                BoxFireworkEffect.Play(emptyBoxFirework, VisualBounds().center, visualCamera, fireworkScale);
            transform.localScale = restingScale;
            IsDisappearing = false;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            CountBoxes.Remove(this);
            OutlineBoxes.Remove(this);
            animation?.Cancel();
            IsLanding = false;
        }

        private void OnDestroy() => animation?.Dispose();

        public void AlignCount(Camera camera)
        {
            if (camera == null || countCanvas == null) return;
            var bounds = visualState == BoxVisualState.QueueBack && face != null ? face.bounds : VisualBounds();
            var forward = camera.transform.forward;
            float extent = Mathf.Abs(forward.x) * bounds.extents.x +
                Mathf.Abs(forward.y) * bounds.extents.y + Mathf.Abs(forward.z) * bounds.extents.z;
            var center = face.bounds.center;
            float distance = Vector3.Dot(forward, center - bounds.center) + extent + .04f;
            countCanvas.SetPositionAndRotation(center - forward * distance, camera.transform.rotation);
            AlignUiOutline(camera, VisualBounds());
        }

        private void EnsureRaisedVisual()
        {
            // Retire the old enlarged mesh backdrop; the camera now draws the silhouette.
            var oldOutline = transform.Find("White Outline");
            if (oldOutline != null && oldOutline.TryGetComponent<Renderer>(out var oldRenderer))
                oldRenderer.enabled = false;
            if (border == null) border = (transform.Find("Highlight Rim") ?? transform.Find("Border"))
                ?.GetComponent<Renderer>();
            if (side == null) side = (transform.Find("Body") ?? transform.Find("Colored Side"))
                ?.GetComponent<Renderer>();
            if (outlineCanvas == null) outlineCanvas = transform.Find("White UI Outline") as RectTransform;
            if (outlineImage == null) outlineImage = outlineCanvas?.GetComponentInChildren<Image>();
            if (border == null && face != null)
            {
                Vector3 lidScale = face.transform.localScale;
                lidScale.x *= 1.136f;
                lidScale.y = .22f;
                lidScale.z *= 1.128f;
                face.transform.localScale = lidScale;
                face.transform.localPosition = new Vector3(0f, .32f, 0f);
                face.name = "Lid";

                var rimObject = Instantiate(face.gameObject, transform);
                rimObject.name = "Highlight Rim";
                rimObject.transform.SetSiblingIndex(face.transform.GetSiblingIndex());
                rimObject.transform.localPosition = new Vector3(0f, .20f, 0f);
                rimObject.transform.localScale = new Vector3(lidScale.x * .99f, .08f, lidScale.z * .99f);
                border = rimObject.GetComponent<Renderer>();
                AddBodyRenderer(border);
            }
            if (side == null && border != null)
            {
                var sideObject = Instantiate(border.gameObject, transform);
                sideObject.name = "Body";
                sideObject.transform.SetSiblingIndex(0);
                side = sideObject.GetComponent<Renderer>();
                Vector3 scale = border.transform.localScale;
                scale.x *= .94f;
                scale.y *= 1.8f;
                scale.z *= .94f;
                side.transform.localScale = scale;
                side.transform.localPosition = border.transform.localPosition + Vector3.down *
                    (border.transform.localScale.y * .8f);
                AddBodyRenderer(side);
            }
        }

        public void SetVisualState(BoxVisualState state)
        {
            EnsureRaisedVisual();
            CaptureVisualScales();
            visualState = state;
            if (state == BoxVisualState.QueueFront) OutlineBoxes.Add(this);
            else OutlineBoxes.Remove(this);
            bool raised = state != BoxVisualState.QueueBack;
            SetRaisedVisible(raised);
            RestoreVisualScales();
        }

        private void CaptureVisualScales()
        {
            if (visualScalesCaptured || side == null || border == null) return;
            sideFullScale = side.transform.localScale;
            borderFullScale = border.transform.localScale;
            if (outlineCanvas != null) outlineCanvasFullScale = outlineCanvas.localScale;
            visualScalesCaptured = true;
        }

        private void RestoreVisualScales()
        {
            if (!visualScalesCaptured) return;
            if (side != null) side.transform.localScale = sideFullScale;
            if (border != null) border.transform.localScale = borderFullScale;
            if (outlineCanvas != null) outlineCanvas.localScale = outlineCanvasFullScale;
        }

        private void SetRaisedVisible(bool visible)
        {
            if (side != null) side.enabled = visible;
            if (border != null) border.enabled = visible;
            if (outlineImage != null) outlineImage.enabled = false;
        }

        private static Vector3 Compressed(Vector3 fullScale)
        {
            fullScale.y = Mathf.Max(.001f, fullScale.y * .04f);
            return fullScale;
        }

        private void AlignUiOutline(Camera camera, Bounds bounds)
        {
            if (outlineCanvas == null || camera == null) return;
            Vector3 right = camera.transform.right;
            Vector3 up = camera.transform.up;
            float halfWidth = Mathf.Abs(right.x) * bounds.extents.x +
                Mathf.Abs(right.y) * bounds.extents.y + Mathf.Abs(right.z) * bounds.extents.z;
            float halfHeight = Mathf.Abs(up.x) * bounds.extents.x +
                Mathf.Abs(up.y) * bounds.extents.y + Mathf.Abs(up.z) * bounds.extents.z;
            const float outlineWorld = .055f;
            const float canvasUnits = 100f;
            float parentScale = Mathf.Max(.0001f, transform.lossyScale.x);
            outlineCanvas.sizeDelta = new Vector2(canvasUnits, canvasUnits);
            outlineCanvas.localScale = new Vector3(
                (halfWidth * 2f + outlineWorld * 2f) / (canvasUnits * parentScale),
                (halfHeight * 2f + outlineWorld * 2f) / (canvasUnits * parentScale), 1f);
            outlineCanvas.SetPositionAndRotation(
                bounds.center + camera.transform.forward * .08f, camera.transform.rotation);
        }

        private void AddBodyRenderer(Renderer renderer)
        {
            if (renderer == null) return;
            if (bodyRenderers == null)
            {
                bodyRenderers = new[] { renderer };
                return;
            }
            if (System.Array.IndexOf(bodyRenderers, renderer) >= 0) return;
            System.Array.Resize(ref bodyRenderers, bodyRenderers.Length + 1);
            bodyRenderers[bodyRenderers.Length - 1] = renderer;
        }

        private Bounds VisualBounds()
        {
            Renderer seed = face != null ? face : border != null ? border : side;
            var bounds = seed != null ? seed.bounds : new Bounds(transform.position, Vector3.one);
            if (bodyRenderers == null) return bounds;
            foreach (Renderer renderer in bodyRenderers)
                if (renderer != null) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private void ApplyWhiteBorder()
        {
            if (border == null) return;
            var properties = new MaterialPropertyBlock();
            border.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", Color.white);
            properties.SetColor("_Color", Color.white);
            border.SetPropertyBlock(properties);
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
            // Count is drawn by the gameplay Canvas, not by the world-space box.
            if (countLabel != null) countLabel.enabled = false;
        }
    }
}
