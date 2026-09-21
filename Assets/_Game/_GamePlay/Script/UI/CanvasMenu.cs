using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasMenu : UICanvas
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Button playButton;
        private bool wired;

        public override void Setup()
        {
            base.Setup();
            WireButtons();
            if (levelText != null)
                levelText.text = "LEVEL\n" + LevelManager.Instance.CurrentLevel;
        }

        private void WireButtons()
        {
            if (wired || playButton == null) return;
            if (playButton.onClick.GetPersistentEventCount() == 0)
                playButton.onClick.AddListener(ButtonPlay);
            wired = true;
        }

        public void ButtonPlay() => GameManager.Instance.PlayCurrentLevel();
    }
}