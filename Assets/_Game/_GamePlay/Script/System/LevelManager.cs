using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class LevelManager : Singleton<LevelManager>
    {
        [SerializeField] private List<TextAsset> levels = new List<TextAsset>();
        [SerializeField] private MapView mapView;
        [SerializeField] private AntGameplay gameplay;

        public int LevelCount => levels.Count;
        public int CurrentLevel => PlayerDataManager.Instance.CurrentLevel;
        public bool HasNextLevel => CurrentLevel < LevelCount;
        public AntGameplay Gameplay => gameplay;

        public void Configure(MapView view, AntGameplay game, IEnumerable<TextAsset> levelAssets)
        {
            mapView = view;
            gameplay = game;
            levels.Clear();
            if (levelAssets != null) levels.AddRange(levelAssets);
        }

        public void Init()
        {
            PlayerDataManager.Instance.Init(LevelCount);
            StopLevel();
        }

        public bool LoadCurrentLevel() => LoadLevel(CurrentLevel);

        public bool LoadLevel(int level)
        {
            if (mapView == null || gameplay == null || level < 1 || level > levels.Count || levels[level - 1] == null)
                return false;
            gameplay.enabled = false;
            bool loaded = mapView.LoadJson(levels[level - 1].text);
            gameplay.enabled = loaded;
            return loaded;
        }

        public bool RestartLevel() => LoadCurrentLevel();

        public void CompleteCurrentLevel()
        {
            if (HasNextLevel)
                PlayerDataManager.Instance.SetCurrentLevel(CurrentLevel + 1, LevelCount);
        }

        public void StopLevel()
        {
            if (gameplay != null) gameplay.enabled = false;
            if (mapView != null) mapView.Clear();
        }
    }
}
