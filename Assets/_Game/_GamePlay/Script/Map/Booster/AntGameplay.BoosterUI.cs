using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed partial class AntGameplay
    {
        private const int ColorsPerRow = 6;
        private readonly List<int> blowColors = new List<int>();
        public bool IsSelectingBlow { get; private set; }
        public IReadOnlyList<int> AvailableBlowColors => blowColors;
        private float GameplayUIScale => Mathf.Min(1, Screen.height / 960f);
        private float UIWidth => Screen.width / GameplayUIScale;
        private float UIHeight => Screen.height / GameplayUIScale;
        private int ColorRows => (blowColors.Count + ColorsPerRow - 1) / ColorsPerRow;
        private float BoosterPanelHeight => GameplayUIScale * (IsSelectingBlow ? 108 + ColorRows * 34 : IsSelectingPickup ? 108 : 76);

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

        private void DrawBoosters()
        {
            float width = (UIWidth - 48) / 3f;
            float y = UIHeight - 66;
            DrawBoosterButton(new Rect(12, y, width, 28), "Add Slot", CanUseBooster() && !HasAddedSlot, () => AddSlot());
            DrawBoosterButton(new Rect(24 + width, y, width, 28), "Pickup", CanUseBooster() && Array.Exists(slots, box => box == null), () => BeginPickupSelection());
            DrawBoosterButton(new Rect(36 + width * 2, y, width, 28), "Blow", CanUseBooster(), () => BeginBlowSelection());
            DrawBoosterSelection();
        }

        private void DrawBoosterSelection()
        {
            if (!IsSelectingPickup && !IsSelectingBlow) return;
            GUI.Label(new Rect(12, UIHeight - 100, UIWidth - 110, 24), IsSelectingPickup ? "Choose a queue box" : "Choose a color");
            if (GUI.Button(new Rect(UIWidth - 90, UIHeight - 102, 80, 28), "Cancel")) CancelBoosterSelection();
            if (IsSelectingBlow) DrawBlowColors();
        }

        private void DrawBlowColors()
        {
            float width = (UIWidth - 24) / ColorsPerRow;
            var background = GUI.backgroundColor;
            // BlowColor clears the selection list; stop drawing after an accepted choice.
            for (int i = 0; i < blowColors.Count; i++)
            {
                int colorId = blowColors[i];
                GUI.backgroundColor = mapView.Model.GetColor(colorId);
                var rect = new Rect(12 + i % ColorsPerRow * width, UIHeight - 136 - i / ColorsPerRow * 34, width - 6, 28);
                bool chosen = false;
                DrawBoosterButton(rect, colorId.ToString(), CanUseBooster() && HasRemainingColor(colorId), () => chosen = BlowColor(colorId));
                if (chosen) break;
            }
            GUI.backgroundColor = background;
        }

        private static void DrawBoosterButton(Rect rect, string label, bool enabled, Action action)
        {
            bool previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            if (GUI.Button(rect, label)) action();
            GUI.enabled = previous;
        }
    }
}
