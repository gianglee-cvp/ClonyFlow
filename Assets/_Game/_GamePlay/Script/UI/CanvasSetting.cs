using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasSetting : UICanvas
    {
        [FormerlySerializedAs("retryButton")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button sfxButton;
        [SerializeField] private Text musicLabel;
        [SerializeField] private Text sfxLabel;
        private AntGameplay gameplay;
        private bool wired;

        public override void Setup()
        {
            gameplay = LevelManager.Instance.Gameplay;
            EnsureRestartButton();
            EnsureAudioControls();
            base.Setup();
            if (!wired)
            {
                AddListener(resumeButton, ButtonResume);
                AddListener(restartButton, ButtonRestart);
                AddListener(homeButton, ButtonHome);
                AddListener(musicButton, ButtonToggleMusic);
                AddListener(sfxButton, ButtonToggleSfx);
                wired = true;
            }
            RefreshAudioLabels();
        }

        public override void Open()
        {
            base.Open();
            AudioManager.Instance.SetMusicDucked(true);
            RefreshAudioLabels();
        }

        public override void Close(float delay = 0)
        {
            AudioManager.Instance.SetMusicDucked(false);
            base.Close(delay);
        }

        public override void CloseDirectly()
        {
            AudioManager.Instance.SetMusicDucked(false);
            base.CloseDirectly();
        }

        private void EnsureRestartButton()
        {
            if (restartButton != null) return;
            var restartTransform = transform.Find("Lose Card/Level Text");
            if (restartTransform == null) restartTransform = transform.Find("Setting Card/Restart Button");
            if (restartTransform == null) return;
            var label = restartTransform.GetComponent<Text>();
            restartButton = restartTransform.GetComponent<Button>();
            if (restartButton == null) restartButton = restartTransform.gameObject.AddComponent<Button>();
            restartButton.targetGraphic = label;
            if (label != null) label.color = new Color(.72f, .38f, .30f);
        }

        private void EnsureAudioControls()
        {
            var card = transform.Find("Setting Card") as RectTransform;
            if (card == null) card = transform.Find("Lose Card") as RectTransform;
            if (card == null) return;
            ResolveAudioButton(card, "Music Button", ref musicButton, ref musicLabel,
                new Vector2(.12f, .65f), new Vector2(.88f, .76f), new Color(.25f, .55f, .78f));
            ResolveAudioButton(card, "SFX Button", ref sfxButton, ref sfxLabel,
                new Vector2(.12f, .51f), new Vector2(.88f, .62f), new Color(.38f, .65f, .42f));
            SetAnchors(resumeButton, new Vector2(.12f, .36f), new Vector2(.88f, .47f));
            SetAnchors(restartButton, new Vector2(.12f, .21f), new Vector2(.88f, .32f));
            SetAnchors(homeButton, new Vector2(.12f, .06f), new Vector2(.88f, .17f));
        }

        private static void ResolveAudioButton(RectTransform card, string buttonName, ref Button button,
            ref Text label, Vector2 min, Vector2 max, Color color)
        {
            if (button == null) button = card.Find(buttonName)?.GetComponent<Button>();
            if (button == null) button = CreateButton(card, buttonName, min, max, color, out label);
            else
            {
                SetAnchors(button, min, max);
                if (label == null) label = button.transform.Find("Label")?.GetComponent<Text>();
            }
        }

        private static Button CreateButton(Transform parent, string buttonName, Vector2 min, Vector2 max,
            Color color, out Text label)
        {
            var owner = new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = owner.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = owner.GetComponent<Image>();
            image.color = color;
            var button = owner.GetComponent<Button>();
            button.targetGraphic = image;

            var labelOwner = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var labelRect = labelOwner.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            label = labelOwner.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return button;
        }

        private static void SetAnchors(Button button, Vector2 min, Vector2 max)
        {
            if (button == null) return;
            var rect = button.transform as RectTransform;
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private void RefreshAudioLabels()
        {
            if (musicLabel != null) musicLabel.text = "MUSIC: " + (AudioManager.Instance.MusicEnabled ? "ON" : "OFF");
            if (sfxLabel != null) sfxLabel.text = "SFX: " + (AudioManager.Instance.SfxEnabled ? "ON" : "OFF");
        }

        public void ButtonToggleMusic()
        {
            var audio = AudioManager.Instance;
            audio.SetMusicEnabled(!audio.MusicEnabled);
            RefreshAudioLabels();
        }

        public void ButtonToggleSfx()
        {
            var audio = AudioManager.Instance;
            audio.SetSfxEnabled(!audio.SfxEnabled);
            RefreshAudioLabels();
        }

        public void ButtonResume()
        {
            if (gameplay != null && gameplay.Paused) gameplay.TogglePause();
            UIManager.Instance.CloseUI<CanvasSetting>();
        }

        public void ButtonRestart()
        {
            AudioManager.Instance.SetMusicDucked(false);
            GameManager.Instance.RestartLevel();
        }

        public void ButtonHome()
        {
            AudioManager.Instance.SetMusicDucked(false);
            GameManager.Instance.ShowMenu();
        }
    }
}