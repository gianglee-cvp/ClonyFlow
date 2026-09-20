using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public enum GameFlowState { Menu, Playing, Win, Lose }

    public sealed class GameManager : Singleton<GameManager>
    {
        public GameFlowState State { get; private set; }
        private AntGameplay subscribedGameplay;

        private void Start() => Init();

        public void Init()
        {
            UIManager.Instance.OnInit();
            LevelManager.Instance.Init();
            SubscribeGameplay();
            ShowMenu();
        }

        private void SubscribeGameplay()
        {
            var gameplay = LevelManager.Instance.Gameplay;
            if (gameplay == subscribedGameplay) return;
            if (subscribedGameplay != null)
            {
                subscribedGameplay.LevelCompleted -= OnLevelCompleted;
                subscribedGameplay.LevelFailed -= OnLevelFailed;
            }
            subscribedGameplay = gameplay;
            if (subscribedGameplay == null) return;
            subscribedGameplay.LevelCompleted += OnLevelCompleted;
            subscribedGameplay.LevelFailed += OnLevelFailed;
        }

        public void ShowMenu()
        {
            State = GameFlowState.Menu;
            LevelManager.Instance.StopLevel();
            UIManager.Instance.CloseAllUI();
            UIManager.Instance.OpenUI<CanvasMenu>();
        }

        public void PlayCurrentLevel()
        {
            UIManager.Instance.CloseAllUI();
            if (!LevelManager.Instance.LoadCurrentLevel())
            {
                ShowMenu();
                return;
            }
            State = GameFlowState.Playing;
            SubscribeGameplay();
            UIManager.Instance.OpenUI<CanvasGamePlay>();
        }

        public void RestartLevel() => PlayCurrentLevel();
        public void ContinueLevel() => PlayCurrentLevel();

        private void OnLevelCompleted()
        {
            if (State != GameFlowState.Playing) return;
            int completedLevel = LevelManager.Instance.CurrentLevel;
            bool hasNext = LevelManager.Instance.HasNextLevel;
            LevelManager.Instance.CompleteCurrentLevel();
            State = GameFlowState.Win;
            UIManager.Instance.CloseAllUI();
            var canvas = UIManager.Instance.OpenUI<CanvasWin>();
            canvas?.Init(completedLevel, hasNext);
        }

        private void OnLevelFailed()
        {
            if (State != GameFlowState.Playing) return;
            State = GameFlowState.Lose;
            UIManager.Instance.CloseAllUI();
            UIManager.Instance.OpenUI<CanvasLose>();
        }

        protected override void OnDestroy()
        {
            if (subscribedGameplay != null)
            {
                subscribedGameplay.LevelCompleted -= OnLevelCompleted;
                subscribedGameplay.LevelFailed -= OnLevelFailed;
            }
            base.OnDestroy();
        }
    }
}
