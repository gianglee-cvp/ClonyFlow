using System;
using System.IO;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    [Serializable]
    public sealed class PlayerData
    {
        public int dataVersion = 2;
        public int currentLevel = 1;
        public bool musicEnabled = true;
        public bool sfxEnabled = true;
    }

    public sealed class PlayerDataManager : Singleton<PlayerDataManager>
    {
        private const string FileName = "playerData.json";
        private PlayerData data;

        public int CurrentLevel => data?.currentLevel ?? 1;
        public bool MusicEnabled => data?.musicEnabled ?? true;
        public bool SfxEnabled => data?.sfxEnabled ?? true;
        public string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public void Init(int levelCount)
        {
            data = ReadData();
            if (data.dataVersion < 2)
            {
                data.dataVersion = 2;
                data.musicEnabled = true;
                data.sfxEnabled = true;
            }
            data.currentLevel = Mathf.Clamp(data.currentLevel, 1, Mathf.Max(1, levelCount));
            Save();
        }

        public void SetCurrentLevel(int level, int levelCount)
        {
            if (data == null) data = new PlayerData();
            data.currentLevel = Mathf.Clamp(level, 1, Mathf.Max(1, levelCount));
            Save();
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (data == null) data = new PlayerData();
            if (data.musicEnabled == enabled) return;
            data.musicEnabled = enabled;
            Save();
        }

        public void SetSfxEnabled(bool enabled)
        {
            if (data == null) data = new PlayerData();
            if (data.sfxEnabled == enabled) return;
            data.sfxEnabled = enabled;
            Save();
        }

        public void ResetData(int levelCount)
        {
            data = new PlayerData();
            data.currentLevel = Mathf.Clamp(data.currentLevel, 1, Mathf.Max(1, levelCount));
            Save();
        }

        public void Save()
        {
            if (data == null) data = new PlayerData();
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }

        private PlayerData ReadData()
        {
            if (!File.Exists(SavePath)) return new PlayerData();
            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrWhiteSpace(json)) return new PlayerData();
            var loaded = JsonUtility.FromJson<PlayerData>(json);
            return loaded ?? new PlayerData();
        }
    }
}