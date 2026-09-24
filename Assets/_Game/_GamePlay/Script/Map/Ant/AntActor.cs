using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using ColonyFlow.Core.Pooling;

namespace ColonyFlow.Gameplay
{
    public enum AntTripState
    {
        Inactive,
        Outbound,
        FacingPickup,
        PickingUp,
        WaitingPickup,
        LiftingBrick,
        FacingReturn,
        Returning,
        FacingJump,
        Jumping
    }

    public sealed class AntActor : MonoBehaviour, IPoolable
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private GameObject carriedBrick;
        [SerializeField] private Renderer carriedRenderer;
        [SerializeField] private Renderer abdomen;
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField, Min(.01f)] private float jumpDuration = .55f;
        [SerializeField, Min(0f)] private float pickupAnimationDuration = .25f;
        [SerializeField, Min(.01f)] private float brickLiftDuration = .25f;
        [SerializeField, Min(0f)] private float brickLiftSpinDegrees = 360f;
        [SerializeField, Min(.01f)] private float turnSmoothTime = .12f;
        [SerializeField, Range(.1f, 15f)] private float pickupFacingAngle = 2f;
        [SerializeField, Min(1)] private float jumpScaleMultiplier = 1.25f;
        [SerializeField, Min(0f)] private float holeAvoidanceOffset = .2f;
        [SerializeField, Min(0f)] private float holeTurnLeadDistance = 1.2f;
        private new ActorAnimation animation;
        private ProceduralAntAnimation proceduralAnimation;
        private Vector3 initialScale;
        private Transform carriedTransform;
        private Transform carryParent;
        private List<Vector3> route;
        private List<Vector3> returnRoute;
        private int waypoint;
        private Vector3 jumpTarget;
        private Vector3 pickupLookAtPosition;
        private Vector3 carryLocalPosition, pickupPosition;
        private Quaternion carryLocalRotation, pickupRotation;
        private float pickupTime;
        private float pickupAnimationTime;
        private float currentYaw;
        private float yawVelocity;
        public long TaskId { get; private set; }
        public int ColorId { get; private set; }
        public Cell Target { get; private set; }
        public BoxActor Source { get; private set; }
        public AntTripState State { get; private set; }
        public Material ColorMaterialTemplate => abdomen != null ? abdomen.sharedMaterial : null;
        public float HoleAvoidanceOffset => holeAvoidanceOffset;
        public float HoleTurnLeadDistance => holeTurnLeadDistance;

        private void Awake()
        {
            EnsureCachedState();
        }

        private void EnsureCachedState()
        {
            if (proceduralAnimation == null) proceduralAnimation = GetComponent<ProceduralAntAnimation>();
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
            proceduralAnimation?.ResetPose();
            TaskId = 0;
            ColorId = 0;
            Source = null;
            Target = null;
            route = null;
            returnRoute = null;
            waypoint = 0;
            pickupTime = .2f;
            pickupAnimationTime = 0;
            State = AntTripState.Inactive;
            transform.localScale = initialScale;
            SyncYaw();
            if (visualRoot != null) visualRoot.localPosition = Vector3.zero;
            if (carriedBrick != null) carriedBrick.SetActive(false);
            if (carriedTransform != null)
            {
                carriedTransform.localPosition = carryLocalPosition;
                carriedTransform.localRotation = carryLocalRotation;
            }
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
            carryLocalRotation = carriedTransform.localRotation;
        }

        public void ConfigureJumpHeight(float height) => jumpHeight = height;

        public void Begin(long id, BoxActor source, Cell target, Vector3 spawn, List<Vector3> outbound,
            List<Vector3> returning, Vector3 pickupLookAt, Vector3 jump, Material colorMaterial)
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
            pickupLookAtPosition = pickupLookAt;
            pickupTime = .2f;
            pickupAnimationTime = 0;
            transform.position = spawn;
            FaceDirectionImmediately(FirstRouteDirection());
            State = AntTripState.Outbound;
            proceduralAnimation?.SetWalking(false);
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
            pickupLookAtPosition = remap(pickupLookAtPosition);
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
            pickupRotation = carriedTransform.rotation;
            route = returnRoute;
            waypoint = 0;
            State = AntTripState.LiftingBrick;
            proceduralAnimation?.SetIdle();
        }

        private void SetCarryScale(Vector3 worldScale)
        {
            var parentScale = carryParent.lossyScale;
            carriedTransform.localScale = new Vector3(
                worldScale.x / parentScale.x, worldScale.y / parentScale.y, worldScale.z / parentScale.z);
        }

        public void Advance(float animationDelta, float movementDistance)
        {
            proceduralAnimation?.Advance(animationDelta);
            AdvanceTrip(animationDelta, movementDistance);
            AdvancePickup(animationDelta);
        }

        private void AdvancePickup(float delta)
        {
            if (State != AntTripState.LiftingBrick || carriedTransform == null || carryParent == null) return;
            pickupTime = Mathf.Min(brickLiftDuration, pickupTime + delta);
            float progress = Mathf.Clamp01(pickupTime / brickLiftDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            carriedTransform.position = Vector3.Lerp(pickupPosition,
                carryParent.TransformPoint(carryLocalPosition), eased);
            Quaternion targetRotation = carryParent.rotation * carryLocalRotation;
            carriedTransform.rotation = Quaternion.AngleAxis(brickLiftSpinDegrees * eased, Vector3.up) *
                Quaternion.Slerp(pickupRotation, targetRotation, eased);
            if (pickupTime < brickLiftDuration) return;
            carriedTransform.localPosition = carryLocalPosition;
            carriedTransform.localRotation = carryLocalRotation;
            State = AntTripState.FacingReturn;
            proceduralAnimation?.SetIdle();
        }

        private void AdvanceTrip(float animationDelta, float movementDistance)
        {
            if (State == AntTripState.Inactive || State == AntTripState.WaitingPickup ||
                State == AntTripState.LiftingBrick) return;
            if (State == AntTripState.FacingPickup)
            {
                if (!SmoothFace(pickupLookAtPosition - transform.position, animationDelta)) return;
                State = AntTripState.PickingUp;
                pickupAnimationTime = 0;
                proceduralAnimation?.SetPickingUp();
                return;
            }
            if (State == AntTripState.FacingReturn)
            {
                if (!SmoothFace(FirstRouteDirection(), animationDelta)) return;
                State = AntTripState.Returning;
                proceduralAnimation?.SetWalking(true);
                return;
            }
            if (State == AntTripState.FacingJump)
            {
                if (!SmoothFace(jumpTarget - transform.position, animationDelta)) return;
                StartJump();
                return;
            }
            if (State == AntTripState.PickingUp)
            {
                pickupAnimationTime += animationDelta;
                if (pickupAnimationTime >= pickupAnimationDuration)
                {
                    State = AntTripState.WaitingPickup;
                    proceduralAnimation?.SetIdle();
                }
                return;
            }
            if (State == AntTripState.Jumping) { animation.Advance(animationDelta); return; }
            if (!WalkRoute(movementDistance, animationDelta)) return;
            if (State == AntTripState.Outbound)
            {
                State = AntTripState.FacingPickup;
                proceduralAnimation?.SetIdle();
            }
            else BeginFacingJump();
        }

        private void FinishJump()
        {
            State = AntTripState.Inactive;
            gameObject.SetActive(false);
        }

        private void BeginFacingJump()
        {
            State = AntTripState.FacingJump;
            proceduralAnimation?.SetIdle();
        }

        private void StartJump()
        {
            State = AntTripState.Jumping;
            proceduralAnimation?.SetJumping();
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

        private bool WalkRoute(float remaining, float turnDelta)
        {
            bool rotated = false;
            while (waypoint < route.Count)
            {
                var next = route[waypoint];
                var direction = next - transform.position;
                float distance = direction.magnitude;
                if (!rotated && distance > .0001f)
                {
                    SmoothFace(direction, turnDelta);
                    rotated = true;
                }
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

        private Vector3 FirstRouteDirection()
        {
            if (route == null) return transform.forward;
            for (int index = waypoint; index < route.Count; index++)
            {
                Vector3 direction = route[index] - transform.position;
                if (direction.x * direction.x + direction.z * direction.z > .0001f) return direction;
            }
            return transform.forward;
        }

        private bool SmoothFace(Vector3 direction, float delta)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= .0001f) return true;
            float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity,
                turnSmoothTime, Mathf.Infinity, delta);
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
            return Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw)) <= pickupFacingAngle;
        }

        private void SyncYaw()
        {
            currentYaw = transform.eulerAngles.y;
            yawVelocity = 0f;
        }

        private void FaceDirectionImmediately(Vector3 direction)
        {
            if (direction.x * direction.x + direction.z * direction.z > .0001f)
            {
                currentYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                yawVelocity = 0f;
                transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
            }
        }

        public void Cancel()
        {
            animation?.Cancel();
            proceduralAnimation?.ResetPose();
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
