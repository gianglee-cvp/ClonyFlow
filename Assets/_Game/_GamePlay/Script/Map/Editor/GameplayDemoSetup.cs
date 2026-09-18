using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay.Editor
{
    public static class GameplayDemoSetup
    {
        private const string BasePath = "Assets/_Game/_GamePlay/";
        private static Material material;
        private static Font font;

        public static void BuildIntoScene(MapView map, Camera camera)
        {
            var old = GameObject.Find("GameplayRoot");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            foreach (string name in new[] { "ReserveSlots", "ColorButtonsArea", "Hole Rim" })
            {
                var placeholder = GameObject.Find(name);
                if (placeholder != null) UnityEngine.Object.DestroyImmediate(placeholder);
            }
            Directory.CreateDirectory(BasePath + "Prefabs");
            Directory.CreateDirectory(BasePath + "Materials");
            string materialPath = BasePath + "Materials/GameplayUnlit.mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("URP Unlit shader is missing.");
                material = new Material(shader) { name = "GameplayUnlit" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var box = CreateBoxPrefab();
            var ant = CreateAntPrefab();
            var root = new GameObject("GameplayRoot").transform;
            Primitive(root, "Ground", PrimitiveType.Cube, new Vector3(0, -.35f, 0),
                new Vector3(21.9f, .2f, 21.9f), Hex("#F5E9D5"), false);
            var slotsRoot = new GameObject("ActiveSlots").transform; slotsRoot.SetParent(root, false);
            var queueRoot = new GameObject("BoxQueues").transform; queueRoot.SetParent(root, false);
            var cardRoot = new GameObject("MapCard").transform;
            cardRoot.SetParent(root, false);
            var bottomLeft = ScreenWorld(camera, .035f, .50f);
            var topRight = ScreenWorld(camera, .965f, .862f);
            cardRoot.localPosition = (bottomLeft + topRight) * .5f + Vector3.down * .65f;
            var cardSize = new Vector2(topRight.x - bottomLeft.x, topRight.z - bottomLeft.z);
            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(BasePath + "UI/RoundedPanel.png");
            CreateCardLayer(cardRoot, "Shadow", rounded, cardSize, new Vector3(0, -.06f, -.7f), Hex("#DEAF6E"));
            CreateCardLayer(cardRoot, "Frame", rounded, cardSize, Vector3.zero, Hex("#EBC68B"));
            float inset = 44 * (2 * camera.orthographicSize / 1920);
            var surface = CreateCardLayer(cardRoot, "Surface", rounded, cardSize - Vector2.one * inset,
                new Vector3(0, .04f, 0), Hex("#F5E9D5"));
            var cardBounds = MapPerimeter.CardBounds(map.Root, surface);
            foreach (string name in new[] { "Map Card Frame", "Map Card Shadow" })
            {
                var obsolete = GameObject.Find(name);
                if (obsolete != null) UnityEngine.Object.DestroyImmediate(obsolete);
            }
            var slotAnchors = new Transform[4];
            var entries = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var slot = new GameObject($"Slot {i}").transform; slot.SetParent(slotsRoot, false);
                var position = ScreenWorld(camera, .275f + i * .15f, .355f);
                slotAnchors[i] = Point(slot, "BoxAnchor", position);
                Point(slot, "AntSpawnPoint", position);
                entries[i] = Point(slot, "PerimeterEntry", new Vector3(Mathf.Clamp(position.x, cardBounds.min.x, cardBounds.max.x), 0, cardBounds.min.z));
                Primitive(slot, "Empty Slot", PrimitiveType.Cube, position + Vector3.down * .35f,
                    new Vector3(4.5f, .18f, 4.5f), Hex("#FFF7E7"), false);
            }
            var queueAnchors = new Transform[3];
            for (int i = 0; i < 3; i++)
                queueAnchors[i] = Point(queueRoot, $"Queue {i} Anchor", ScreenWorld(camera, .36f + i * .14f, .26f));
            var hole = new GameObject("AntHole").transform; hole.SetParent(root, false);
            var holePosition = ScreenWorld(camera, .5f, .455f);
            Primitive(hole, "Hole Rim", PrimitiveType.Cylinder, holePosition + Vector3.down * .25f,
                new Vector3(4.1f, .10f, 2.9f), Hex("#B98243"), false);
            Primitive(hole, "Dark Hole", PrimitiveType.Cylinder, holePosition + Vector3.down * .1f,
                new Vector3(3.35f, .11f, 2.1f), Hex("#59341C"), false);
            var returning = Point(hole, "ReturnPoint", holePosition);
            var jumping = Point(hole, "JumpTarget", holePosition + Vector3.down * 2);
            var exit = Point(root, "HoleExit", new Vector3(0, 0, cardBounds.min.z));
            var queuesJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/BoxQueues/demo-box-queues.json");
            if (queuesJson == null) throw new InvalidOperationException("Assign a Box queue JSON asset.");
            var gameplay = root.gameObject.AddComponent<AntGameplay>();
            gameplay.Configure(map, camera, box, ant, slotAnchors, entries, queueAnchors, returning, jumping, exit, null);
            gameplay.ConfigureCard(surface);
            gameplay.ConfigureQueues(queuesJson);
        }

        private static SpriteRenderer CreateCardLayer(Transform root, string name, Sprite sprite,
            Vector2 size, Vector3 position, Color color)
        {
            if (sprite == null) throw new InvalidOperationException("RoundedPanel sprite is missing.");
            var layer = new GameObject(name, typeof(SpriteRenderer));
            layer.transform.SetParent(root, false);
            layer.transform.localPosition = position;
            layer.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var renderer = layer.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.color = color;
            renderer.drawMode = SpriteDrawMode.Sliced; renderer.size = size;
            return renderer;
        }
        private static BoxActor CreateBoxPrefab()
        {
            var root = new GameObject("ColorBox");
            var actor = root.AddComponent<BoxActor>();
            var border = Primitive(root.transform, "Border", PrimitiveType.Cube, Vector3.zero,
                new Vector3(4.35f, .35f, 4.35f), Hex("#FFF5DC"), false);
            var face = Primitive(root.transform, "Face", PrimitiveType.Cube, new Vector3(0, .22f, 0),
                new Vector3(3.95f, .2f, 3.95f), Color.white, false);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .2f, 0); collider.size = new Vector3(4.35f, .8f, 4.35f);
            var canvas = new GameObject("Count", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(root.transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 200); rect.localScale = Vector3.one * .02f;
            rect.localPosition = new Vector3(0, .38f, 0); rect.localRotation = Quaternion.Euler(90, 0, 0);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(canvas.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            label.font = font; label.fontSize = 100; label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
            label.text = "30";
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black; outline.effectDistance = new Vector2(3, -3);
            actor.Configure(label, face.GetComponent<Renderer>());
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BasePath + "Prefabs/ColorBox.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<BoxActor>();
        }

        private static AntActor CreateAntPrefab()
        {
            var root = new GameObject("Ant");
            var actor = root.AddComponent<AntActor>();
            Color dark = Hex("#302435");
            Primitive(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0, .17f, .35f), new Vector3(.42f, .3f, .42f), dark, false);
            Primitive(root.transform, "Thorax", PrimitiveType.Sphere, new Vector3(0, .14f, 0), new Vector3(.27f, .25f, .36f), dark, false);
            var abdomen = Primitive(root.transform, "Abdomen", PrimitiveType.Sphere, new Vector3(0, .16f, -.4f),
                new Vector3(.47f, .3f, .65f), dark, false).GetComponent<Renderer>();
            for (int i = 0; i < 3; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var leg = Primitive(root.transform, $"Leg {i} {side}", PrimitiveType.Cube,
                        new Vector3(side * .27f, .05f, .2f - i * .2f), new Vector3(.48f, .055f, .06f), dark, false);
                    leg.transform.localRotation = Quaternion.Euler(0, side * (i - 1) * 25, 0);
                }
            var brick = Primitive(root.transform, "Carried Brick", PrimitiveType.Cube, new Vector3(0, .55f, .24f),
                new Vector3(.7f, .24f, .7f), Color.white, false);
            actor.Configure(brick, abdomen);
            brick.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BasePath + "Prefabs/Ant.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<AntActor>();
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Color color, bool keepCollider)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            var renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Persist the authoring color in a material asset; property blocks are not serialized.
            string path = BasePath + "Materials/Gameplay-" + ColorUtility.ToHtmlStringRGBA(color) + ".mat";
            var tinted = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (tinted == null)
            {
                tinted = new Material(material); tinted.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(tinted, path);
            }
            renderer.sharedMaterial = tinted;
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        private static Transform Point(Transform parent, string name, Vector3 world)
        {
            var point = new GameObject(name).transform;
            point.SetParent(parent, false); point.position = world; return point;
        }
        private static Vector3 ScreenWorld(Camera camera, float x, float y)
        {
            // Bake against portrait aspect regardless of the Editor Game View's current size.
            float size = camera.orthographicSize;
            var point = camera.transform.position + camera.transform.right * ((x - .5f) * 2 * size * 1080 / 1920)
                + camera.transform.up * ((y - .5f) * 2 * size) + camera.transform.forward * 80;
            point.y = 0; return point;
        }
        private static Color Hex(string html) { ColorUtility.TryParseHtmlString(html, out var color); return color; }
    }
}
