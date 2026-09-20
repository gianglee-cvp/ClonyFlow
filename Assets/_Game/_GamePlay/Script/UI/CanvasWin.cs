using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasWin : UICanvas
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button homeButton;
        private bool hasNextLevel;
        private bool wired;

        public void Init(int completedLevel, bool hasNext)
        {
            hasNextLevel = hasNext;
            if (levelText != null) levelText.text = "Level " + completedLevel + " Complete";
            if (continueButton != null) continueButton.gameObject.SetActive(hasNext);
            if (homeButton != null) homeButton.gameObject.SetActive(!hasNext);
        }

        public override void Setup()
        {
            base.Setup();
            if (wired) return;
            if (continueButton != null) continueButton.onClick.AddListener(ButtonContinue);
            if (homeButton != null) homeButton.onClick.AddListener(ButtonHome);
            wired = true;
        }

        public void ButtonContinue()
        {
            if (hasNextLevel) GameManager.Instance.ContinueLevel();
        }

        public void ButtonHome() => GameManager.Instance.ShowMenu();
    }
}
