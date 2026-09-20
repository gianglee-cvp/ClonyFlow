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
        [SerializeField] private Button restartButton;
        [SerializeField] private Button addSlotButton;
        [SerializeField] private Button pickupButton;
        [SerializeField] private Button blowButton;
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
            ConfigureScreenSpaceCamera();
            gameplay = LevelManager.Instance.Gameplay;
            WireButtons();
            if (pointerSurface != null) pointerSurface.Configure(this);
            RefreshState();
        }

        private void ConfigureScreenSpaceCamera()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            if (canvas.worldCamera != null)
                canvas.planeDistance = Mathf.Clamp(100f, canvas.worldCamera.nearClipPlane + .01f,
                    canvas.worldCamera.farClipPlane - .01f);
        }

        private void Update() => RefreshState();

        private void WireButtons()
        {
            if (wired) return;
            AddListener(pauseButton, ButtonPause);
            AddListener(speedButton, ButtonSpeed);
            AddListener(restartButton, ButtonRestart);
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
            if (addSlotButton != null) addSlotButton.interactable = gameplay.CanAddSlot;
            if (pickupButton != null) pickupButton.interactable = gameplay.CanPickupBooster;
            if (blowButton != null) blowButton.interactable = gameplay.CanBlowBooster;
            RefreshSelection();
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
        public void ButtonPause() => gameplay?.TogglePause();
        public void ButtonSpeed() => gameplay?.ToggleSpeed();
        public void ButtonRestart() => GameManager.Instance.RestartLevel();
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
