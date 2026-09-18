using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColonyFlow.Gameplay.Editor
{
    public static class MapDemoSetup
    {
        private const string ScenePath = "Assets/_Game/_GamePlay/Scenes/MapDemo.unity";
        private const string MaterialPath = "Assets/_Game/_GamePlay/Materials/MapCell.mat";
        private const string JsonPath = "Assets/_Game/Data/Maps/star-map.json";

        [MenuItem("ColonyFlow/Map/Open or Create Demo")]
        public static void OpenOrCreateDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else CreateDemoAssets();
        }

        public static void CreateDemoAssets()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            if (json == null) return;
            var model = MapJsonLoader.Load(json.text);
            if (model == null || !PrepareMaterial()) return;
            var prefab = GameplayDemoSetup.PrepareRoundedCell();
            if (prefab == null || File.Exists(ScenePath)) return;
            CreateScene(prefab, json, model);
            FixedLayoutSetup.Apply();
        }

        private static bool PrepareMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            Directory.CreateDirectory("Assets/_Game/_GamePlay/Prefabs");
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null) return true;
            AssetDatabase.CreateAsset(new Material(shader) { name = "MapCell" }, MaterialPath);
            return true;
        }

        private static void CreateScene(CellView prefab, TextAsset json, MapModel model)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreateCamera(model);
            var light = new GameObject("Map Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var owner = new GameObject("Map");
            owner.AddComponent<MapView>().Configure(prefab, camera, owner.transform, json);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static Camera CreateCamera(MapModel model)
        {
            var owner = new GameObject("Map Camera");
            owner.tag = "MainCamera";
            owner.AddComponent<AudioListener>();
            var camera = owner.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .15f, .2f);
            camera.orthographic = true;
            camera.orthographicSize = 6;
            camera.transform.SetPositionAndRotation(model.CameraPosition, Quaternion.Euler(model.CameraRotation));
            return camera;
        }
    }
}
