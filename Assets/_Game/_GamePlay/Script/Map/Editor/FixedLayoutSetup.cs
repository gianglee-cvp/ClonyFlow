using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColonyFlow.Gameplay.Editor
{
    public static class FixedLayoutSetup
    {
        private const string ScenePath = "Assets/_Game/_GamePlay/Scenes/MapDemo.unity";
        [MenuItem("ColonyFlow/Map/Force Rebuild Fixed Portrait Layout")]
        public static void ApplyFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Rebuild Gameplay Layout?",
                    "Thao tác này sẽ xóa GameplayRoot hiện tại và tạo lại từ Legacy Settings. " +
                    "Các chỉnh sửa thủ công trong Scene sẽ bị ghi đè.", "Rebuild", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply();
        }

        public static void ApplyFromSettings(GameplayLayoutSettings settings)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply(settings);
        }

        // All layout and camera framing is baked into the scene, never adjusted at runtime.
        public static void Apply()
        {
            Apply(GameplayLayoutSettings.LoadOrCreate());
        }

        public static void Apply(GameplayLayoutSettings settings)
        {
            settings.ValidateValues();
            if (!settings.IsCameraProjectionValid(out string cameraWarning))
            {
                Debug.LogError("Cannot apply gameplay layout: " + cameraWarning);
                return;
            }
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var view = UnityEngine.Object.FindFirstObjectByType<MapView>();
            var camera = Camera.main;
            if (view == null || camera == null) return;
            var cellPrefab = GameplayDemoSetup.PrepareRoundedCell();
            if (cellPrefab == null)
            {
                Debug.LogError("Gameplay layout rebuild cancelled: RoundedMapCell prefab is invalid. Existing scene was preserved.");
                return;
            }

            ConfigureMapRoot(view.Root);
            ConfigureCamera(camera, settings);
            ConfigureLight();
            ConfigurePortrait();
            if (!GameplayDemoSetup.BuildIntoScene(view, camera, settings)) return;

            view.Clear();
            view.ConfigureCellPrefab(cellPrefab);
            UIFlowSetup.RewireSceneOnly();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureMapRoot(Transform root)
        {
            root.name = "MapRoot";
            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.localScale = Vector3.one;
        }

        private static void ConfigureCamera(Camera camera, GameplayLayoutSettings settings)
        {
            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(settings.cameraPosition, Quaternion.Euler(settings.cameraRotation));
            camera.orthographicSize = settings.orthographicSize;
            camera.nearClipPlane = settings.nearClipPlane;
            camera.farClipPlane = settings.farClipPlane;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#FFD18E");
            camera.ResetAspect();
        }

        private static void ConfigureLight()
        {
            var light = SceneObjects.FindRoot("Map Light");
            if (light == null) light = new GameObject("Map Light", typeof(Light));
            var key = light.GetComponent<Light>();
            key.type = LightType.Directional; key.intensity = .85f;
            key.color = new Color(1, .96f, .88f); key.shadows = LightShadows.None;
            key.shadowStrength = .2f; key.shadowBias = .03f;
            light.transform.rotation = Quaternion.Euler(50, -35, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.65f, .65f, .65f);
        }

        private static void ConfigurePortrait()
        {
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var color);
            return color;
        }

    }
}
