using System.Collections;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public enum GameFlowState { Menu, Loading, Playing, Win, Lose }

    public sealed class GameManager : Singleton<GameManager>
    {
        public GameFlowState State { get; private set; }
        private const float MinimumLoadingDuration = 2f;
        private AntGameplay subscribedGameplay;
        private Coroutine loadingRoutine;

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
            if (loadingRoutine != null)
            {
                StopCoroutine(loadingRoutine);
                loadingRoutine = null;
            }
            State = GameFlowState.Menu;
            LevelManager.Instance.StopLevel();
            UIManager.Instance.CloseAllUI();
            UIManager.Instance.OpenUI<CanvasMenu>();
        }

        public void PlayCurrentLevel()
        {
            if (loadingRoutine == null) loadingRoutine = StartCoroutine(LoadCurrentLevelRoutine());
        }

        private IEnumerator LoadCurrentLevelRoutine()
        {
            State = GameFlowState.Loading;
            UIManager.Instance.CloseAllUI();
            var loading = UIManager.Instance.OpenUI<CanvasLoading>();
            float startedAt = Time.unscaledTime;
            yield return null;

            while (Time.unscaledTime - startedAt < MinimumLoadingDuration)
            {
                float progress = (Time.unscaledTime - startedAt) / MinimumLoadingDuration;
                loading?.SetProgress(progress * .95f);
                yield return null;
            }
            bool loaded = LevelManager.Instance.LoadCurrentLevel();
            loading?.SetProgress(1);
            yield return null;
            loadingRoutine = null;

            if (!loaded)
            {
                ShowMenu();
                yield break;
            }

            State = GameFlowState.Playing;
            SubscribeGameplay();
            UIManager.Instance.CloseAllUI();
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
