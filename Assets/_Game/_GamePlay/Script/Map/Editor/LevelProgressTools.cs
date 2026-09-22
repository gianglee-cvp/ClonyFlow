using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColonyFlow.Gameplay.Editor
{
    public static class LevelProgressTools
    {
        private const string ResetMenuPath = "ColonyFlow/Debug/Reset Progress To Level 0";
        private const string SaveFileName = "playerData.json";

        [MenuItem(ResetMenuPath)]
        public static void ResetToLevelZero()
        {
            if (!EditorUtility.DisplayDialog("Reset Level Progress",
                    "Reset progress to level index 0 (the first playable level)?", "Reset", "Cancel"))
                return;

            if (Application.isPlaying)
            {
                var levelManager = Object.FindAnyObjectByType<LevelManager>();
                int levelCount = levelManager != null ? levelManager.LevelCount : 1;
                PlayerDataManager.Instance.ResetData(Mathf.Max(1, levelCount));
                Object.FindAnyObjectByType<GameManager>()?.ShowMenu();
            }
            else
            {
                string savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(savePath, JsonUtility.ToJson(new PlayerData(), true));
            }

            Debug.Log("Level progress reset to index 0 (display level 1).");
        }
    }
}
