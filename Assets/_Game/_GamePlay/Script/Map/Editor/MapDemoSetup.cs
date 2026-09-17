using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonyFlow.Gameplay.Editor
{
    public static class MapDemoSetup
    {
        private const string ScenePath = "Assets/_Game/_GamePlay/Scenes/MapDemo.unity";
        private const string PrefabPath = "Assets/_Game/_GamePlay/Prefabs/MapCell.prefab";
        private const string MaterialPath = "Assets/_Game/_GamePlay/Materials/MapCell.mat";
        private const string JsonPath = "Assets/_Game/Data/Maps/demo-map.json";

        [MenuItem("ColonyFlow/Map/Open or Create Demo")]
        public static void OpenOrCreateDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else CreateDemoAssets();
        }

        // Can also run in batch mode in an isolated verification project.
        public static void CreateDemoAssets()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.Refresh();
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            if (json == null) throw new InvalidOperationException("Demo map JSON is missing: " + JsonPath);
            var model = MapJsonLoader.Load(json.text);

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("A Lit shader is required for the demo.");
                material = new Material(shader) { name = "MapCell" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "MapCell";
                cube.transform.localScale = new Vector3(1, 0.4f, 1);
                cube.GetComponent<Renderer>().sharedMaterial = material;
                cube.AddComponent<CellView>();
                prefab = PrefabUtility.SaveAsPrefabAsset(cube, PrefabPath);
                UnityEngine.Object.DestroyImmediate(cube);
            }

            if (File.Exists(ScenePath)) return; // Do not overwrite an edited demo scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var cameraObject = new GameObject("Map Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.15f, 0.2f);
            camera.orthographic = true;
            camera.orthographicSize = 6;
            camera.transform.SetPositionAndRotation(model.CameraPosition, Quaternion.Euler(model.CameraRotation));
            var light = new GameObject("Map Light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var owner = new GameObject("Map");
            owner.AddComponent<MapView>().Configure(prefab, camera, owner.transform, json);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Map demo created: " + ScenePath + ". Enter Play Mode to spawn the JSON map.");
        }
    }
}
