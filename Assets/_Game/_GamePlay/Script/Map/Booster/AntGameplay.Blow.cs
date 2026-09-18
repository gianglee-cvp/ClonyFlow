namespace ColonyFlow.Gameplay
{
    public sealed partial class AntGameplay
    {
        public bool BlowColor(int colorId)
        {
            if (!CanUseBooster() || !HasRemainingColor(colorId)) return false;
            CancelUncollectedColor(colorId);
            CollectColor(colorId);
            ClearColorBudgets(colorId);
            ResolvePendingBoxes();
            RefreshQueues(true);
            navigation.Rebuild();
            CancelBoosterSelection();
            RefreshLevelState();
            return true;
        }

        private bool HasRemainingColor(int colorId)
        {
            if (colorId <= 0) return false;
            foreach (var cell in mapView.Model.EnumerateCells())
                if (cell.ColorId == colorId) return true;
            return false;
        }

        private void CancelUncollectedColor(int colorId)
        {
            foreach (var ant in active)
            {
                if (ant.ColorId != colorId ||
                    (ant.State != AntTripState.Outbound && ant.State != AntTripState.WaitingPickup)) continue;
                ant.Source?.DiscardOutgoing();
                if (reserves.TryGetValue(ant.Target, out var owner) && owner == ant.TaskId) reserves.Remove(ant.Target);
                ant.DetachSource();
                ant.Cancel();
            }
            RemoveFinishedAnts();
            pickups.Clear();
        }

        private void CollectColor(int colorId)
        {
            foreach (var cell in mapView.Model.EnumerateCells())
                if (cell.ColorId == colorId) mapView.Collect(cell);
        }

        private void ClearColorBudgets(int colorId)
        {
            ClearQueuedColor(colorId);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || slots[i].ColorId != colorId) continue;
                slots[i].ClearBudget();
                if (slots[i].OutgoingCount == 0) ReleaseBox(i);
            }
            foreach (var box in pendingBoxes)
                if (box.ColorId == colorId) box.ClearBudget();
        }

        private void ClearQueuedColor(int colorId)
        {
            foreach (var queue in queues)
            {
                for (int row = queue.Count - 1; row >= 0; row--)
                {
                    var box = queue[row];
                    if (box.ColorId != colorId) continue;
                    box.ClearBudget();
                    queue.RemoveAt(row);
                    colliderBoxes.Remove(box.HitCollider);
                    pool.Recycle(box);
                }
            }
        }
    }
}
