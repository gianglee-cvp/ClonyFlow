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
            return true;
        }

        public void CancelBoosterSelection()
        {
            IsSelectingPickup = false;
            IsSelectingBlow = false;
            blowColors.Clear();
        }

        private bool CanUseBooster() => HasCurrentSession && !Paused && !IsBoardCleared;

        public bool PickupBox(BoxActor box)
        {
            if (!CanUseBooster() || box == null || !box.CanPickup || box.SlotIndex >= 0 ||
                box.QueueIndex < 0 || box.QueueIndex >= queues.Count) return false;
            int row = queues[box.QueueIndex].IndexOf(box);
            if (row < 0 || row >= VisibleQueueRows) return false;
            int slot = Array.FindIndex(slots, item => item == null);
            if (slot < 0) return false;
            MoveBoxToSlot(box.QueueIndex, row, slot);
            RefreshQueues(true);
            CancelBoosterSelection();
            RefreshLevelState();
            return true;
        }
    }
}
