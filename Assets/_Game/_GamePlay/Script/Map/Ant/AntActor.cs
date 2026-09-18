using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public enum AntTripState { Inactive, Outbound, WaitingPickup, Returning, Jumping }
    public sealed class AntActor : MonoBehaviour
    {
        [SerializeField] private GameObject carriedBrick;
        [SerializeField] private Renderer abdomen;
        private List<Vector3> route;
        private List<Vector3> returnRoute;
        private int waypoint;
        private Vector3 jumpStart, jumpTarget;
        private float jumpTime;
        public long TaskId { get; private set; }
        public Cell Target { get; private set; }
        public BoxActor Source { get; private set; }
        public AntTripState State { get; private set; }
        public void Configure(GameObject brick, Renderer body) { carriedBrick = brick; abdomen = body; }
        public void Begin(long id, BoxActor source, Cell target, Vector3 spawn, List<Vector3> outbound,
            List<Vector3> returning, Vector3 jump, Color color)
        {
            TaskId = id; Source = source; Target = target; route = outbound; returnRoute = returning;
            waypoint = 0; jumpTime = 0; jumpTarget = jump;
            transform.position = spawn;
            State = AntTripState.Outbound;
            carriedBrick.SetActive(false);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            abdomen.SetPropertyBlock(block);
            carriedBrick.GetComponent<Renderer>().SetPropertyBlock(block);
            gameObject.SetActive(true);
        }
        public void RemapCardRoute(System.Func<Vector3, Vector3> remap)
        {
            if (State == AntTripState.Inactive || State == AntTripState.Jumping) return;
            transform.position = remap(transform.position);
            if (route != null)
                for (int i = waypoint; i < route.Count; i++) route[i] = remap(route[i]);
            if (returnRoute != null && returnRoute != route)
                for (int i = 0; i < returnRoute.Count; i++) returnRoute[i] = remap(returnRoute[i]);
        }
        public void ConfirmPickup()
        {
            if (State != AntTripState.WaitingPickup) return;
            carriedBrick.SetActive(true);
            route = returnRoute; waypoint = 0; State = AntTripState.Returning;
        }
        public void Advance(float delta, float speed)
        {
            if (State == AntTripState.Inactive || State == AntTripState.WaitingPickup) return;
            if (State == AntTripState.Jumping)
            {
                jumpTime += delta;
                float t = Mathf.Clamp01(jumpTime / .55f);
                transform.position = Vector3.Lerp(jumpStart, jumpTarget, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.4f);
                if (t >= 1) { State = AntTripState.Inactive; gameObject.SetActive(false); }
                return;
            }
            float remaining = delta * speed;
            while (waypoint < route.Count)
            {
                var next = route[waypoint];
                var direction = next - transform.position;
                float distance = direction.magnitude;
                if (direction.x * direction.x + direction.z * direction.z > .0001f)
                    transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                if (distance > remaining)
                {
                    transform.position += direction.normalized * remaining;
                    return;
                }
                transform.position = next;
                remaining -= distance; waypoint++;
            }
            if (State == AntTripState.Outbound) State = AntTripState.WaitingPickup;
            else { jumpStart = transform.position; jumpTime = 0; State = AntTripState.Jumping; }
        }
        public void Cancel()
        {
            State = AntTripState.Inactive; Source = null; Target = null;
            route = null; returnRoute = null; carriedBrick.SetActive(false); gameObject.SetActive(false);
        }
    }
}
