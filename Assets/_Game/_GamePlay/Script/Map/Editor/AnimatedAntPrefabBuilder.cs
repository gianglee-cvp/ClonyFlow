using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ColonyFlow.Gameplay.Editor
{
    public static class AnimatedAntPrefabBuilder
    {
        private const string PreviewPath = "Assets/_Game/_GamePlay/Prefabs/AntModelPreview.prefab";
        private const string GameplayPath = "Assets/_Game/_GamePlay/Prefabs/Ant.prefab";
        private const string LegacyPath = "Assets/_Game/_GamePlay/Prefabs/AntLegacy.prefab";


        [MenuItem("ColonyFlow/Ant/Rebuild Model And Gameplay")]
        public static void RebuildAll()
        {
            ProceduralAntModelBuilder.Build();
            Build();
        }
        [MenuItem("ColonyFlow/Ant/Build Animated Gameplay Prefab")]
        public static void Build()
        {
            var preview = AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPath);
            if (preview == null)
            {
                Debug.LogError($"Missing ant model preview at {PreviewPath}.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPath) == null)
                AssetDatabase.CopyAsset(GameplayPath, LegacyPath);

            AddPreviewAnimation();

            var root = (GameObject)PrefabUtility.InstantiatePrefab(preview);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "Ant";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one * .35f;

            Transform visual = root.transform.Find("Visual");
            Transform carryPoint = visual.Find("CarryPoint");
            Renderer abdomen = visual.Find("Abdomen").GetComponent<Renderer>();

            var brick = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brick.name = "Carried Brick";
            Object.DestroyImmediate(brick.GetComponent<Collider>());
            brick.transform.SetParent(carryPoint, false);
            brick.transform.localPosition = Vector3.zero;
            brick.transform.localRotation = Quaternion.Euler(0, 45f, 0);
            brick.transform.localScale = new Vector3(.46f, .22f, .46f);
            Renderer brickRenderer = brick.GetComponent<Renderer>();
            brickRenderer.sharedMaterial = abdomen.sharedMaterial;

            var procedural = root.GetComponent<ProceduralAntAnimation>();
            if (procedural == null) procedural = root.AddComponent<ProceduralAntAnimation>();
            procedural.ConfigurePreview(false);
            var actor = root.AddComponent<AntActor>();
            actor.Configure(visual, brickRenderer, abdomen);
            actor.ConfigureJumpHeight(.3f);
            brick.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, GameplayPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPath);
            Debug.Log($"Animated ant prefab created at {GameplayPath}. Legacy prefab saved at {LegacyPath}.");
        }

        private static void AddPreviewAnimation()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PreviewPath);
            var animation = root.GetComponent<ProceduralAntAnimation>();
            if (animation == null) animation = root.AddComponent<ProceduralAntAnimation>();
            animation.ConfigurePreview(true);
            PrefabUtility.SaveAsPrefabAsset(root, PreviewPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}