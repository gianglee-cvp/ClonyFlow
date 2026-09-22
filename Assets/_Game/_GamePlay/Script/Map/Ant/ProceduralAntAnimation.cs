using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public enum ProceduralAntPose { Idle, Walk, Pickup, Carry, Jump }

    public sealed class ProceduralAntAnimation : MonoBehaviour
    {
        [SerializeField] private bool previewMode;
        [SerializeField] private ProceduralAntPose previewPose = ProceduralAntPose.Walk;
        private Transform visualRoot;
        private Transform head;
        private Transform[] legs;
        private Transform[] antennae;
        private Transform[] mandibles;
        private Quaternion[] legRotations;
        private Quaternion[] antennaRotations;
        private Quaternion[] mandibleRotations;
        private Quaternion headRotation;
        private Vector3 visualPosition;
        private ProceduralAntPose pose;
        private float phase;
        private bool cached;

        private void Awake()
        {
            Cache();
            pose = previewMode ? previewPose : ProceduralAntPose.Idle;
        }

        private void Update()
        {
            if (previewMode) Advance(Time.deltaTime);
        }

        public void ConfigurePreview(bool enabled)
        {
            previewMode = enabled;
            pose = enabled ? previewPose : ProceduralAntPose.Idle;
        }

        public void SetIdle() => pose = ProceduralAntPose.Idle;
        public void SetWalking(bool carrying) => pose = carrying ? ProceduralAntPose.Carry : ProceduralAntPose.Walk;
        public void SetPickingUp() { phase = 0; pose = ProceduralAntPose.Pickup; }
        public void SetJumping() => pose = ProceduralAntPose.Jump;

        public void Advance(float delta)
        {
            Cache();
            if (delta <= 0) return;
            phase += delta * (pose == ProceduralAntPose.Idle ? 2.2f : pose == ProceduralAntPose.Pickup ? 15f : 9f);
            ApplyPose();
        }

        public void ResetPose()
        {
            Cache();
            phase = 0;
            pose = ProceduralAntPose.Idle;
            visualRoot.localPosition = visualPosition;
            head.localRotation = headRotation;
            for (int i = 0; i < legs.Length; i++) legs[i].localRotation = legRotations[i];
            for (int i = 0; i < antennae.Length; i++) antennae[i].localRotation = antennaRotations[i];
            for (int i = 0; i < mandibles.Length; i++) if (mandibles[i] != null) mandibles[i].localRotation = mandibleRotations[i];
        }

        private void ApplyPose()
        {
            float wave = Mathf.Sin(phase);
            float bob = pose == ProceduralAntPose.Idle ? Mathf.Sin(phase) * .012f : pose == ProceduralAntPose.Pickup ? -Mathf.Abs(wave) * .018f : Mathf.Abs(wave) * .025f;
            float lunge = pose == ProceduralAntPose.Pickup ? .055f + Mathf.Abs(Mathf.Sin(phase * .75f)) * .075f : 0;
            visualRoot.localPosition = visualPosition + Vector3.up * bob + Vector3.forward * lunge;
            float headPitch = pose == ProceduralAntPose.Pickup ? 24f + Mathf.Abs(wave) * 13f :
                pose == ProceduralAntPose.Idle ? wave * 2f : -wave * 3f;
            head.localRotation = headRotation * Quaternion.Euler(headPitch, 0, 0);

            for (int i = 0; i < antennae.Length; i++)
            {
                float side = i == 0 ? -1f : 1f;
                antennae[i].localRotation = antennaRotations[i] * Quaternion.Euler(0, 0, side * Mathf.Sin(phase * .65f) * 6f);
            }

            float bite = pose == ProceduralAntPose.Pickup ? Mathf.Abs(Mathf.Sin(phase * 1.5f)) * 24f : 0;
            for (int i = 0; i < mandibles.Length; i++)
            {
                float side = i == 0 ? -1f : 1f;
                if (mandibles[i] != null) mandibles[i].localRotation = mandibleRotations[i] * Quaternion.Euler(0, 0, side * bite);
            }

            for (int i = 0; i < legs.Length; i++)
            {
                float alternating = ((i + i / 2) & 1) == 0 ? wave : -wave;
                Quaternion offset;
                if (pose == ProceduralAntPose.Jump)
                    offset = Quaternion.Euler(0, (i % 2 == 0 ? -1 : 1) * 28f, 22f);
                else if (pose == ProceduralAntPose.Pickup)
                    offset = i < 2
                        ? Quaternion.Euler(0, (i == 0 ? 1f : -1f) * (18f + wave * 10f), 12f)
                        : Quaternion.Euler(0, alternating * 2f, 0);
                else if (pose == ProceduralAntPose.Idle)
                    offset = Quaternion.Euler(0, alternating * 2f, 0);
                else
                    offset = Quaternion.Euler(0, alternating * (pose == ProceduralAntPose.Carry ? 18f : 28f), 0);
                legs[i].localRotation = legRotations[i] * offset;
            }
        }

        private void Cache()
        {
            if (cached) return;
            visualRoot = transform.Find("Visual");
            head = visualRoot.Find("Head");
            legs = new[]
            {
                visualRoot.Find("Leg Root Front L"), visualRoot.Find("Leg Root Front R"),
                visualRoot.Find("Leg Root Middle L"), visualRoot.Find("Leg Root Middle R"),
                visualRoot.Find("Leg Root Back L"), visualRoot.Find("Leg Root Back R")
            };
            antennae = new[] { visualRoot.Find("Antenna Root L"), visualRoot.Find("Antenna Root R") };
            mandibles = new[] { visualRoot.Find("Mandible L"), visualRoot.Find("Mandible R") };
            visualPosition = visualRoot.localPosition;
            headRotation = head.localRotation;
            legRotations = new Quaternion[legs.Length];
            antennaRotations = new Quaternion[antennae.Length];
            mandibleRotations = new Quaternion[mandibles.Length];
            for (int i = 0; i < legs.Length; i++) legRotations[i] = legs[i].localRotation;
            for (int i = 0; i < antennae.Length; i++) antennaRotations[i] = antennae[i].localRotation;
            for (int i = 0; i < mandibles.Length; i++) if (mandibles[i] != null) mandibleRotations[i] = mandibles[i].localRotation;
            cached = true;
        }
    }
}