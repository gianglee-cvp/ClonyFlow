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
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private Renderer[] detailRenderers;
        [SerializeField] private SpriteRenderer contactShadow;
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField, Min(.01f)] private float jumpDuration = .55f;
        [SerializeField, Min(0f)] private float pickupAnimationDuration = .25f;
        [SerializeField, Min(.01f)] private float brickLiftDuration = .25f;
        [SerializeField, Min(0f)] private float brickLiftSpinDegrees = 360f;
        [SerializeField, Min(.01f)] private float turnRadius = .35f;
        [SerializeField, Range(3, 16)] private int turnCurveSteps = 8;
        [SerializeField, Min(.01f)] private float turnLookAheadDistance = .4f;
        [SerializeField, Min(1f)] private float turnDegreesPerCell = 210f;
        [SerializeField, Min(1f)] private float turnDegreesPerSecond = 720f;
        [SerializeField, Min(.01f)] private float turnRotateSpeed = 12f;
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
        private Vector3 routeStartPosition;
        private int waypoint;
        private Vector3 jumpTarget;
        private Vector3 pickupLookAtPosition;
        private Vector3 carryLocalPosition, pickupPosition;
        private Quaternion carryLocalRotation, pickupRotation;
        private float pickupTime;
        private float pickupAnimationTime;
        private float turnCellWidth;
        private float frameTurnDelta;
        private float frameMovement;
        private Vector3 frameHeading;
        private System.Func<Vector3, bool> canTurnAt;
        private readonly List<TurnSample> turnSamples = new List<TurnSample>(17);
        private float turnDistance;
        private float turnLength;
        private int turnExitWaypoint;
        private bool returnTurn;

        private struct TurnSample
        {
            public Vector3 Position;
            public Vector3 Tangent;
            public float Distance;
        }
        public long TaskId { get; private set; }
        public int ColorId { get; private set; }
        public Cell Target { get; private set; }
        public BoxActor Source { get; private set; }
        public AntTripState State { get; private set; }
        public Material ColorMaterialTemplate => abdomen != null ? abdomen.sharedMaterial : null;
        public float HoleAvoidanceOffset => holeAvoidanceOffset;
        public float HoleTurnLeadDistance => holeTurnLeadDistance;

        public void AddFocusRenderers(List<Renderer> targets)
        {
            if (bodyRenderers != null) targets.AddRange(bodyRenderers);
            if (detailRenderers != null) targets.AddRange(detailRenderers);
            if (carriedRenderer != null) targets.Add(carriedRenderer);
            if (contactShadow != null) targets.Add(contactShadow);
        }

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
            if (contactShadow != null) contactShadow.enabled = false;
            TaskId = 0;
            ColorId = 0;
            Source = null;
            Target = null;
            route = null;
            returnRoute = null;
            routeStartPosition = Vector3.zero;
            canTurnAt = null;
            turnCellWidth = 0f;
            frameTurnDelta = frameMovement = 0f;
            frameHeading = Vector3.zero;
            ClearTurn();
            waypoint = 0;
            pickupTime = .2f;
            pickupAnimationTime = 0;
            State = AntTripState.Inactive;
            transform.localScale = initialScale;
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

        public void ConfigureRenderers(Renderer[] body, Renderer[] details)
        {
            bodyRenderers = body;
            detailRenderers = details;
        }

        private void ApplyBodyColor(Material colorMaterial)
        {
            if (colorMaterial == null || bodyRenderers == null) return;
            foreach (var renderer in bodyRenderers)
                if (renderer != null) renderer.sharedMaterial = colorMaterial;
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
        public void ConfigureContactShadow(SpriteRenderer renderer) => contactShadow = renderer;

        private void ShowContactShadow()
        {
            if (contactShadow != null) contactShadow.enabled = true;
        }

        public void Begin(long id, BoxActor source, Cell target, Vector3 spawn, List<Vector3> outbound,
            List<Vector3> returning, Vector3 pickupLookAt, Vector3 jump, Material colorMaterial,
            float fittedCellWidth, System.Func<Vector3, bool> canTurnAt)
        {
            if (carriedTransform == null) CacheCarryTransforms();
            TaskId = id;
            ColorId = target.ColorId;
            Source = source;
            Target = target;
            route = outbound;
            returnRoute = returning;
            routeStartPosition = spawn;
            turnCellWidth = fittedCellWidth;
            this.canTurnAt = canTurnAt;
            ClearTurn();
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
            ShowContactShadow();
            proceduralAnimation?.SetWalking(false);
            carriedBrick.SetActive(false);
            if (colorMaterial != null)
            {
                ApplyBodyColor(colorMaterial);
                carriedRenderer.sharedMaterial = colorMaterial;
            }
            gameObject.SetActive(true);
        }

        public void RemapCardRoute(System.Func<Vector3, Vector3> remap)
        {
            if (State == AntTripState.Inactive || State == AntTripState.Jumping) return;
            bool wasReturnTurn = turnSamples.Count > 0 && returnTurn;
            transform.position = remap(transform.position);
            routeStartPosition = remap(routeStartPosition);
            pickupLookAtPosition = remap(pickupLookAtPosition);
            RemapWaypoints(route, waypoint, remap);
            if (returnRoute != route) RemapWaypoints(returnRoute, 0, remap);
            ClearTurn();
            if (wasReturnTurn) TryStartReturnTurn();
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
            routeStartPosition = transform.position;
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

        public void Advance(float animationDelta, float movementDistance, float speedMultiplier = 1f)
        {
            frameTurnDelta = animationDelta * speedMultiplier;
            frameMovement = 0f;
            frameHeading = Vector3.zero;
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
            State = AntTripState.Returning;
            proceduralAnimation?.SetWalking(true);
            TryStartReturnTurn();
        }

        private void AdvanceTrip(float animationDelta, float movementDistance)
        {
            if (State == AntTripState.Inactive || State == AntTripState.WaitingPickup ||
                State == AntTripState.LiftingBrick) return;
            // Legacy Facing states are accepted for old pooled actors, but never wait and
            // rotate in place. Normal turns are handled by the moving route below.
            if (State == AntTripState.FacingPickup) { BeginPickupAnimation(); return; }
            if (State == AntTripState.FacingReturn) State = AntTripState.Returning;
            if (State == AntTripState.FacingJump) { StartJump(); return; }
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
            bool routeComplete = WalkRoute(movementDistance);
            RotateToMovement();
            if (!routeComplete) return;
            if (State == AntTripState.Outbound)
                BeginPickupAnimation();
            else StartJump();
        }

        private void BeginPickupAnimation()
        {
            State = AntTripState.PickingUp;
            pickupAnimationTime = 0;
            proceduralAnimation?.SetPickingUp();
        }

        private void FinishJump()
        {
            State = AntTripState.Inactive;
            gameObject.SetActive(false);
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

        private bool WalkRoute(float remaining)
        {
            while (true)
            {
                if (turnSamples.Count > 0 && !AdvanceTurn(ref remaining)) return false;
                if (waypoint >= route.Count) return true;

                Vector3 next = route[waypoint];
                Vector3 direction = next - transform.position;
                float distance = direction.magnitude;
                if (distance <= .0001f) { waypoint++; continue; }

                if (TryStartCornerTurn(distance, out float approachDistance))
                    continue;
                if (approachDistance > .0001f)
                {
                    float step = Mathf.Min(remaining, approachDistance);
                    TrackMovement(direction, step);
                    transform.position += direction / distance * step;
                    remaining -= step;
                    if (remaining <= .000001f) return false;
                    continue;
                }

                if (distance > remaining)
                {
                    TrackMovement(direction, remaining);
                    transform.position += direction / distance * remaining;
                    return false;
                }
                TrackMovement(direction, distance);
                transform.position = next;
                remaining -= distance;
                waypoint++;
            }
        }

        private bool TryStartCornerTurn(float distance, out float approachDistance)
        {
            approachDistance = 0f;
            if (canTurnAt == null || turnCellWidth <= 0f || waypoint + 1 >= route.Count) return false;
            Vector3 corner = route[waypoint];
            // A grid turn belongs to three route nodes. Using the node segment also
            // keeps consecutive turns independent of the previous curve's exit point.
            Vector3 previous = waypoint > 0 ? route[waypoint - 1] : routeStartPosition;
            Vector3 entrySegment = corner - previous;
            float previousDistance = entrySegment.magnitude;
            if (previousDistance <= .0001f) return false;
            Vector3 incoming = entrySegment / previousDistance;
            Vector3 outgoing = route[waypoint + 1] - corner;
            float nextDistance = outgoing.magnitude;
            if (nextDistance <= .0001f) return false;
            outgoing /= nextDistance;
            float dot = Vector3.Dot(incoming, outgoing);
            if (dot >= .99f || dot <= -.99f) return false;

            float radius = Mathf.Min(turnRadius * turnCellWidth,
                previousDistance * .45f, nextDistance * .45f);
            float minimum = turnCellWidth * .025f;
            while (radius >= minimum)
            {
                if (distance > radius + .0001f)
                {
                    approachDistance = distance - radius;
                    return false;
                }
                if (TryQuadraticTurn(transform.position, corner, corner + outgoing * radius,
                        waypoint + 1)) return true;
                radius *= .5f;
            }
            return false;
        }

        private void TryStartReturnTurn()
        {
            ClearTurn();
            if (route == null || canTurnAt == null || turnCellWidth <= 0f) return;
            while (waypoint < route.Count &&
                   (route[waypoint] - transform.position).sqrMagnitude <= .000001f) waypoint++;
            if (waypoint >= route.Count) return;
            Vector3 start = transform.position;
            Vector3 returning = route[waypoint] - start;
            float distance = returning.magnitude;
            Vector3 endDirection = returning / distance;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            if (forward.sqrMagnitude < .5f || Vector3.Dot(forward, endDirection) >= .965f) return;

            float lead = Mathf.Min(turnLookAheadDistance * turnCellWidth, distance * .45f);
            float radius = Mathf.Min(turnRadius * turnCellWidth, distance * .45f);
            Vector3 side = new Vector3(forward.z, 0f, -forward.x);
            while (radius >= turnCellWidth * .025f)
            {
                Vector3 end = start + endDirection * lead;
                if (TryQuarticTurn(start, forward, end, endDirection, side, radius, waypoint) ||
                    TryQuarticTurn(start, forward, end, endDirection, -side, radius, waypoint)) return;
                radius *= .5f;
                lead *= .5f;
            }
        }

        private bool TryQuadraticTurn(Vector3 start, Vector3 corner, Vector3 end, int exitWaypoint)
        {
            ClearTurn();
            int steps = Mathf.Max(3, turnCurveSteps);
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float u = 1f - t;
                Vector3 point = u * u * start + 2f * u * t * corner + t * t * end;
                Vector3 tangent = 2f * u * (corner - start) + 2f * t * (end - corner);
                if (!AddTurnSample(point, tangent)) { ClearTurn(); return false; }
            }
            return FinishTurn(exitWaypoint, false);
        }

        private bool TryQuarticTurn(Vector3 start, Vector3 forward, Vector3 end,
            Vector3 endDirection, Vector3 side, float radius, int exitWaypoint)
        {
            ClearTurn();
            Vector3 p1 = start + forward * radius;
            Vector3 p2 = start + forward * radius + side * (radius * 2f);
            Vector3 p3 = end - endDirection * radius;
            int steps = Mathf.Max(3, turnCurveSteps);
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float u = 1f - t;
                float u2 = u * u, t2 = t * t;
                Vector3 point = u2 * u2 * start + 4f * u2 * u * t * p1 +
                    6f * u2 * t2 * p2 + 4f * u * t2 * t * p3 + t2 * t2 * end;
                Vector3 tangent = 4f * (u2 * u * (p1 - start) + 3f * u2 * t * (p2 - p1) +
                    3f * u * t2 * (p3 - p2) + t2 * t * (end - p3));
                if (!AddTurnSample(point, tangent)) { ClearTurn(); return false; }
            }
            return FinishTurn(exitWaypoint, true);
        }

        private bool AddTurnSample(Vector3 point, Vector3 tangent)
        {
            tangent.y = 0f;
            if (tangent.sqrMagnitude <= .00000001f || !canTurnAt(point)) return false;
            float length = 0f;
            if (turnSamples.Count > 0)
            {
                Vector3 previous = turnSamples[turnSamples.Count - 1].Position;
                if (!canTurnAt((previous + point) * .5f)) return false;
                length = turnSamples[turnSamples.Count - 1].Distance + Vector3.Distance(previous, point);
            }
            turnSamples.Add(new TurnSample { Position = point, Tangent = tangent.normalized, Distance = length });
            return true;
        }

        private bool FinishTurn(int exitWaypoint, bool isReturn)
        {
            turnLength = turnSamples[turnSamples.Count - 1].Distance;
            if (turnLength <= .0001f) { ClearTurn(); return false; }
            turnDistance = 0f;
            turnExitWaypoint = exitWaypoint;
            returnTurn = isReturn;
            return true;
        }

        private bool AdvanceTurn(ref float remaining)
        {
            float step = Mathf.Min(remaining, turnLength - turnDistance);
            turnDistance += step;
            remaining -= step;
            int next = 1;
            while (next < turnSamples.Count - 1 && turnSamples[next].Distance < turnDistance) next++;
            var a = turnSamples[next - 1];
            var b = turnSamples[next];
            float fraction = Mathf.InverseLerp(a.Distance, b.Distance, turnDistance);
            transform.position = Vector3.Lerp(a.Position, b.Position, fraction);
            TrackMovement(Vector3.Slerp(a.Tangent, b.Tangent, fraction), step);
            if (turnDistance < turnLength - .000001f) return false;
            int exitWaypoint = turnExitWaypoint;
            ClearTurn();
            waypoint = exitWaypoint;
            return true;
        }

        private void ClearTurn()
        {
            turnSamples.Clear();
            turnDistance = turnLength = 0f;
            turnExitWaypoint = 0;
            returnTurn = false;
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

        private void FaceDirectionImmediately(Vector3 direction)
        {
            if (direction.x * direction.x + direction.z * direction.z > .0001f)
            {
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        private void TrackMovement(Vector3 direction, float distance)
        {
            if (distance <= 0f || turnCellWidth <= 0f ||
                direction.x * direction.x + direction.z * direction.z <= .0001f) return;
            frameMovement += distance;
            frameHeading = direction;
        }

        private void RotateToMovement()
        {
            if (frameMovement <= 0f || frameTurnDelta <= 0f || turnCellWidth <= 0f) return;
            frameHeading.y = 0f;
            float targetYaw = Mathf.Atan2(frameHeading.x, frameHeading.z) * Mathf.Rad2Deg;
            float currentYaw = transform.eulerAngles.y;
            float blend = 1f - Mathf.Exp(-turnRotateSpeed * frameTurnDelta);
            float smoothYaw = Mathf.LerpAngle(currentYaw, targetYaw, blend);
            float maxAngle = Mathf.Min(turnDegreesPerCell * frameMovement / turnCellWidth,
                turnDegreesPerSecond * frameTurnDelta);
            float yaw = Mathf.MoveTowardsAngle(currentYaw, smoothYaw, maxAngle);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public void Cancel()
        {
            animation?.Cancel();
            proceduralAnimation?.ResetPose();
            if (contactShadow != null) contactShadow.enabled = false;
            State = AntTripState.Inactive;
            Source = null;
            Target = null;
            route = null;
            returnRoute = null;
            routeStartPosition = Vector3.zero;
            canTurnAt = null;
            turnCellWidth = 0f;
            frameTurnDelta = frameMovement = 0f;
            frameHeading = Vector3.zero;
            ClearTurn();
            carriedBrick.SetActive(false);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            animation?.Cancel();
            if (contactShadow != null) contactShadow.enabled = false;
        }

        private void OnDestroy()
        {
            animation?.Dispose();
        }
    }
}
