using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using ColonyFlow.Core.Pooling;

namespace ColonyFlow.Gameplay
{
    public enum AntTripState { Inactive, Outbound, WaitingPickup, Returning, Jumping }

    public sealed class AntActor : MonoBehaviour, IPoolable
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private GameObject carriedBrick;
        [SerializeField] private Renderer carriedRenderer;
        [SerializeField] private Renderer abdomen;
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField, Min(.01f)] private float jumpDuration = .55f;
        [SerializeField, Min(1)] private float jumpScaleMultiplier = 1.25f;
        private ActorAnimation animation;
        private Vector3 initialScale;
        private Transform carriedTransform;
        private Transform carryParent;
        private List<Vector3> route;
        private List<Vector3> returnRoute;
        private int waypoint;
        private Vector3 jumpTarget;
        private Vector3 carryLocalPosition, pickupPosition;
        private float pickupTime;
        public long TaskId { get; private set; }
        public int ColorId { get; private set; }
        public Cell Target { get; private set; }
        public BoxActor Source { get; private set; }
        public AntTripState State { get; private set; }
        public Material ColorMaterialTemplate => abdomen != null ? abdomen.sharedMaterial : null;

        private void Awake()
        {
            EnsureCachedState();
        }

        private void EnsureCachedState()
        {
            if (animation != null) return;
            initialScale = transform.localScale;
            animation = new ActorAnimation(gameObject);
            CacheCarryTransforms();
        }

        public void OnPoolSpawned()
        {
            EnsureCachedState();
            ResetTrip();
        }

        public void OnPoolRecycled() => ResetTrip();
        public void DetachSource() => Source = null;

        private void ResetTrip()
        {
            animation?.Cancel();
            TaskId = 0;
            ColorId = 0;
            Source = null;
            Target = null;
            route = null;
            returnRoute = null;
            waypoint = 0;
            pickupTime = .2f;
            State = AntTripState.Inactive;
            transform.localScale = initialScale;
            if (visualRoot != null) visualRoot.localPosition = Vector3.zero;
            if (carriedBrick != null) carriedBrick.SetActive(false);
            if (carriedTransform != null) carriedTransform.localPosition = carryLocalPosition;
        }

        public void Configure(Transform visual, Renderer brick, Renderer body)
        {
            visualRoot = visual;
            carriedRenderer = brick;
            carriedBrick = brick.gameObject;
            abdomen = body;
            CacheCarryTransforms();
        }

        private void CacheCarryTransforms()
        {
            if (carriedBrick == null) return;
            carriedTransform = carriedBrick.transform;
            carryParent = carriedTransform.parent;
            carryLocalPosition = carriedTransform.localPosition;
        }

        public void ConfigureJumpHeight(float height) => jumpHeight = height;

        public void Begin(long id, BoxActor source, Cell target, Vector3 spawn, List<Vector3> outbound,
            List<Vector3> returning, Vector3 jump, Material colorMaterial)
        {
            if (carriedTransform == null) CacheCarryTransforms();
            TaskId = id;
            ColorId = target.ColorId;
            Source = source;
            Target = target;
            route = outbound;
            returnRoute = returning;
            waypoint = 0;
            if (animation == null)
            {
                initialScale = transform.localScale;
                animation = new ActorAnimation(gameObject);
            }
            animation.Cancel();
            transform.localScale = initialScale;
            jumpTarget = jump;
            pickupTime = .2f;
            transform.position = spawn;
            State = AntTripState.Outbound;
            carriedBrick.SetActive(false);
            if (colorMaterial != null)
            {
                abdomen.sharedMaterial = colorMaterial;
                carriedRenderer.sharedMaterial = colorMaterial;
            }
            gameObject.SetActive(true);
        }

        public void RemapCardRoute(System.Func<Vector3, Vector3> remap)
        {
            if (State == AntTripState.Inactive || State == AntTripState.Jumping) return;
            transform.position = remap(transform.position);
            RemapWaypoints(route, waypoint, remap);
            if (returnRoute != route) RemapWaypoints(returnRoute, 0, remap);
        }

        private static void RemapWaypoints(List<Vector3> points, int start, System.Func<Vector3, Vector3> remap)
        {
            if (points == null) return;
            for (int i = start; i < points.Count; i++) points[i] = remap(points[i]);
        }

        public void ConfirmPickup(Vector3 brickPosition, Vector3 brickWorldScale)
        {
            if (State != AntTripState.WaitingPickup) return;
            SetCarryScale(brickWorldScale);
            carriedBrick.SetActive(true);
            pickupPosition = brickPosition;
            pickupTime = 0;
            carriedTransform.position = brickPosition;
            route = returnRoute;
            waypoint = 0;
            State = AntTripState.Returning;
        }

        private void SetCarryScale(Vector3 worldScale)
        {
            var parentScale = carryParent.lossyScale;
            carriedTransform.localScale = new Vector3(
                worldScale.x / parentScale.x, worldScale.y / parentScale.y, worldScale.z / parentScale.z);
        }

        public void Advance(float delta, float speed)
        {
            AdvanceTrip(delta, speed);
            AdvancePickup(delta);
        }

        private void AdvancePickup(float delta)
        {
            if ((State != AntTripState.Returning && State != AntTripState.Jumping) || pickupTime >= .2f) return;
            pickupTime = Mathf.Min(.2f, pickupTime + delta);
            carriedTransform.position = Vector3.Lerp(pickupPosition,
                carryParent.TransformPoint(carryLocalPosition), pickupTime / .2f);
            if (pickupTime >= .2f) carriedTransform.localPosition = carryLocalPosition;
        }

        private void AdvanceTrip(float delta, float speed)
        {
            if (State == AntTripState.Inactive || State == AntTripState.WaitingPickup) return;
            if (State == AntTripState.Jumping) { animation.Advance(delta); return; }
            if (!WalkRoute(delta * speed)) return;
            if (State == AntTripState.Outbound) State = AntTripState.WaitingPickup;
            else BeginJump();
        }

        private void FinishJump()
        {
            State = AntTripState.Inactive;
            gameObject.SetActive(false);
        }

        private void BeginJump()
        {
            State = AntTripState.Jumping;
            FaceDirection(jumpTarget - transform.position);
            var jump = DOTween.Sequence()
                .Append(transform.DOJump(jumpTarget, jumpHeight, 1, jumpDuration).SetEase(Ease.Linear))
                .Join(DOTween.Sequence()
                    .Append(transform.DOScale(initialScale * jumpScaleMultiplier, jumpDuration * .25f).SetEase(Ease.OutQuad))
                    .Append(transform.DOScale(Vector3.zero, jumpDuration * .75f).SetEase(Ease.InQuad)))
                .OnComplete(FinishJump);
            if (animation.Play(jump)) return;
            transform.position = jumpTarget;
            transform.localScale = Vector3.zero;
            FinishJump();
        }

        private bool WalkRoute(float remaining)
        {
            while (waypoint < route.Count)
            {
                var next = route[waypoint];
                var direction = next - transform.position;
                float distance = direction.magnitude;
                FaceDirection(direction);
                if (distance > remaining)
                {
                    transform.position += direction.normalized * remaining;
                    return false;
                }
                transform.position = next;
                remaining -= distance;
                waypoint++;
            }
            return true;
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.x * direction.x + direction.z * direction.z > .0001f)
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        }

        public void Cancel()
        {
            animation?.Cancel();
            State = AntTripState.Inactive;
            Source = null;
            Target = null;
            route = null;
            returnRoute = null;
            carriedBrick.SetActive(false);
            gameObject.SetActive(false);
        }

        private void OnDisable() => animation?.Cancel();
        private void OnDestroy() => animation?.Dispose();
    }
}
