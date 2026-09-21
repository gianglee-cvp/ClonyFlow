using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasSetting : UICanvas
    {
        [FormerlySerializedAs("retryButton")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        private AntGameplay gameplay;
        private bool wired;

        public override void Setup()
        {
            base.Setup();
            gameplay = LevelManager.Instance.Gameplay;
            EnsureRestartButton();
            if (wired) return;
            AddListener(resumeButton, ButtonResume);
            AddListener(restartButton, ButtonRestart);
            AddListener(homeButton, ButtonHome);
            wired = true;
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
        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        public void ButtonResume()
        {
            if (gameplay != null && gameplay.Paused) gameplay.TogglePause();
            UIManager.Instance.CloseUI<CanvasSetting>();
        }

        public void ButtonRestart() => GameManager.Instance.RestartLevel();
        public void ButtonHome() => GameManager.Instance.ShowMenu();
    }
}