using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed partial class AntGameplay
    {
        private readonly List<int> blowColors = new List<int>();
        public bool IsSelectingBlow { get; private set; }
        public IReadOnlyList<int> AvailableBlowColors => blowColors;
        public bool CanPickupBooster => CanUseBooster() && slots != null &&
            System.Array.Exists(slots, box => box == null);
        public bool CanBlowBooster => CanUseBooster();

        public bool BeginBlowSelection()
        {
            if (!CanUseBooster()) return false;
            CancelBoosterSelection();
            foreach (var cell in mapView.Model.EnumerateCells())
                if (!cell.IsEmpty && !blowColors.Contains(cell.ColorId)) blowColors.Add(cell.ColorId);
            blowColors.Sort();
            IsSelectingBlow = blowColors.Count > 0;
            if (IsSelectingBlow)
            {
                mapView.AddBlowFocusRenderers(QueueOutlineFeature.BlowFocusRenderers);
                QueueOutlineFeature.BlowFocusGameplay = this;
            }
            return IsSelectingBlow;
        }

        internal void AddBlowFocusAntRenderers(List<Renderer> targets)
        {
            foreach (var ant in active)
            {
                if (ant == null || !ant.isActiveAndEnabled) continue;
                var position = ant.transform.position;
                if (position.x < cardBounds.min.x || position.x > cardBounds.max.x ||
                    position.z < cardBounds.min.z || position.z > cardBounds.max.z) continue;
                ant.AddFocusRenderers(targets);
            }
        }

        public Color GetGameplayColor(int colorId) => mapView != null && mapView.Model != null
            ? mapView.Model.GetColor(colorId) : Color.white;
    }
}
