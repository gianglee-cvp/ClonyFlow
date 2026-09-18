using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed partial class AntGameplay
    {
        private const int ColorsPerRow = 6;
        private readonly List<int> blowColors = new List<int>();
        private GUIStyle boosterButtonStyle;
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
            DrawBoosterButton(BoosterActionRect(0), "Add Slot", CanAddSlot, () => AddSlot());
            DrawBoosterButton(BoosterActionRect(1), "Pickup", CanUseBooster() && Array.Exists(slots, box => box == null), () => BeginPickupSelection());
            DrawBoosterButton(BoosterActionRect(2), "Blow", CanUseBooster(), () => BeginBlowSelection());
            DrawBoosterSelection();
        }

        private Rect BoosterActionRect(int index)
        {
            float width = (UIWidth - 48) / 3f;
            return new Rect(12 + index * (width + 12), UIHeight - 66, width, 28);
        }

        private Rect BoosterCancelRect => new Rect(UIWidth - 90, UIHeight - 102, 80, 28);

        private Rect BlowColorRect(int index)
        {
            float width = (UIWidth - 24) / ColorsPerRow;
            return new Rect(12 + index % ColorsPerRow * width, UIHeight - 136 - index / ColorsPerRow * 34, width - 6, 28);
        }

        private bool HandleBoosterPointer(Vector2 screen)
        {
            var point = new Vector2(screen.x, Screen.height - screen.y) / GameplayUIScale;
            if (BoosterActionRect(0).Contains(point)) { AddSlot(); return true; }
            if (BoosterActionRect(1).Contains(point)) { BeginPickupSelection(); return true; }
            if (BoosterActionRect(2).Contains(point)) { BeginBlowSelection(); return true; }
            if (!IsSelectingPickup && !IsSelectingBlow) return false;
            if (BoosterCancelRect.Contains(point)) { CancelBoosterSelection(); return true; }
            if (!IsSelectingBlow) return false;
            for (int i = 0; i < blowColors.Count; i++)
            {
                if (!BlowColorRect(i).Contains(point)) continue;
                BlowColor(blowColors[i]);
                return true;
            }
            return false;
        }

        private void DrawBoosterSelection()
        {
            if (!IsSelectingPickup && !IsSelectingBlow) return;
            GUI.Label(new Rect(12, UIHeight - 100, UIWidth - 110, 24), IsSelectingPickup ? "Choose a queue box" : "Choose a color");
            DrawBoosterButton(BoosterCancelRect, "Cancel", true, CancelBoosterSelection);
            if (IsSelectingBlow) DrawBlowColors();
        }

        private void DrawBlowColors()
        {
            var background = GUI.backgroundColor;
            // BlowColor clears the selection list; stop drawing after an accepted choice.
            for (int i = 0; i < blowColors.Count; i++)
            {
                int colorId = blowColors[i];
                GUI.backgroundColor = mapView.Model.GetColor(colorId);
                var rect = BlowColorRect(i);
                bool chosen = false;
                DrawBoosterButton(rect, colorId.ToString(), CanUseBooster() && HasRemainingColor(colorId), () => chosen = BlowColor(colorId));
                if (chosen) break;
            }
            GUI.backgroundColor = background;
        }

        private void DrawBoosterButton(Rect rect, string label, bool enabled, Action action)
        {
            if (boosterButtonStyle == null)
                boosterButtonStyle = new GUIStyle(GUI.skin.button) { padding = new RectOffset(6, 6, 0, 0) };
            boosterButtonStyle.fontSize = Mathf.RoundToInt(12 / GameplayUIScale);
            bool previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            if (GUI.Button(rect, label, boosterButtonStyle)) action();
            GUI.enabled = previous;
        }
    }
}
