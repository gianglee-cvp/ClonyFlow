using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ColonyFlow.Gameplay.Editor
{
    public static class AnimatedAntPrefabBuilder
    {
        private const string PreviewPath = "Assets/_Game/_GamePlay/Prefabs/AntModelPreview.prefab";
        private const string GameplayPath = "Assets/_Game/_GamePlay/Prefabs/Ant.prefab";
        private const string LegacyPath = "Assets/_Game/_GamePlay/Prefabs/AntLegacy.prefab";
        private const string ContactShadowPath = "Assets/_Game/GUIPackage/GUI/_Other/object_shadow.png";

        [MenuItem("ColonyFlow/Ant/Attach Contact Shadow To Gameplay Prefab")]
        public static void AttachContactShadowToGameplayPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(GameplayPath);
            try
            {
                ConfigureContactShadow(root, root.GetComponent<AntActor>());
                PrefabUtility.SaveAsPrefabAsset(root, GameplayPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureContactShadow(GameObject root, AntActor actor)
        {
            var shadow = root.transform.Find("Ant Contact Shadow");
            if (shadow == null)
            {
                shadow = new GameObject("Ant Contact Shadow").transform;
                shadow.SetParent(root.transform, false);
            }
            shadow.localPosition = new Vector3(0f, .025f, 0f);
            shadow.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var renderer = shadow.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = shadow.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ContactShadowPath);
            if (renderer.sprite == null)
                Debug.LogError($"Missing contact shadow sprite at {ContactShadowPath}.");
            else
            {
                var size = renderer.sprite.bounds.size;
                shadow.localScale = new Vector3(.9f / size.x, 1.15f / size.y, 1f);
            }
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            actor.ConfigureContactShadow(renderer);
        }

        [MenuItem("ColonyFlow/Ant/Optimize Shadow Casters")]
        public static void OptimizeShadows()
        {
            OptimizePrefabShadows(PreviewPath);
            OptimizePrefabShadows(GameplayPath);
            AssetDatabase.SaveAssets();
        }

        private static void OptimizePrefabShadows(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    string name = renderer.gameObject.name;
                    renderer.shadowCastingMode = path == PreviewPath &&
                        (name == "Abdomen" || name == "Thorax" || name == "Head")
                        ? ShadowCastingMode.On : ShadowCastingMode.Off;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }


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
            brickRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var procedural = root.GetComponent<ProceduralAntAnimation>();
            if (procedural == null) procedural = root.AddComponent<ProceduralAntAnimation>();
            procedural.ConfigurePreview(false);
            var actor = root.AddComponent<AntActor>();
            actor.Configure(visual, brickRenderer, abdomen);
            actor.ConfigureJumpHeight(.3f);
            ConfigureContactShadow(root, actor);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.Off;
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
