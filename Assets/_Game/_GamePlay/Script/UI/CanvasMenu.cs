using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasMenu : UICanvas
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Button playButton;
        public override void Setup()
        {
            base.Setup();
            if (levelText != null) levelText.text = "Level " + LevelManager.Instance.CurrentLevel;
        }

        public void ButtonPlay()
        {
            Debug.Log("play");
            GameManager.Instance.PlayCurrentLevel();
        }
    }
}
