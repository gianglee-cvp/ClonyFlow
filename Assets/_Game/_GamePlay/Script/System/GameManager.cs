using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public enum GameFlowState { Menu, Loading, Playing, Win, Lose }

    public sealed class GameManager : Singleton<GameManager>
    {
        public GameFlowState State { get; private set; }
        private const float MinimumLoadingDuration = 2f;
        private const float WinFireworkInterval = .6f;
        [SerializeField] private GameObject winFireworkPrefab;
        private AntGameplay subscribedGameplay;
        private Coroutine loadingRoutine;
        private Coroutine winRoutine;
        private bool winProgressPending;
        private readonly List<BoxFireworkEffect> winEffects = new List<BoxFireworkEffect>();
        private AudioManager audioManager;

        private void Start() => Init();

        public void Init()
        {
            UIManager.Instance.OnInit();
            LevelManager.Instance.Init();
            audioManager = GetComponent<AudioManager>();
            if (audioManager == null) audioManager = gameObject.AddComponent<AudioManager>();
            audioManager.Init();
            SubscribeGameplay();
            ShowMenu();
        }

        private void SubscribeGameplay()
        {
            var gameplay = LevelManager.Instance.Gameplay;
            if (gameplay == subscribedGameplay) return;
            UnsubscribeGameplay();
            subscribedGameplay = gameplay;
            if (subscribedGameplay == null) return;
            subscribedGameplay.LevelCompleted += OnLevelCompleted;
            subscribedGameplay.LevelFailed += OnLevelFailed;
            subscribedGameplay.BoxSelected += OnBoxSelected;
            subscribedGameplay.AntPickupCompleted += OnAntPickupCompleted;
            subscribedGameplay.BoosterUsed += OnBoosterUsed;
        }

        private void UnsubscribeGameplay()
        {
            if (subscribedGameplay == null) return;
            subscribedGameplay.LevelCompleted -= OnLevelCompleted;
            subscribedGameplay.LevelFailed -= OnLevelFailed;
            subscribedGameplay.BoxSelected -= OnBoxSelected;
            subscribedGameplay.AntPickupCompleted -= OnAntPickupCompleted;
            subscribedGameplay.BoosterUsed -= OnBoosterUsed;
            subscribedGameplay = null;
        }

        public void ShowMenu()
        {
            StopWinRoutine();
            if (loadingRoutine != null)
            {
                StopCoroutine(loadingRoutine);
                loadingRoutine = null;
            }
            State = GameFlowState.Menu;
            audioManager?.SetMusicDucked(false);
            audioManager?.PlayMusic(AudioCue.MenuMusic);
            LevelManager.Instance.StopLevel();
            UIManager.Instance.CloseAllUI();
            UIManager.Instance.OpenUI<CanvasMenu>();
        }

        public void PlayCurrentLevel()
        {
            StopWinRoutine();
            if (loadingRoutine == null) loadingRoutine = StartCoroutine(LoadCurrentLevelRoutine());
        }

        private IEnumerator LoadCurrentLevelRoutine()
        {
            State = GameFlowState.Loading;
            audioManager?.SetMusicDucked(false);
            audioManager?.PlayMusic(AudioCue.MenuMusic);
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
            // Prepare the win view while the loading screen is still visible.
            UIManager.Instance.GetUI<CanvasWin>()?.Setup();
            audioManager?.PlayMusic(AudioCue.GameplayMusic);
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
            winProgressPending = true;
            State = GameFlowState.Win;
            winRoutine = StartCoroutine(PlayWinRoutine(completedLevel, hasNext));
        }

        private IEnumerator PlayWinRoutine(int completedLevel, bool hasNext)
        {
            // The map remains visible until all three bursts have finished.
            Vector2[] positions =
            {
                new Vector2(.5f, .58f), // mid
                new Vector2(.25f, .7f), // left decoration
                new Vector2(.75f, .7f) // right decoration
            };
            for (int i = 0; i < positions.Length; i++)
            {
                var position = positions[i];
                var effect = BoxFireworkEffect.PlayAtScreen(winFireworkPrefab,
                    new Vector2(Screen.width * position.x, Screen.height * position.y));
                if (effect != null) winEffects.Add(effect);
                WinFeedback.LightImpact();
                yield return new WaitForSecondsRealtime(WinFireworkInterval);
                if (i == 0) CommitWinProgress();
            }

            winRoutine = null;
            if (State != GameFlowState.Win) yield break;
            audioManager?.PlaySfx(AudioCue.Win);
            UIManager.Instance.CloseAllUI();
            var canvas = UIManager.Instance.OpenUI<CanvasWin>();
            canvas?.Init(completedLevel, hasNext);
        }

        private void StopWinRoutine()
        {
            if (winRoutine != null)
            {
                StopCoroutine(winRoutine);
                winRoutine = null;
            }
            foreach (var effect in winEffects)
                if (effect != null) effect.StopNow();
            winEffects.Clear();
            CommitWinProgress();
        }

        private void CommitWinProgress()
        {
            if (!winProgressPending) return;
            winProgressPending = false;
            var levelManager = FindAnyObjectByType<LevelManager>();
            if (levelManager != null) levelManager.CompleteCurrentLevel();
        }

        private void OnLevelFailed()
        {
            if (State != GameFlowState.Playing) return;
            State = GameFlowState.Lose;
            audioManager?.PlaySfx(AudioCue.Lose);
            UIManager.Instance.CloseAllUI();
            UIManager.Instance.OpenUI<CanvasLose>();
        }

        private void OnBoxSelected() => audioManager?.PlaySfx(AudioCue.BoxSelected);
        private void OnAntPickupCompleted() => audioManager?.PlaySfx(AudioCue.AntPickup);
        private void OnBoosterUsed() => audioManager?.PlaySfx(AudioCue.Booster);

        protected override void OnDestroy()
        {
            StopWinRoutine();
            UnsubscribeGameplay();
            base.OnDestroy();
        }
    }

    internal static class WinFeedback
    {
        public static void LightImpact()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        if (version.GetStatic<int>("SDK_INT") >= 26)
                        {
                            using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                            using (var pulse = effect.CallStatic<AndroidJavaObject>("createOneShot", 18L, 35))
                                vibrator.Call("vibrate", pulse);
                        }
                        else vibrator.Call("vibrate", 18L);
                    }
                }
            }
            catch (System.Exception) { /* Devices without haptics can still celebrate. */ }
#endif
        }
    }
}
