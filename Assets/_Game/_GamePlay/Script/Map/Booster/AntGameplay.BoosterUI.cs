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
            return IsSelectingBlow;
        }

        public Color GetGameplayColor(int colorId) => mapView != null && mapView.Model != null
            ? mapView.Model.GetColor(colorId) : Color.white;
    }
}
