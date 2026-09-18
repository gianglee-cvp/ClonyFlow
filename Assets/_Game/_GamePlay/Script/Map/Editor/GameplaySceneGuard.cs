using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonyFlow.Gameplay.Editor
{
    [InitializeOnLoad]
    public static class GameplaySceneGuard
    {
        static GameplaySceneGuard() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) UpgradeOpenScene();
        }
        public static bool UpgradeOpenScene()
        {
            if (SceneManager.GetActiveScene().path != "Assets/_Game/_GamePlay/Scenes/MapDemo.unity") return false;
            var existing = Object.FindFirstObjectByType<AntGameplay>();
            if (existing != null)
            {
                var serialized = new SerializedObject(existing);
                if (serialized.FindProperty("boxQueuesJson").objectReferenceValue == null)
                    existing.ConfigureQueues(AssetDatabase.LoadAssetAtPath<TextAsset>(
                        "Assets/_Game/Data/BoxQueues/demo-box-queues.json"));
                return false;
            }
            var map = Object.FindFirstObjectByType<MapView>();
            if (map == null || Camera.main == null) return false;
            // Upgrade in memory; preserve the open scene instead of reloading unsaved work.
            map.SetDemoBounds(true);
            GameplayDemoSetup.BuildIntoScene(map, Camera.main);
            Debug.Log("Upgraded legacy MapDemo: empty Slots and pickable 3D Box queues.");
            return true;
        }
    }
}
