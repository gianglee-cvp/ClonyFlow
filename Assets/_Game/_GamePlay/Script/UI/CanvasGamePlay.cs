using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasGamePlay : UICanvas
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text selectionText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button addSlotButton;
        [SerializeField] private CanvasGroup addSlotButtonGroup;
        [SerializeField] private Button pickupButton;
        [SerializeField] private CanvasGroup pickupButtonGroup;
        [SerializeField] private Button blowButton;
        [SerializeField] private CanvasGroup blowButtonGroup;
        [SerializeField] private Button cancelButton;
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private GameplayPointerSurface pointerSurface;
        [SerializeField] private List<Button> colorButtons = new List<Button>();
        [SerializeField] private List<Text> colorLabels = new List<Text>();
        [SerializeField] private List<Image> colorImages = new List<Image>();
        private readonly List<int> displayedColors = new List<int>();
        private AntGameplay gameplay;
        private bool wired;

        public override void Setup()
        {
            base.Setup();
            gameplay = LevelManager.Instance.Gameplay;
            if (addSlotButtonGroup == null && addSlotButton != null)
                addSlotButtonGroup = addSlotButton.GetComponent<CanvasGroup>();
            if (pickupButtonGroup == null && pickupButton != null)
                pickupButtonGroup = pickupButton.GetComponent<CanvasGroup>();
            if (blowButtonGroup == null && blowButton != null)
                blowButtonGroup = blowButton.GetComponent<CanvasGroup>();
            if (levelText == null)
                levelText = transform.Find("Level Text")?.GetComponent<Text>();
            if (levelText != null) levelText.gameObject.SetActive(true);
            var obsoleteRestart = transform.Find("Restart Button");
            if (obsoleteRestart != null) obsoleteRestart.gameObject.SetActive(false);
            WireButtons();
            if (pointerSurface != null) pointerSurface.Configure(this);
            RefreshState();
        }

        private void Update() => RefreshState();

        private void WireButtons()
        {
            if (wired) return;
            AddListener(pauseButton, ButtonPause);
            AddListener(speedButton, ButtonSpeed);
            AddListener(addSlotButton, ButtonAddSlot);
            AddListener(pickupButton, ButtonPickup);
            AddListener(blowButton, ButtonBlow);
            AddListener(cancelButton, ButtonCancel);
            for (int i = 0; i < colorButtons.Count; i++)
            {
                int index = i;
                AddListener(colorButtons[i], () => ButtonBlowColor(index));
            }
            wired = true;
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private void RefreshState()
        {
            if (gameplay == null) return;
            if (levelText != null) levelText.text = "Level " + LevelManager.Instance.CurrentLevel;
            if (statusText != null) statusText.text = gameplay.StatusText;
            if (speedText != null) speedText.text = "x" + gameplay.SpeedMultiplier;
            SetButtonState(addSlotButton, addSlotButtonGroup, gameplay.CanAddSlot);
            SetButtonState(pickupButton, pickupButtonGroup, gameplay.CanPickupBooster);
            SetButtonState(blowButton, blowButtonGroup, gameplay.CanBlowBooster);
            RefreshSelection();
        }

        private static void SetButtonState(Button button, CanvasGroup group, bool interactable)
        {
            if (button != null) button.interactable = interactable;
            if (group != null) group.alpha = interactable ? 1f : .4f;
        }
        private void RefreshSelection()
        {
            bool selecting = gameplay.IsSelectingPickup || gameplay.IsSelectingBlow;
            if (selectionPanel != null) selectionPanel.SetActive(selecting);
            if (selectionText != null)
                selectionText.text = gameplay.IsSelectingPickup ? "Choose a queue box" : "Choose a color";
            displayedColors.Clear();
            foreach (int color in gameplay.AvailableBlowColors) displayedColors.Add(color);
            for (int i = 0; i < colorButtons.Count; i++)
            {
                bool active = gameplay.IsSelectingBlow && i < displayedColors.Count;
                colorButtons[i].gameObject.SetActive(active);
                if (!active) continue;
                int colorId = displayedColors[i];
                if (i < colorLabels.Count && colorLabels[i] != null) colorLabels[i].text = colorId.ToString();
                if (i < colorImages.Count && colorImages[i] != null) colorImages[i].color = gameplay.GetGameplayColor(colorId);
            }
        }

        public void HandleWorldPointer(Vector2 screenPosition) => gameplay?.HandlePointer(screenPosition);
        public void ButtonPause()
        {
            if (gameplay == null) return;
            if (!gameplay.Paused) gameplay.TogglePause();
            UIManager.Instance.OpenUI<CanvasSetting>();
        }
        public void ButtonSpeed() => gameplay?.ToggleSpeed();
        public void ButtonAddSlot() => gameplay?.AddSlot();
        public void ButtonPickup() => gameplay?.BeginPickupSelection();
        public void ButtonBlow() => gameplay?.BeginBlowSelection();
        public void ButtonCancel() => gameplay?.CancelBoosterSelection();

        public void ButtonBlowColor(int index)
        {
            if (gameplay == null || index < 0 || index >= displayedColors.Count) return;
            gameplay.BlowColor(displayedColors[index]);
        }
    }
}
