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
        private static Font font;

        [MenuItem("ColonyFlow/UI/Build UI And Level Flow")]
        public static void ApplyFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply();
        }

        public static void BuildPauseSettingsPrefabs()
        {
            Directory.CreateDirectory(PrefabRoot);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RemoveGameplayRestartButton();
            CreateSettingPrefab();
            AssetDatabase.SaveAssets();
        }

        private static void RemoveGameplayRestartButton()
        {
            string path = PrefabRoot + "/CanvasGamePlay.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var restart = root.transform.Find("Restart Button");
            if (restart != null) Object.DestroyImmediate(restart.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        public static void Apply()
        {
            Directory.CreateDirectory(PrefabRoot);
            CreateLevelVariants();
            AssetDatabase.Refresh();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ConfigureMenuBackground();
            CreateMenuPrefab();
            CreateGameplayPrefab();
            CreateSettingPrefab();
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

        private static void ConfigureMenuBackground()
        {
            const string path = "Assets/Resources/UI/Art/MenuGardenBackground.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }
        private static void CreateMenuPrefab()
        {
            var canvas = CanvasRoot<CanvasMenu>("CanvasMenu");

            var background = Rect(canvas.transform, "Garden Background", Vector2.zero, Vector2.one);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Art/MenuGardenBackground.png");
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = false;

            Label(canvas.transform, "Update Label", "NEW UPDATE", new Vector2(.035f, .955f), new Vector2(.35f, .99f), 22, Color.white);
            StaticPill(canvas.transform, "Coin Panel", "COIN  540", new Vector2(.25f, .89f), new Vector2(.49f, .945f), Hex("#2E8CA8"));
            StaticPill(canvas.transform, "Life Panel", "LIFE  3", new Vector2(.54f, .89f), new Vector2(.76f, .945f), Hex("#3F7A94"));
            StaticPill(canvas.transform, "Timer Panel", "14m", new Vector2(.76f, .89f), new Vector2(.94f, .945f), Hex("#3F7A94"));

            var progress = Panel(canvas.transform, "Progress Bar", new Vector2(.07f, .81f), new Vector2(.93f, .85f), Hex("#15468C"), false);
            Panel(progress, "Progress Fill", new Vector2(.015f, .18f), new Vector2(.55f, .82f), Hex("#FFA608"), false);
            Label(progress, "Progress Text", "30 / 200", Vector2.zero, Vector2.one, 26, Color.white);

            StaticEvent(canvas.transform, "Left Event 1", "STAR", new Vector2(.035f, .70f), new Vector2(.17f, .78f), Hex("#F2732E"));
            StaticEvent(canvas.transform, "Left Event 2", "GIFT", new Vector2(.035f, .58f), new Vector2(.17f, .66f), Hex("#70BF33"));
            StaticEvent(canvas.transform, "Right Event 1", "DAILY", new Vector2(.83f, .70f), new Vector2(.965f, .78f), Hex("#F29E1F"));
            StaticEvent(canvas.transform, "Right Event 2", "EVENT", new Vector2(.83f, .58f), new Vector2(.965f, .66f), Hex("#9E43D4"));

            Text level = Label(canvas.transform, "Level Text", "LEVEL\n1", new Vector2(.27f, .51f), new Vector2(.73f, .72f), 58, Color.white);
            level.resizeTextForBestFit = true;
            level.resizeTextMinSize = 30;
            level.resizeTextMaxSize = 70;
            AddOutline(level, new Color(.25f, .10f, .45f), new Vector2(4, -4));

            Button play = Button(canvas.transform, "Play Button", "PLAY", new Vector2(.25f, .105f), new Vector2(.75f, .19f), Hex("#54E012"), out Text playLabel);
            playLabel.fontSize = 48;
            AddOutline(playLabel, Hex("#143F08"), new Vector2(3, -3));

            Panel(canvas.transform, "Bottom Bar", Vector2.zero, new Vector2(1, .085f), new Color(.12f, .34f, .65f, .96f), false);
            Label(canvas.transform, "Shop Tab", "SHOP", new Vector2(.02f, .015f), new Vector2(.19f, .065f), 20, Color.white);
            Label(canvas.transform, "Collection Tab", "CUP", new Vector2(.21f, .015f), new Vector2(.38f, .065f), 20, Color.white);
            var home = Panel(canvas.transform, "Home Tab", new Vector2(.39f, 0), new Vector2(.61f, .10f), Hex("#2E78D1"), false);
            Label(home, "Home Label", "HOME", new Vector2(0, .08f), Vector2.one, 23, Color.white);
            Label(canvas.transform, "Event Tab", "EVENT", new Vector2(.63f, .015f), new Vector2(.80f, .065f), 20, Color.white);
            Label(canvas.transform, "Setting Tab", "SETTING", new Vector2(.81f, .015f), new Vector2(.99f, .065f), 18, Color.white);

            Set(canvas, "levelText", level);
            Set(canvas, "playButton", play);
            Save(canvas.gameObject, "CanvasMenu");
        }

        private static void StaticPill(Transform parent, string name, string value, Vector2 min, Vector2 max, Color color)
        {
            var panel = Panel(parent, name, min, max, color, false);
            Label(panel, "Text", value, Vector2.zero, Vector2.one, 25, Color.white);
        }

        private static void StaticEvent(Transform parent, string name, string value, Vector2 min, Vector2 max, Color color)
        {
            var panel = Panel(parent, name, min, max, color, false);
            Label(panel, "Label", value, Vector2.zero, Vector2.one, 18, Color.white);
        }

        private static void AddOutline(Text text, Color color, Vector2 distance)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void CreateGameplayPrefab()
        {
            var canvas = CanvasRoot<CanvasGamePlay>("CanvasGamePlay");
            var pointer = Panel(canvas.transform, "Gameplay Pointer Surface", new Vector2(.02f, .12f), new Vector2(.98f, .91f), new Color(1, 1, 1, .001f), true);
            var pointerSurface = pointer.gameObject.AddComponent<GameplayPointerSurface>();

            Panel(canvas.transform, "Header", new Vector2(0, .91f), Vector2.one, new Color(1, .82f, .55f, .94f), false);
            Button pause = Button(canvas.transform, "Pause Button", "II", new Vector2(.025f, .925f), new Vector2(.13f, .985f), Hex("#5878A8"), out _);
            Text level = Label(canvas.transform, "Level Text", "Level 1", new Vector2(.30f, .925f), new Vector2(.70f, .985f), 46, Hex("#5A381A"));
            Button speed = Button(canvas.transform, "Speed Button", "x1", new Vector2(.78f, .925f), new Vector2(.97f, .985f), Hex("#8B887E"), out Text speedText);

            Text status = Label(canvas.transform, "Status Text", "Bricks", new Vector2(.04f, .13f), new Vector2(.68f, .18f), 28, Hex("#5A381A"));

            Panel(canvas.transform, "Booster Bar", Vector2.zero, new Vector2(1, .12f), Hex("#6882AE"), false);
            Button addSlot = Button(canvas.transform, "Add Slot Button", "Add Slot", new Vector2(.03f, .025f), new Vector2(.31f, .095f), Hex("#F7D750"), out _);
            CanvasGroup addSlotGroup = addSlot.gameObject.AddComponent<CanvasGroup>();
            Button pickup = Button(canvas.transform, "Pickup Button", "Pickup", new Vector2(.36f, .025f), new Vector2(.64f, .095f), Hex("#6AD9F1"), out _);
            CanvasGroup pickupGroup = pickup.gameObject.AddComponent<CanvasGroup>();
            Button blow = Button(canvas.transform, "Blow Button", "Blow", new Vector2(.69f, .025f), new Vector2(.97f, .095f), Hex("#B8614D"), out _);
            CanvasGroup blowGroup = blow.gameObject.AddComponent<CanvasGroup>();

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
            Set(canvas, "addSlotButton", addSlot); Set(canvas, "addSlotButtonGroup", addSlotGroup);
            Set(canvas, "pickupButton", pickup); Set(canvas, "pickupButtonGroup", pickupGroup);
            Set(canvas, "blowButton", blow); Set(canvas, "blowButtonGroup", blowGroup); Set(canvas, "cancelButton", cancel); Set(canvas, "selectionPanel", selection.gameObject);
            Set(canvas, "pointerSurface", pointerSurface); SetList(canvas, "colorButtons", colorButtons);
            SetList(canvas, "colorLabels", colorLabels); SetList(canvas, "colorImages", colorImages);
            selection.gameObject.SetActive(false);
            Save(canvas.gameObject, "CanvasGamePlay");
        }

        private static void CreateSettingPrefab()
        {
            var canvas = CanvasRoot<CanvasSetting>("CanvasSetting");
            Panel(canvas.transform, "Blocker", Vector2.zero, Vector2.one, new Color(0, 0, 0, .58f), true);
            var card = Panel(canvas.transform, "Setting Card", new Vector2(.12f, .25f), new Vector2(.88f, .75f), Hex("#FFF4DA"), true);
            Label(card, "Title", "SETTINGS", new Vector2(.05f, .76f), new Vector2(.95f, .94f), 54, Hex("#5A381A"));
            Button resume = Button(card, "Resume Button", "RESUME", new Vector2(.15f, .53f), new Vector2(.85f, .68f), Hex("#5B83C3"), out _);
            Button restart = Button(card, "Restart Button", "RESTART", new Vector2(.15f, .32f), new Vector2(.85f, .47f), Hex("#B8614D"), out _);
            Button home = Button(card, "Home Button", "HOME", new Vector2(.15f, .11f), new Vector2(.85f, .26f), Hex("#8B887E"), out _);
            Set(canvas, "resumeButton", resume); Set(canvas, "restartButton", restart); Set(canvas, "homeButton", home);
            Save(canvas.gameObject, "CanvasSetting");
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
