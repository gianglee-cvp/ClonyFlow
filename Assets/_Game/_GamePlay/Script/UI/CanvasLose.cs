using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasLose : UICanvas
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;
        private bool wired;

        public override void Setup()
        {
            base.Setup();
            if (levelText != null) levelText.text = "Level " + LevelManager.Instance.CurrentLevel + " Failed";
            if (wired) return;
            if (retryButton != null) retryButton.onClick.AddListener(ButtonRetry);
            if (homeButton != null) homeButton.onClick.AddListener(ButtonHome);
            wired = true;
        }

        public void ButtonRetry() => GameManager.Instance.RestartLevel();
        public void ButtonHome() => GameManager.Instance.ShowMenu();
    }
}
