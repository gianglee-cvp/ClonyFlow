using System;

namespace ColonyFlow.Gameplay
{
    public sealed partial class AntGameplay
    {
        private const int VisibleQueueRows = 3;
        public bool IsSelectingPickup { get; private set; }

        public bool BeginPickupSelection()
        {
            if (!CanUseBooster() || Array.FindIndex(slots, box => box == null) < 0) return false;
            CancelBoosterSelection();
            IsSelectingPickup = true;
            RefreshPickupFocus();
            return IsSelectingPickup;
        }

        public void CancelBoosterSelection()
        {
            IsSelectingPickup = false;
            IsSelectingBlow = false;
            blowColors.Clear();
            BoxActor.PickupFocusBoxes.Clear();
            QueueOutlineFeature.BlowFocusRenderers.Clear();
            QueueOutlineFeature.BlowFocusGameplay = null;
        }

        private bool CanUseBooster() => HasCurrentSession && !Paused && !IsBoardCleared;

        public bool PickupBox(BoxActor box)
        {
            if (!IsSelectingPickup || box == null || !BoxActor.PickupFocusBoxes.Contains(box) ||
                !CanPickupBox(box)) return false;
            int row = queues[box.QueueIndex].IndexOf(box);
            int slot = Array.FindIndex(slots, item => item == null);
            MoveBoxToSlot(box.QueueIndex, row, slot);
            RefreshQueues(true);
            CancelBoosterSelection();
            RefreshLevelState();
            BoosterUsed?.Invoke();
            return true;
        }

        private bool CanPickupBox(BoxActor box)
        {
            if (!CanUseBooster() || box == null || !box.CanPickup || box.SlotIndex >= 0 ||
                box.QueueIndex < 0 || box.QueueIndex >= queues.Count ||
                Array.FindIndex(slots, item => item == null) < 0) return false;
            int row = queues[box.QueueIndex].IndexOf(box);
            return row >= 0 && row < VisibleQueueRows;
        }

        private void RefreshPickupFocus()
        {
            if (!IsSelectingPickup) return;
            BoxActor.PickupFocusBoxes.Clear();
            foreach (var queue in queues)
            {
                int visible = Math.Min(VisibleQueueRows, queue.Count);
                for (int row = 0; row < visible; row++)
                    if (CanPickupBox(queue[row])) BoxActor.PickupFocusBoxes.Add(queue[row]);
            }
            if (BoxActor.PickupFocusBoxes.Count == 0) CancelBoosterSelection();
        }
    }
}
