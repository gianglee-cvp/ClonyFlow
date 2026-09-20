using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay.Editor
{
    public static class UIFlowSetup
    {
        private const string ScenePath = "Assets/_Game/_GamePlay/Scenes/MapDemo.unity";
        private const string MapRoot = "Assets/_Game/Data/Maps";
        private const string PrefabRoot = "Assets/Resources/UI";
        private const string GameplayBackgroundPath = "Assets/_Game/_GamePlay/UI/GameplayBackground.png";
        private static Font font;

        [MenuItem("ColonyFlow/UI/Build UI And Level Flow")]
        public static void ApplyFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply();
        }

        public static void Apply()
        {
            Directory.CreateDirectory(PrefabRoot);
            CreateLevelVariants();
            AssetDatabase.Refresh();
            ConfigureGameplayBackground();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateMenuPrefab();
            CreateGameplayPrefab();
            CreateWinPrefab();
            CreateLosePrefab();
            ConfigureScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static void CreateLevelVariants()
        {
            string source = File.ReadAllText(MapRoot + "/map1.json");
            var second = JsonUtility.FromJson<MapJsonData>(source);
            MirrorRows(second);
            System.Array.Reverse(second.queues);
            File.WriteAllText(MapRoot + "/map2.json", JsonUtility.ToJson(second, true));

            var third = JsonUtility.FromJson<MapJsonData>(source);
            MirrorRows(third);
            RemapColors(third, new[] { 0, 3, 4, 5, 1, 2 });
            File.WriteAllText(MapRoot + "/map3.json", JsonUtility.ToJson(third, true));
        }

        private static void MirrorRows(MapJsonData data)
        {
            for (int row = 0; row < data.rows; row++)
                System.Array.Reverse(data.cells, row * data.columns, data.columns);
        }

        private static void RemapColors(MapJsonData data, int[] mapping)
        {
            for (int i = 0; i < data.cells.Length; i++)
                if (data.cells[i] > 0 && data.cells[i] < mapping.Length) data.cells[i] = mapping[data.cells[i]];
            foreach (var queue in data.queues)
                foreach (var box in queue.boxes)
                    if (box.colorId > 0 && box.colorId < mapping.Length) box.colorId = mapping[box.colorId];
        }

        private static void CreateMenuPrefab()
        {
            var canvas = CanvasRoot<CanvasMenu>("CanvasMenu");
            Panel(canvas.transform, "Background", Vector2.zero, Vector2.one, Hex("#FFCB85"), false);
            Text label = Label(canvas.transform, "Title", "COLONY FLOW", new Vector2(.1f, .72f), new Vector2(.9f, .84f), 72, Hex("#5A381A"));
            Text level = Label(canvas.transform, "Level Text", "Level 1", new Vector2(.2f, .48f), new Vector2(.8f, .60f), 58, Hex("#5A381A"));
            Button play = Button(canvas.transform, "Play Button", "PLAY", new Vector2(.25f, .27f), new Vector2(.75f, .39f), Hex("#5B83C3"), out _);
            Set(canvas, "levelText", level);
            Set(canvas, "playButton", play);
            Save(canvas.gameObject, "CanvasMenu");
        }

        private static void CreateGameplayPrefab()
        {
            var canvas = CanvasRoot<CanvasGamePlay>("CanvasGamePlay", RenderMode.ScreenSpaceCamera);
            var background = Rect(canvas.transform, "Gameplay Background", Vector2.zero, Vector2.one);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameplayBackgroundPath);
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = false;
            var pointer = Panel(canvas.transform, "Gameplay Pointer Surface", new Vector2(.02f, .12f), new Vector2(.98f, .91f), new Color(1, 1, 1, .001f), true);
            var pointerSurface = pointer.gameObject.AddComponent<GameplayPointerSurface>();

            Panel(canvas.transform, "Header", new Vector2(0, .91f), Vector2.one, new Color(1, .82f, .55f, .94f), false);
            Button pause = Button(canvas.transform, "Pause Button", "II", new Vector2(.025f, .925f), new Vector2(.13f, .985f), Hex("#5878A8"), out _);
            Text level = Label(canvas.transform, "Level Text", "Level 1", new Vector2(.30f, .925f), new Vector2(.70f, .985f), 46, Hex("#5A381A"));
            Button speed = Button(canvas.transform, "Speed Button", "x1", new Vector2(.78f, .925f), new Vector2(.97f, .985f), Hex("#8B887E"), out Text speedText);

            Text status = Label(canvas.transform, "Status Text", "Bricks", new Vector2(.04f, .13f), new Vector2(.68f, .18f), 28, Hex("#5A381A"));
            Button restart = Button(canvas.transform, "Restart Button", "Restart", new Vector2(.72f, .13f), new Vector2(.96f, .18f), Hex("#B8614D"), out _);

            Panel(canvas.transform, "Booster Bar", Vector2.zero, new Vector2(1, .12f), Hex("#6882AE"), false);
            Button addSlot = Button(canvas.transform, "Add Slot Button", "Add Slot", new Vector2(.03f, .025f), new Vector2(.31f, .095f), Hex("#F7D750"), out _);
            Button pickup = Button(canvas.transform, "Pickup Button", "Pickup", new Vector2(.36f, .025f), new Vector2(.64f, .095f), Hex("#6AD9F1"), out _);
            Button blow = Button(canvas.transform, "Blow Button", "Blow", new Vector2(.69f, .025f), new Vector2(.97f, .095f), Hex("#B8614D"), out _);

            var selection = Panel(canvas.transform, "Selection Panel", new Vector2(.03f, .19f), new Vector2(.97f, .30f), new Color(.18f, .16f, .18f, .88f), true);
            Text selectionText = Label(selection, "Selection Text", "Choose a color", new Vector2(.02f, .58f), new Vector2(.72f, .95f), 24, Color.white);
            Button cancel = Button(selection, "Cancel Button", "Cancel", new Vector2(.76f, .58f), new Vector2(.98f, .95f), Hex("#8B887E"), out _);
            var colorButtons = new Button[6];
            var colorLabels = new Text[6];
            var colorImages = new Image[6];
            for (int i = 0; i < colorButtons.Length; i++)
            {
                float min = .02f + i * .16f;
                colorButtons[i] = Button(selection, "Color Button " + (i + 1), (i + 1).ToString(),
                    new Vector2(min, .08f), new Vector2(min + .14f, .48f), Color.white, out colorLabels[i]);
                colorImages[i] = colorButtons[i].GetComponent<Image>();
            }

            Set(canvas, "levelText", level); Set(canvas, "statusText", status); Set(canvas, "speedText", speedText);
            Set(canvas, "selectionText", selectionText); Set(canvas, "pauseButton", pause); Set(canvas, "speedButton", speed);
            Set(canvas, "restartButton", restart); Set(canvas, "addSlotButton", addSlot); Set(canvas, "pickupButton", pickup);
            Set(canvas, "blowButton", blow); Set(canvas, "cancelButton", cancel); Set(canvas, "selectionPanel", selection.gameObject);
            Set(canvas, "pointerSurface", pointerSurface); SetList(canvas, "colorButtons", colorButtons);
            SetList(canvas, "colorLabels", colorLabels); SetList(canvas, "colorImages", colorImages);
            selection.gameObject.SetActive(false);
            Save(canvas.gameObject, "CanvasGamePlay");
        }

        private static void CreateWinPrefab()
        {
            var canvas = CanvasRoot<CanvasWin>("CanvasWin");
            Panel(canvas.transform, "Blocker", Vector2.zero, Vector2.one, new Color(0, 0, 0, .58f), true);
            var card = Panel(canvas.transform, "Win Card", new Vector2(.12f, .30f), new Vector2(.88f, .72f), Hex("#FFF4DA"), true);
            Label(card, "Title", "YOU WIN!", new Vector2(.05f, .68f), new Vector2(.95f, .92f), 58, Hex("#5B83C3"));
            Text level = Label(card, "Level Text", "Level Complete", new Vector2(.05f, .48f), new Vector2(.95f, .68f), 34, Hex("#5A381A"));
            Button next = Button(card, "Continue Button", "CONTINUE", new Vector2(.15f, .15f), new Vector2(.85f, .35f), Hex("#5B83C3"), out _);
            Button home = Button(card, "Home Button", "HOME", new Vector2(.15f, .15f), new Vector2(.85f, .35f), Hex("#8B887E"), out _);
            Set(canvas, "levelText", level); Set(canvas, "continueButton", next); Set(canvas, "homeButton", home);
            Save(canvas.gameObject, "CanvasWin");
        }

        private static void CreateLosePrefab()
        {
            var canvas = CanvasRoot<CanvasLose>("CanvasLose");
            Panel(canvas.transform, "Blocker", Vector2.zero, Vector2.one, new Color(0, 0, 0, .58f), true);
            var card = Panel(canvas.transform, "Lose Card", new Vector2(.12f, .27f), new Vector2(.88f, .73f), Hex("#FFF4DA"), true);
            Label(card, "Title", "TRY AGAIN", new Vector2(.05f, .70f), new Vector2(.95f, .92f), 54, Hex("#B8614D"));
            Text level = Label(card, "Level Text", "Level Failed", new Vector2(.05f, .51f), new Vector2(.95f, .70f), 32, Hex("#5A381A"));
            Button retry = Button(card, "Retry Button", "RETRY", new Vector2(.15f, .27f), new Vector2(.85f, .44f), Hex("#B8614D"), out _);
            Button home = Button(card, "Home Button", "HOME", new Vector2(.15f, .08f), new Vector2(.85f, .23f), Hex("#8B887E"), out _);
            Set(canvas, "levelText", level); Set(canvas, "retryButton", retry); Set(canvas, "homeButton", home);
            Save(canvas.gameObject, "CanvasLose");
        }

        private static T CanvasRoot<T>(string name, RenderMode renderMode = RenderMode.ScreenSpaceOverlay) where T : UICanvas
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = renderMode;
            if (renderMode == RenderMode.ScreenSpaceCamera) canvas.planeDistance = 100f;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            return root.AddComponent<T>();
        }

        private static void ConfigureGameplayBackground()
        {
            AssetDatabase.ImportAsset(GameplayBackgroundPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(GameplayBackgroundPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color, bool raycast)
        {
            var rect = Rect(parent, name, min, max);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast;
            return rect;
        }

        private static Text Label(Transform parent, string name, string value, Vector2 min, Vector2 max, int size, Color color)
        {
            var rect = Rect(parent, name, min, max);
            var text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size;
            text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter; text.color = color; text.raycastTarget = false;
            return text;
        }

        private static Button Button(Transform parent, string name, string value, Vector2 min, Vector2 max, Color color, out Text label)
        {
            var rect = Panel(parent, name, min, max, color, true);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            label = Label(rect, "Label", value, Vector2.zero, Vector2.one, 28, Color.white);
            return button;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetList<T>(Object target, string field, T[] values) where T : Object
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field); property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static void ConfigureScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SceneObjects.RemoveRoots("Game Systems", "EventSystem", "Fixed Gameplay Canvas", "Background Canvas");
            var map = Object.FindFirstObjectByType<MapView>();
            var gameplay = Object.FindFirstObjectByType<AntGameplay>();
            if (map == null || gameplay == null) return;

            var systems = new GameObject("Game Systems");
            systems.AddComponent<PlayerDataManager>();
            var level = systems.AddComponent<LevelManager>();
            var ui = systems.AddComponent<UIManager>();
            systems.AddComponent<GameManager>();
            var runtimeUi = new GameObject("Runtime UI").transform; runtimeUi.SetParent(systems.transform, false);
            Set(ui, "parent", runtimeUi);
            Set(level, "mapView", map); Set(level, "gameplay", gameplay);
            var levelAssets = new[]
            {
                AssetDatabase.LoadAssetAtPath<TextAsset>(MapRoot + "/map1.json"),
                AssetDatabase.LoadAssetAtPath<TextAsset>(MapRoot + "/map2.json"),
                AssetDatabase.LoadAssetAtPath<TextAsset>(MapRoot + "/map3.json")
            };
            SetList(level, "levels", levelAssets);

            var mapSerialized = new SerializedObject(map);
            mapSerialized.FindProperty("loadOnStart").boolValue = false;
            mapSerialized.ApplyModifiedPropertiesWithoutUndo();
            gameplay.enabled = false;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var color);
            return color;
        }
    }
}
