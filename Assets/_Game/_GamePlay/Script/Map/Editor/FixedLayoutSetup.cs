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
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.orthographicSize = 42;
            camera.transform.position = new Vector3(0, 80, -15.12f);
            camera.nearClipPlane = .3f;
            camera.farClipPlane = 300;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#FFD18E");
            camera.ResetAspect();
            view.Clear();
            rounded = CreateRoundedSprite();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Native world-space Canvas lies behind the cells, with fixed transform and size.
            var background = NewCanvas("Background Canvas", RenderMode.WorldSpace);
            background.sizeDelta = new Vector2(1080, 1920);
            background.position = camera.transform.position + camera.transform.forward * 160;
            background.rotation = camera.transform.rotation;
            background.localScale = Vector3.one * (2 * camera.orthographicSize / 1920);
            Box(background, "Orange Background", Vector2.zero, Vector2.one, Hex("#FFD18E"), false);
            for (int i = 0; i < 18; i++)
            {
                float x = ((i * 37) % 100) / 100f;
                float y = ((i * 23 + 7) % 100) / 100f;
                var shape = Box(background, $"Background Pattern {i + 1}",
                    new Vector2(x, y), new Vector2(x + .17f, y + .075f), new Color(1, .92f, .7f, .18f), false);
                shape.localRotation = Quaternion.Euler(0, 0, 38);
            }
            Box(background, "Map Card Shadow", new Vector2(.035f, .491f), new Vector2(.965f, .854f), Hex("#DEAF6E"));
            var card = Box(background, "Map Card Frame", new Vector2(.035f, .50f), new Vector2(.965f, .862f), Hex("#EBC68B"));
            InsetBox(card, "Map Card Background", 22, Hex("#F5E9D5"));

            var foreground = NewCanvas("Fixed Gameplay Canvas", RenderMode.ScreenSpaceOverlay);
            var scaler = foreground.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0;
            Region(foreground, "MapArea", new Vector2(.06f, .52f), new Vector2(.94f, .84f));
            var header = Region(foreground, "Header", new Vector2(0, .875f), Vector2.one);
            var pause = Card(header, "PauseSlot", new Vector2(.025f, .33f), new Vector2(.10f, .67f), Hex("#657EA6"));
            Label(pause, "II", 45, Color.white);
            var badge = Card(header, "BadgeSlot", new Vector2(.17f, .26f), new Vector2(.29f, .76f), Hex("#225B30"));
            Label(badge, "X", 53, Hex("#9CCD4B"));
            var level = Region(header, "LevelSlot", new Vector2(.34f, .28f), new Vector2(.70f, .75f));
            Label(level, "Level 17", 60, Hex("#593716"));
            var speed = Card(header, "SpeedSlot", new Vector2(.78f, .33f), new Vector2(.97f, .67f), Hex("#BDB9A6"));
            Label(speed, ">> 2x", 39, Color.white);
            Region(foreground, "GameplayArea", new Vector2(.06f, .39f), new Vector2(.94f, .52f));
            var footer = Box(foreground, "BottomBar", Vector2.zero, new Vector2(1, .108f), Hex("#6582B9"), false);
            Box(footer, "Top Line", new Vector2(0, .64f), new Vector2(1, .66f), Hex("#B4B6C2"), false);
            string[] symbols = { "+", "II", "/" };
            for (int i = 0; i < 3; i++)
            {
                float x = .16f + i * .29f;
                var outer = Box(foreground, $"BoosterSlot {i + 1}", new Vector2(x, .065f),
                    new Vector2(x + .15f, .133f), Hex("#385996"));
                var inner = InsetBox(outer, "Gold Rim", 9, Hex("#D7AD67"));
                inner = InsetBox(inner, "Face", 9, Hex("#9FD2EE"));
                Label(inner, symbols[i], 72, Hex("#5BA72B"));
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
