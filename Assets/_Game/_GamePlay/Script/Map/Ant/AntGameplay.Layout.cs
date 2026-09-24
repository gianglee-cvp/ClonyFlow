using System;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    [Serializable]
    public sealed class GameplaySceneLayout
    {
        [Header("Camera")]
        public Vector3 cameraPosition = new Vector3(0f, 56.56854f, -60.33054f);
        public Vector3 cameraRotation = new Vector3(45f, 0f, 0f);
        [Min(.01f)] public float orthographicSize = 9f;

        [Header("Positions (Portrait Viewport 0-1)")]
        public Vector2 hole = new Vector2(.5f, .46f);
        public Vector2 slotRowCenter = new Vector2(.5f, .40f);
        [Range(0f, 1f)] public float slotSpacing = .135f;
        [Tooltip("Queue position relative to Slot Row Center.")]
        public Vector2 queueOffsetFromSlots = new Vector2(0f, -.08f);
        [Range(0f, 1f)] public float queueSpacing = .135f;
        public Vector2 cardMin = new Vector2(.04f, .49f);
        public Vector2 cardMax = new Vector2(.96f, .93f);

        [Header("Sizes")]
        [Min(1), Tooltip("Minimum permanent slots in the scene. Apply Layout adds missing slots without rebuilding assets.")]
        public int minimumSlotCount = 5;
        [Min(0f)] public float mapPadding = .12f;
        [Min(0f)] public float cardInsetPixels = 22f;
        [Range(.05f, .25f)] public float slotWidth = .115f;
        [Range(.5f, 1f), Tooltip("How much of a slot/queue column each 3D box occupies.")]
        public float queueBoxFill = .88f;
        [Range(.2f, .8f), Tooltip("Visible box height relative to its width. Higher values look more raised.")]
        public float queueBoxHeightRatio = .45f;
        [Range(.03f, .15f)] public float queueRowSpacing = .066f;
        [Range(.05f, .3f)] public float holeWidth = .16f;
        [Range(.03f, .2f)] public float holeDepth = .09f;

        public Vector2 QueueCenter => slotRowCenter + queueOffsetFromSlots;

        public void Validate()
        {
            orthographicSize = Mathf.Max(.01f, orthographicSize);
            hole = ClampViewport(hole);
            slotRowCenter = ClampViewport(slotRowCenter);
            slotSpacing = Mathf.Clamp01(slotSpacing);
            queueOffsetFromSlots = new Vector2(
                Mathf.Clamp(queueOffsetFromSlots.x, -slotRowCenter.x, 1f - slotRowCenter.x),
                Mathf.Clamp(queueOffsetFromSlots.y, -slotRowCenter.y, 1f - slotRowCenter.y));
            queueSpacing = Mathf.Clamp01(queueSpacing);
            cardMin = ClampViewport(cardMin);
            cardMax = ClampViewport(cardMax);
            cardMax = new Vector2(Mathf.Max(cardMin.x, cardMax.x), Mathf.Max(cardMin.y, cardMax.y));
            minimumSlotCount = minimumSlotCount <= 0 ? 5 : Mathf.Clamp(minimumSlotCount, 1, 10);
            mapPadding = Mathf.Max(0f, mapPadding);
            cardInsetPixels = Mathf.Max(0f, cardInsetPixels);
            slotWidth = Mathf.Clamp(slotWidth, .05f, .25f);
            queueBoxFill = queueBoxFill <= 0f ? .88f : Mathf.Clamp(queueBoxFill, .5f, 1f);
            queueBoxHeightRatio = queueBoxHeightRatio <= 0f
                ? .45f : Mathf.Clamp(queueBoxHeightRatio, .2f, .8f);
            queueRowSpacing = Mathf.Clamp(queueRowSpacing, .03f, .15f);
            holeWidth = Mathf.Clamp(holeWidth, .05f, .3f);
            holeDepth = Mathf.Clamp(holeDepth, .03f, .2f);
        }

        private static Vector2 ClampViewport(Vector2 value) =>
            new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
    }

    public sealed partial class AntGameplay
    {
        [Header("Scene Layout Authoring")]
        [SerializeField] private GameplaySceneLayout sceneLayout = new GameplaySceneLayout();
        [SerializeField, Tooltip("Apply Inspector changes immediately while not in Play Mode.")]
        private bool liveLayoutPreview;

        public GameplaySceneLayout SceneLayout => sceneLayout;

        private void OnValidate()
        {
            sceneLayout ??= new GameplaySceneLayout();
            sceneLayout.Validate();
            if (liveLayoutPreview && !Application.isPlaying) ApplySceneLayout(false);
        }

        public bool ApplySceneLayout(bool addMissingSlots = true)
        {
            sceneLayout ??= new GameplaySceneLayout();
            sceneLayout.Validate();
            if (!HasLayoutReferences()) return false;
            if (addMissingSlots && !EnsureMinimumSlotCount()) return false;

            gameplayCamera.orthographic = true;
            gameplayCamera.transform.SetPositionAndRotation(
                sceneLayout.cameraPosition, Quaternion.Euler(sceneLayout.cameraRotation));
            gameplayCamera.orthographicSize = sceneLayout.orthographicSize;

            ApplyCardLayout();
            Bounds cardBounds = MapPerimeter.CardBounds(mapView.Root, mapCardSurface);
            ApplySlotLayout(cardBounds);
            ApplyQueueLayout();
            ApplyHoleLayout(cardBounds);

            mapView.ConfigureMapPadding(sceneLayout.mapPadding);
            float portraitWidth = PortraitWorldWidth();
            queueColumnStep = portraitWidth * sceneLayout.queueSpacing;
            queueRowStep = 2f * gameplayCamera.orthographicSize * sceneLayout.queueRowSpacing /
                Mathf.Max(.001f, Mathf.Abs(gameplayCamera.transform.up.z));
            return true;
        }

        public bool CaptureSceneLayout()
        {
            if (!HasLayoutReferences()) return false;
            sceneLayout ??= new GameplaySceneLayout();

            sceneLayout.cameraPosition = gameplayCamera.transform.position;
            sceneLayout.cameraRotation = gameplayCamera.transform.eulerAngles;
            sceneLayout.orthographicSize = gameplayCamera.orthographicSize;
            CaptureSlots();
            CaptureQueues();
            sceneLayout.hole = Viewport(holeReturn.position);
            CaptureCard();
            CaptureSizes();
            sceneLayout.mapPadding = mapView.MapPadding;
            sceneLayout.Validate();
            return true;
        }

        private bool HasLayoutReferences() => gameplayCamera != null && mapView != null && mapView.Root != null &&
            mapCardSurface != null && holeReturn != null && boxAnchors != null && boxAnchors.Length > 0 &&
            queueAnchors != null && queueAnchors.Length > 0;

        private bool EnsureMinimumSlotCount()
        {
            if (boxAnchors.Length >= sceneLayout.minimumSlotCount) return true;
            if (boxAnchors.Length == 0 || antSpawnPoints == null || perimeterEntries == null || slotSurfaces == null ||
                antSpawnPoints.Length != boxAnchors.Length || perimeterEntries.Length != boxAnchors.Length ||
                slotSurfaces.Length != boxAnchors.Length || slotSurfaces[slotSurfaces.Length - 1] == null)
                return false;

            while (boxAnchors.Length < sceneLayout.minimumSlotCount)
            {
                int index = boxAnchors.Length;
                Transform seed = slotSurfaces[index - 1].transform.parent;
                if (seed == null) return false;
                Transform clone = Instantiate(seed.gameObject, seed.parent).transform;
                clone.name = $"Slot {index}";

                Transform anchor = clone.Find("BoxAnchor");
                Transform spawn = clone.Find("AntSpawnPoint");
                Transform entry = clone.Find("PerimeterEntry");
                Renderer surface = clone.Find("Empty Slot")?.GetComponent<Renderer>();
                if (anchor == null || spawn == null || entry == null || surface == null)
                {
                    if (Application.isPlaying) Destroy(clone.gameObject);
                    else DestroyImmediate(clone.gameObject);
                    return false;
                }

                Append(ref boxAnchors, anchor);
                Append(ref antSpawnPoints, spawn);
                Append(ref perimeterEntries, entry);
                Append(ref slotSurfaces, surface);
            }
            return true;
        }

        private static void Append<T>(ref T[] values, T value)
        {
            int index = values.Length;
            Array.Resize(ref values, index + 1);
            values[index] = value;
        }

        private void ApplyCardLayout()
        {
            Transform card = mapCardSurface.transform.parent;
            if (card == null) return;
            Vector3 bottom = PortraitWorld(sceneLayout.cardMin);
            Vector3 top = PortraitWorld(sceneLayout.cardMax);
            Vector3 targetCenter = (bottom + top) * .5f;
            Vector3 cardDelta = targetCenter - mapCardSurface.bounds.center;
            cardDelta.y = 0f;
            card.position += cardDelta;

            float width = Mathf.Abs(top.x - bottom.x);
            float depth = Mathf.Abs(top.z - bottom.z);
            float inset = sceneLayout.cardInsetPixels * (2f * gameplayCamera.orthographicSize / 1920f);
            ResizeRenderer(card.Find("Frame")?.GetComponent<Renderer>(), width, depth);
            ResizeRenderer(card.Find("Shadow")?.GetComponent<Renderer>(), width, depth);
            ResizeRenderer(mapCardSurface, Mathf.Max(.01f, width - inset), Mathf.Max(.01f, depth - inset));
        }

        private void ApplySlotLayout(Bounds bounds)
        {
            float portraitWidth = PortraitWorldWidth();
            float boxWidth = portraitWidth * sceneLayout.slotWidth;
            float boxDepth = (boxWidth - .14f * Mathf.Abs(gameplayCamera.transform.up.y)) /
                Mathf.Max(.001f, Mathf.Abs(gameplayCamera.transform.up.z));
            for (int i = 0; i < boxAnchors.Length; i++)
            {
                if (boxAnchors[i] == null) continue;
                Vector3 target = PortraitWorld(new Vector2(
                    sceneLayout.slotRowCenter.x + (i - (boxAnchors.Length - 1) * .5f) * sceneLayout.slotSpacing,
                    sceneLayout.slotRowCenter.y));
                Transform slot = boxAnchors[i].parent;
                if (slot != null) slot.position += target - boxAnchors[i].position;
                else boxAnchors[i].position = target;
                if (slotSurfaces != null && i < slotSurfaces.Length)
                    ResizeRenderer(slotSurfaces[i], boxWidth, boxDepth);
                if (perimeterEntries != null && i < perimeterEntries.Length && perimeterEntries[i] != null)
                    perimeterEntries[i].position = new Vector3(
                        Mathf.Clamp(target.x, bounds.min.x, bounds.max.x), 0f, bounds.min.z);
            }
        }

        private void ApplyQueueLayout()
        {
            Vector2 center = sceneLayout.QueueCenter;
            for (int i = 0; i < queueAnchors.Length; i++)
            {
                if (queueAnchors[i] == null) continue;
                queueAnchors[i].position = PortraitWorld(new Vector2(
                    center.x + (i - (queueAnchors.Length - 1) * .5f) * sceneLayout.queueSpacing, center.y));
            }
        }

        private void ApplyHoleLayout(Bounds bounds)
        {
            Vector3 target = PortraitWorld(sceneLayout.hole);
            Transform holeRoot = holeReturn.parent;
            if (holeRoot != null) holeRoot.position += target - holeReturn.position;
            else holeReturn.position = target;

            float portraitWidth = PortraitWorldWidth();
            var rim = holeRoot?.Find("Hole Rim")?.GetComponent<Renderer>();
            var dark = holeRoot?.Find("Dark Hole")?.GetComponent<Renderer>();
            ResizeRenderer(rim, portraitWidth * sceneLayout.holeWidth, portraitWidth * sceneLayout.holeDepth);
            ResizeRenderer(dark, portraitWidth * sceneLayout.holeWidth * .84f,
                portraitWidth * sceneLayout.holeDepth * .76f);
            if (holeExit != null) holeExit.position = new Vector3(mapView.Root.position.x, 0f, bounds.min.z);
        }

        private void CaptureSlots()
        {
            Vector2 center = Vector2.zero;
            var xs = new float[boxAnchors.Length];
            int count = 0;
            foreach (Transform anchor in boxAnchors)
            {
                if (anchor == null) continue;
                Vector2 point = Viewport(anchor.position);
                center += point;
                xs[count++] = point.x;
            }
            if (count == 0) return;
            sceneLayout.slotRowCenter = center / count;
            if (count < 2) return;
            Array.Sort(xs, 0, count);
            float spacing = 0f;
            for (int i = 1; i < count; i++) spacing += xs[i] - xs[i - 1];
            sceneLayout.slotSpacing = spacing / (count - 1);
        }

        private void CaptureQueues()
        {
            Vector2 center = Vector2.zero;
            var xs = new float[queueAnchors.Length];
            int count = 0;
            foreach (Transform anchor in queueAnchors)
            {
                if (anchor == null) continue;
                Vector2 point = Viewport(anchor.position);
                center += point;
                xs[count++] = point.x;
            }
            if (count == 0) return;
            center /= count;
            sceneLayout.queueOffsetFromSlots = center - sceneLayout.slotRowCenter;
            if (count < 2) return;
            Array.Sort(xs, 0, count);
            float spacing = 0f;
            for (int i = 1; i < count; i++) spacing += xs[i] - xs[i - 1];
            sceneLayout.queueSpacing = spacing / (count - 1);
        }

        private void CaptureCard()
        {
            var frame = mapCardSurface.transform.parent?.Find("Frame")?.GetComponent<Renderer>();
            if (frame == null) return;
            ViewportBounds(frame.bounds, out sceneLayout.cardMin, out sceneLayout.cardMax);
            float worldUnitsPerPixel = 2f * gameplayCamera.orthographicSize / 1920f;
            sceneLayout.cardInsetPixels = Mathf.Max(0f,
                (frame.bounds.size.x - mapCardSurface.bounds.size.x) / worldUnitsPerPixel);
        }

        private void CaptureSizes()
        {
            float width = PortraitWorldWidth();
            if (slotSurfaces != null && slotSurfaces.Length > 0 && slotSurfaces[0] != null)
                sceneLayout.slotWidth = slotSurfaces[0].bounds.size.x / width;
            var rim = holeReturn.parent?.Find("Hole Rim")?.GetComponent<Renderer>();
            if (rim != null)
            {
                sceneLayout.holeWidth = rim.bounds.size.x / width;
                sceneLayout.holeDepth = rim.bounds.size.z / width;
            }
            sceneLayout.queueRowSpacing = queueRowStep * Mathf.Abs(gameplayCamera.transform.up.z) /
                (2f * gameplayCamera.orthographicSize);
        }

        private Vector3 PortraitWorld(Vector2 viewport)
        {
            float size = gameplayCamera.orthographicSize;
            Vector3 origin = gameplayCamera.transform.position +
                gameplayCamera.transform.right * ((viewport.x - .5f) * 2f * size * 1080f / 1920f) +
                gameplayCamera.transform.up * ((viewport.y - .5f) * 2f * size);
            var ray = new Ray(origin, gameplayCamera.transform.forward);
            return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)
                ? ray.GetPoint(distance) : origin;
        }

        private Vector2 Viewport(Vector3 world)
        {
            Vector3 local = gameplayCamera.transform.InverseTransformPoint(world);
            float size = gameplayCamera.orthographicSize;
            return new Vector2(.5f + local.x / (2f * size * 1080f / 1920f), .5f + local.y / (2f * size));
        }

        private void ViewportBounds(Bounds bounds, out Vector2 min, out Vector2 max)
        {
            min = Vector2.one;
            max = Vector2.zero;
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector2 point = Viewport(new Vector3(
                    bounds.center.x + x * bounds.extents.x, bounds.center.y, bounds.center.z + z * bounds.extents.z));
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }

        private float PortraitWorldWidth() => 2f * gameplayCamera.orthographicSize * 1080f / 1920f;

        private void FitBoxToLayout(BoxActor box)
        {
            if (box == null || slotSurfaces == null || slotSurfaces.Length == 0 || slotSurfaces[0] == null) return;
            Vector3 slotSize = slotSurfaces[0].bounds.size;
            float queueWidth = queueColumnStep > .001f ? queueColumnStep : slotSize.x;
            box.FitToFootprint(Mathf.Min(slotSize.x, queueWidth), slotSize.z,
                sceneLayout != null ? sceneLayout.queueBoxFill : .88f,
                sceneLayout != null ? sceneLayout.queueBoxHeightRatio : .45f, gameplayCamera);
        }

        private Vector3 SlotLandingPosition(BoxActor box, int slotIndex)
        {
            if (box == null || boxAnchors == null || slotIndex < 0 || slotIndex >= boxAnchors.Length)
                return box != null ? box.transform.position : Vector3.zero;
            Renderer surface = slotSurfaces != null && slotIndex < slotSurfaces.Length
                ? slotSurfaces[slotIndex]
                : extraSlotSurface;
            box.SetVisualState(BoxVisualState.Slot);
            return box.RestOnSlot(boxAnchors[slotIndex].position, surface);
        }

        private static void ResizeRenderer(Renderer renderer, float width, float depth)
        {
            if (renderer == null || renderer.bounds.size.x <= .0001f || renderer.bounds.size.z <= .0001f) return;
            Vector3 scale = renderer.transform.localScale;
            scale.x *= width / renderer.bounds.size.x;
            scale.z *= depth / renderer.bounds.size.z;
            renderer.transform.localScale = scale;
        }
    }
}
