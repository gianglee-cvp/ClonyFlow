using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay.Editor
{
    public static class FixedLayoutSetup
    {
        private const string ScenePath = "Assets/_Game/_GamePlay/Scenes/MapDemo.unity";
        private const string ArtPath = "Assets/_Game/_GamePlay/UI";
        private static Sprite rounded;
        private static Font font;

        [MenuItem("ColonyFlow/Map/Apply Fixed Portrait Layout")]
        public static void ApplyFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply();
        }

        // All layout and camera framing is baked into the scene, never adjusted at runtime.
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var view = UnityEngine.Object.FindFirstObjectByType<MapView>();
            if (view == null) throw new InvalidOperationException("MapDemo needs a MapView.");
            view.Clear();
            foreach (string name in new[] { "Fixed Gameplay Canvas", "Background Canvas" })
            {
                var old = GameObject.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
            }
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("MapDemo needs a main camera.");
            var serialized = new SerializedObject(view);
            var mapRoot = (Transform)serialized.FindProperty("mapRoot").objectReferenceValue;
            mapRoot.name = "MapRoot";
            mapRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mapRoot.localScale = Vector3.one;
            // Freeze the camera against the maximum 20x20 contract, not each level's silhouette.
            view.SetDemoBounds(true);
            camera.orthographic = true;
            camera.transform.rotation = Quaternion.Euler(45, 0, 0);
            camera.orthographicSize = 9;
            camera.transform.position = new Vector3(0, 0, -3.762f) - camera.transform.forward * 80;
            camera.nearClipPlane = .3f;
            camera.farClipPlane = 300;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#FFD18E");
            camera.ResetAspect();
            var light = GameObject.Find("Map Light");
            if (light == null) light = new GameObject("Map Light", typeof(Light));
            var key = light.GetComponent<Light>();
            key.type = LightType.Directional; key.intensity = .85f;
            key.color = new Color(1, .96f, .88f); key.shadows = LightShadows.None;
            key.shadowStrength = .2f; key.shadowBias = .03f;
            light.transform.rotation = Quaternion.Euler(50, -35, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.65f, .65f, .65f);
            view.ConfigureCellPrefab(GameplayDemoSetup.PrepareRoundedCell());
            view.Clear();
            rounded = CreateRoundedSprite();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Native world-space Canvas lies behind the cells, with fixed transform and size.
            var background = NewCanvas("Background Canvas", RenderMode.WorldSpace);
            background.sizeDelta = new Vector2(1080, 1920);
            background.position = camera.transform.position + camera.transform.forward * 160;
            background.rotation = camera.transform.rotation;
            background.localScale = Vector3.one * (2 * camera.orthographicSize / 1920);
            Box(background, "Orange Background", Vector2.zero, Vector2.one, Hex("#FFCB85"), false);
            for (int i = 0; i < 20; i++)
            {
                float x = ((i * 37) % 100) / 100f;
                float y = ((i * 23 + 7) % 100) / 100f;
                var shape = Box(background, $"Background Pattern {i + 1}",
                    new Vector2(x, y), new Vector2(x + .17f, y + .075f), new Color(1, .94f, .75f, .16f), false);
                shape.localRotation = Quaternion.Euler(0, 0, 38);
            }

            var foreground = NewCanvas("Fixed Gameplay Canvas", RenderMode.ScreenSpaceOverlay);
            var scaler = foreground.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0;
            Region(foreground, "MapArea", new Vector2(.06f, .52f), new Vector2(.94f, .84f));
            var header = Region(foreground, "Header", new Vector2(0, .93f), Vector2.one);
            var pause = Card(header, "PauseSlot", new Vector2(.025f, .22f), new Vector2(.105f, .86f), Hex("#5878A8"));
            Label(pause, "II", 44, Color.white);
            var level = Region(header, "LevelSlot", new Vector2(.30f, .28f), new Vector2(.70f, .75f));
            Label(level, "Level 1", 64, Hex("#5A381A"));
            var speed = Card(header, "SpeedSlot", new Vector2(.78f, .22f), new Vector2(.97f, .86f), Hex("#C2BEAF"));
            Label(speed, ">> 2x", 38, Color.white);
            Region(foreground, "GameplayArea", new Vector2(.06f, .39f), new Vector2(.94f, .52f));
            var footer = Region(foreground, "FooterArea", new Vector2(0, 0), new Vector2(1, .045f));
            Box(footer, "BottomBar", Vector2.zero, Vector2.one, Hex("#6882AE"), false);
            Box(footer, "TopLine", new Vector2(0, .96f), Vector2.one, Hex("#AAB8D0"), false);
            string[] locks = { "Lv.3", "Lv.6", "Lv.9" };
            for (int i = 0; i < 3; i++)
            {
                float x = .175f + i * .25f;
                var lockOuter = Box(footer, $"LockSlot {i + 1}", new Vector2(x, -.15f),
                    new Vector2(x + .15f, 1.72f), Hex("#41598C"));
                var lockInner = InsetBox(lockOuter, "LockFace", 4, Hex("#7D94C2"));
                Label(lockInner, locks[i], 38, Hex("#FDFDFD"));
            }
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            GameplayDemoSetup.BuildIntoScene(view, camera);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("FIXED_PORTRAIT_CREATED: unscaled map, baked camera, background and foreground canvases.");
        }

        private static RectTransform NewCanvas(string name, RenderMode mode)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Canvas)).GetComponent<RectTransform>();
            rect.GetComponent<Canvas>().renderMode = mode;
            return rect;
        }

        private static RectTransform Region(RectTransform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform Box(RectTransform parent, string name, Vector2 min, Vector2 max, Color color, bool round = true)
        {
            var rect = Region(parent, name, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (round)
            {
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
            }
            return rect;
        }

        private static RectTransform InsetBox(RectTransform parent, string name, float inset, Color color)
        {
            var rect = Box(parent, name, Vector2.zero, Vector2.one, color);
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
            return rect;
        }

        private static RectTransform Card(RectTransform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var shadow = Box(parent, name + " Shadow", min - new Vector2(0, .025f), max - new Vector2(0, .025f), Hex("#A38969"));
            var outer = Box(parent, name, min, max, Hex("#FFF4DA"));
            var face = InsetBox(outer, "Face", 6, color);
            return face;
        }

        private static void Label(RectTransform parent, string value, int size, Color color)
        {
            var rect = Region(parent, "Label", Vector2.zero, Vector2.one);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.2f, .13f, .1f, .55f);
            outline.effectDistance = new Vector2(2, -2);
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var color);
            return color;
        }

        private static Sprite CreateRoundedSprite()
        {
            Directory.CreateDirectory(ArtPath);
            string path = ArtPath + "/RoundedPanel.png";
            const int size = 128;
            const float radius = 30;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x - .5f, x + .5f - (size - radius), 0);
                    float dy = Mathf.Max(radius - y - .5f, y + .5f - (size - radius), 0);
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + .5f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = new Vector4(32, 32, 32, 32);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
