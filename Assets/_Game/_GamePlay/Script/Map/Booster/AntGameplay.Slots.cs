using System;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed partial class AntGameplay
    {
        private Transform[][] originalSlotPoints;
        private Vector3[][] originalSlotPositions;
        private Vector3[] originalSurfacePositions;
        private Renderer extraSlotSurface;
        public bool HasAddedSlot => originalSlotPoints != null;
        public bool CanAddSlot => CanUseBooster() && !HasAddedSlot &&
            boxAnchors.Length >= 2 && slotSurfaces != null && slotSurfaces.Length == boxAnchors.Length &&
            Array.TrueForAll(slotSurfaces, surface => surface != null);

        public bool AddSlot()
        {
            if (!CanAddSlot) return false;
            CaptureSlotLayout();
            var step = boxAnchors[1].position - boxAnchors[0].position;
            boxAnchors = ExpandSlotPoints(boxAnchors, step, "BoxAnchor");
            antSpawnPoints = ExpandSlotPoints(antSpawnPoints, step, "AntSpawnPoint");
            perimeterEntries = ExpandSlotPoints(perimeterEntries, step, "PerimeterEntry");
            ExpandSlotSurface(step);
            Array.Resize(ref slots, boxAnchors.Length);
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].JumpToSlot(SlotLandingPosition(slots[i], i), layoutUnit);
            RefreshEntries();
            RefreshLevelState();
            BoosterUsed?.Invoke();
            return true;
        }

        private void CaptureSlotLayout()
        {
            originalSlotPoints = new[] { boxAnchors, antSpawnPoints, perimeterEntries };
            originalSlotPositions = new Vector3[originalSlotPoints.Length][];
            for (int row = 0; row < originalSlotPoints.Length; row++)
                originalSlotPositions[row] = Array.ConvertAll(originalSlotPoints[row], point => point.position);
            originalSurfacePositions = Array.ConvertAll(slotSurfaces, surface => surface.transform.position);
        }

        private Transform[] ExpandSlotPoints(Transform[] points, Vector3 step, string name)
        {
            var expanded = new Transform[points.Length + 1];
            Array.Copy(points, expanded, points.Length);
            var end = points[points.Length - 1].position + step * .5f;
            foreach (var point in points) point.position -= step * .5f;
            var added = new GameObject("Extra Slot " + name).transform;
            added.SetParent(runtimeRoot, false);
            added.position = end;
            expanded[points.Length] = added;
            return expanded;
        }

        private void ExpandSlotSurface(Vector3 step)
        {
            var seed = slotSurfaces[slotSurfaces.Length - 1];
            extraSlotSurface = Instantiate(seed, runtimeRoot);
            extraSlotSurface.name = "Extra Empty Slot";
            extraSlotSurface.transform.position = seed.transform.position + step * .5f;
            foreach (var surface in slotSurfaces) surface.transform.position -= step * .5f;
        }

        private void RestoreSlotLayout()
        {
            if (!HasAddedSlot) return;
            for (int row = 0; row < originalSlotPoints.Length; row++)
                for (int i = 0; i < originalSlotPoints[row].Length; i++)
                    originalSlotPoints[row][i].position = originalSlotPositions[row][i];
            for (int i = 0; i < slotSurfaces.Length; i++)
                slotSurfaces[i].transform.position = originalSurfacePositions[i];
            boxAnchors = originalSlotPoints[0];
            antSpawnPoints = originalSlotPoints[1];
            perimeterEntries = originalSlotPoints[2];
            if (extraSlotSurface != null) extraSlotSurface.gameObject.SetActive(false);
            extraSlotSurface = null;
            originalSlotPoints = null;
            originalSlotPositions = null;
            originalSurfacePositions = null;
        }
    }
}
