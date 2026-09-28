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
        [SerializeField] private CanvasGroup pickupDimGroup;
        [SerializeField] private GameplayPointerSurface pointerSurface;
        [SerializeField] private List<Button> colorButtons = new List<Button>();
        [SerializeField] private List<Text> colorLabels = new List<Text>();
        [SerializeField] private List<Image> colorImages = new List<Image>();
        private readonly List<int> displayedColors = new List<int>();
        private AntGameplay gameplay;
        private bool wired;
        private int displayedLevel;

        public override void Setup()
        {
            base.Setup();
            gameplay = LevelManager.Instance.Gameplay;
            displayedLevel = LevelManager.Instance.CurrentLevel;
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
            EnsurePickupDimRoot();
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
            if (levelText != null) levelText.text = "Level " + displayedLevel;
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
            if (pickupDimGroup != null) pickupDimGroup.alpha = gameplay.IsSelectingPickup ? .32f : 1f;
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

        private void EnsurePickupDimRoot()
        {
            if (pickupDimGroup == null)
                pickupDimGroup = transform.Find("Pickup Dim Root")?.GetComponent<CanvasGroup>();
            if (pickupDimGroup == null)
            {
                var owner = new GameObject("Pickup Dim Root", typeof(RectTransform), typeof(CanvasGroup));
                var rect = (RectTransform)owner.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                pickupDimGroup = owner.GetComponent<CanvasGroup>();
            }

            Transform dimRoot = pickupDimGroup.transform;
            var ordinary = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == dimRoot || child == selectionPanel?.transform ||
                    child == pointerSurface?.transform || child.name == "Box Counts" ||
                    child.name == "Box Count Template") continue;
                ordinary.Add(child);
            }
            foreach (Transform child in ordinary) child.SetParent(dimRoot, false);
            dimRoot.SetAsFirstSibling();
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
