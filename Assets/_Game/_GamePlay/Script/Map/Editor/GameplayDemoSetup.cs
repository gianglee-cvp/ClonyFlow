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
        private const float LayoutUnit = 9f / 42f;
        private static float boxWidth, boxDepth, queueColumnStep, queueRowStep;
        private static Material material;
        private static Font font;
        private static Mesh roundedMesh;

        public static void BuildIntoScene(MapView map, Camera camera)
        {
            var current = UnityEngine.Object.FindFirstObjectByType<AntGameplay>();
            var queuesJson = current != null ? (TextAsset)new SerializedObject(current).FindProperty("boxQueuesJson").objectReferenceValue : null;
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
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
                material = new Material(shader) { name = "GameplayUnlit" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            EditorUtility.SetDirty(material);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float screenWidth = 2 * camera.orthographicSize * 1080 / 1920;
            boxWidth = screenWidth * .13f;
            boxDepth = (boxWidth - .14f * Mathf.Abs(camera.transform.up.y)) /
                Mathf.Abs(camera.transform.up.z);
            queueColumnStep = screenWidth * .155f;
            queueRowStep = 2 * camera.orthographicSize * .073f / Mathf.Abs(camera.transform.up.z);
            var box = CreateBoxPrefab();
            var ant = CreateAntPrefab();
            var root = new GameObject("GameplayRoot").transform;
            var slotsRoot = new GameObject("ActiveSlots").transform; slotsRoot.SetParent(root, false);
            var queueRoot = new GameObject("BoxQueues").transform; queueRoot.SetParent(root, false);
            var cardRoot = new GameObject("MapCard").transform;
            cardRoot.SetParent(root, false);
            var bottomLeft = ScreenWorld(camera, .04f, .515f);
            var topRight = ScreenWorld(camera, .96f, .885f);
            cardRoot.localPosition = (bottomLeft + topRight) * .5f + Vector3.down * (.65f * LayoutUnit);
            var cardSize = new Vector2(topRight.x - bottomLeft.x, topRight.z - bottomLeft.z);
            float inset = 44 * (2 * camera.orthographicSize / 1920);
            CreateCardLayer(cardRoot, "Shadow", cardSize, new Vector3(0, -.06f * LayoutUnit, -.7f * LayoutUnit), Hex("#DEAF6E"));
            CreateCardLayer(cardRoot, "Frame", cardSize, Vector3.zero, Hex("#EBC68B"));
            var surface = CreateCardLayer(cardRoot, "Surface", cardSize - Vector2.one * inset,
                new Vector3(0, .04f * LayoutUnit, 0), Hex("#F5E9D5"));
            var cardBounds = MapPerimeter.CardBounds(map.Root, surface);
            foreach (string name in new[] { "Map Card Frame", "Map Card Shadow" })
            {
                var obsolete = GameObject.Find(name);
                if (obsolete != null) UnityEngine.Object.DestroyImmediate(obsolete);
            }
            float holeY = .445f;
            float slotY = .355f;
            float queueY = .263f;
            var slotAnchors = new Transform[4];
            var spawnPoints = new Transform[4];
            var entries = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var slot = new GameObject($"Slot {i}").transform; slot.SetParent(slotsRoot, false);
                var position = ScreenWorld(camera, .5f + (i - 1.5f) * .1525f, slotY);

                slotAnchors[i] = Point(slot, "BoxAnchor", position);
                spawnPoints[i] = Point(slot, "AntSpawnPoint", position + Vector3.forward * (boxDepth * .5f));
                entries[i] = Point(slot, "PerimeterEntry", new Vector3(Mathf.Clamp(position.x, cardBounds.min.x, cardBounds.max.x), 0, cardBounds.min.z));
                Primitive(slot, "Empty Slot", PrimitiveType.Cube, position + Vector3.down * (.35f * LayoutUnit),
                    new Vector3(boxWidth, .10f, boxDepth) / LayoutUnit, Hex("#FFF7E7"), false);
            }
            var queueAnchors = new Transform[3];
            for (int i = 0; i < 3; i++)
                queueAnchors[i] = Point(queueRoot, $"Queue {i} Anchor", ScreenWorld(camera, .36f + i * .14f, queueY));
            var hole = new GameObject("AntHole").transform; hole.SetParent(root, false);
            var holePosition = ScreenWorld(camera, .5f, holeY);

            Primitive(hole, "Hole Rim", PrimitiveType.Cylinder, holePosition + Vector3.down * (.25f * LayoutUnit),
                new Vector3(screenWidth * .12f, .045f, screenWidth * .075f) / LayoutUnit, Hex("#B98243"), false, false);
            Primitive(hole, "Dark Hole", PrimitiveType.Cylinder, holePosition + Vector3.down * (.1f * LayoutUnit),
                new Vector3(screenWidth * .105f, .05f, screenWidth * .063f) / LayoutUnit, Hex("#59341C"), false, false);
            var returning = Point(hole, "ReturnPoint", holePosition);
            var jumping = Point(hole, "JumpTarget", holePosition + Vector3.down * (2 * LayoutUnit));
            var exit = Point(root, "HoleExit", new Vector3(0, 0, cardBounds.min.z));
            if (queuesJson == null) queuesJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/BoxQueues/demo-box-queues.json");
            if (queuesJson == null) throw new InvalidOperationException("Assign a Box queue JSON asset.");
            var gameplay = root.gameObject.AddComponent<AntGameplay>();
            gameplay.Configure(map, camera, box, ant, slotAnchors, entries, queueAnchors, returning, jumping, exit, null);
            gameplay.SetAntSpawnPoints(spawnPoints);
            gameplay.ConfigureCard(surface);
            map.ConfigureCellSurface(surface);
            gameplay.ConfigureQueues(queuesJson);
            gameplay.ConfigureLayoutUnit(LayoutUnit);
            gameplay.ConfigureBoxLayout(queueColumnStep, queueRowStep, boxDepth * .5f);
        }

        private static Renderer CreateCardLayer(Transform root, string name,
            Vector2 size, Vector3 position, Color color)
        {
            return Primitive(root, name, PrimitiveType.Cube, position,
                new Vector3(size.x / LayoutUnit, .12f, size.y / LayoutUnit), color, false, false).GetComponent<Renderer>();
        }
        private static BoxActor CreateBoxPrefab()
        {
            var root = new GameObject("ColorBox");
            var actor = root.AddComponent<BoxActor>();
            var border = Primitive(root.transform, "Border", PrimitiveType.Cube, Vector3.zero,
                new Vector3(boxWidth, .14f, boxDepth) / LayoutUnit, Hex("#FFF5DC"), false);
            var face = Primitive(root.transform, "Face", PrimitiveType.Cube, new Vector3(0, .115f / LayoutUnit, 0),
                new Vector3(boxWidth - .10f, .10f, boxDepth - .10f) / LayoutUnit, Color.white, false);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .10f, 0); collider.size = new Vector3(boxWidth, .35f, boxDepth);
            var canvas = new GameObject("Count", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(root.transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 200); rect.localScale = Vector3.one * (boxWidth * .9f / 200);
            rect.localPosition = new Vector3(0, .23f, 0); rect.localRotation = Quaternion.Euler(45, 0, 0);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(canvas.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            label.font = font; label.fontSize = 90; label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
            label.text = "30";
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black; outline.effectDistance = new Vector2(3, -3);
            actor.Configure(label, face.GetComponent<Renderer>());
            actor.AlignCount(Camera.main);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BasePath + "Prefabs/ColorBox.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<BoxActor>();
        }

        private static AntActor CreateAntPrefab()
        {
            var previous = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath + "Prefabs/Ant.prefab");
            var root = new GameObject("Ant");
            root.transform.localScale = previous != null ? previous.transform.localScale : Vector3.one * 2;
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
            actor.ConfigureJumpHeight(1.4f * LayoutUnit);
            brick.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BasePath + "Prefabs/Ant.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<AntActor>();
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Color color, bool keepCollider, bool affectedByLight = true)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name;
            scale *= LayoutUnit;
            if (parent.root.name == "ColorBox" || parent.root.name == "Ant") position *= LayoutUnit;
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
            tinted.shader = Shader.Find(affectedByLight ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit"); tinted.SetColor("_BaseColor", color);
            if (affectedByLight) ConfigureSoftLit(tinted);
            EditorUtility.SetDirty(tinted);
            renderer.sharedMaterial = tinted;
            if (type == PrimitiveType.Cube) obj.GetComponent<MeshFilter>().sharedMesh = RoundedCube();
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        private static Transform Point(Transform parent, string name, Vector3 world)
        {
            var point = new GameObject(name).transform;
            point.SetParent(parent, false); point.position = world; return point;
        }
        private static float PortraitViewportY(Camera camera, Vector3 point) =>
            .5f + Vector3.Dot(point - camera.transform.position, camera.transform.up) / (2 * camera.orthographicSize);
        private static Vector3 ScreenWorld(Camera camera, float x, float y)
        {
            // Bake against portrait aspect regardless of the Editor Game View's current size.
            float size = camera.orthographicSize;
            var origin = camera.transform.position + camera.transform.right * ((x - .5f) * 2 * size * 1080 / 1920)
                + camera.transform.up * ((y - .5f) * 2 * size);
            var ray = new Ray(origin, camera.transform.forward);
            new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance);
            return ray.GetPoint(distance);
        }
        public static GameObject PrepareRoundedCell()
        {
            const string modelPath = BasePath + "Prefabs/rounded_cube.glb";
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("Cannot import rounded_cube.glb as a model.");
            var root = new GameObject("RoundedMapCell");
            try
            {
                root.AddComponent<CellView>();
                var visual = UnityEngine.Object.Instantiate(model, root.transform, false);
                visual.name = "Rounded Cube";
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("Rounded cube has no renderer.");
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0)
                    throw new InvalidOperationException("Rounded cube has invalid bounds.");
                var scale = new Vector3(1 / bounds.size.x, 1 / bounds.size.y, 1 / bounds.size.z);
                visual.transform.localScale = Vector3.Scale(visual.transform.localScale, scale);
                visual.transform.localPosition = -Vector3.Scale(bounds.center, scale);
                var cellMaterial = AssetDatabase.LoadAssetAtPath<Material>(BasePath + "Materials/MapCell.mat");
                cellMaterial.shader = Shader.Find("Universal Render Pipeline/Lit");
                ConfigureSoftLit(cellMaterial);
                EditorUtility.SetDirty(cellMaterial);
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = cellMaterial;
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, BasePath + "Prefabs/RoundedMapCell.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void ConfigureSoftLit(Material target)
        {
            target.SetFloat("_Metallic", 0);
            target.SetFloat("_Smoothness", .35f);
            target.SetFloat("_ReceiveShadows", 0);
            target.EnableKeyword("_RECEIVE_SHADOWS_OFF");
        }
        private static Mesh RoundedCube()
        {
            const string path = BasePath + "Meshes/RoundedCube.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (roundedMesh != null) return roundedMesh;
            Directory.CreateDirectory(BasePath + "Meshes");
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            var vertexNormals = new List<Vector3>();
            float[] axis = { -.5f, -.497f, -.488f, -.468f, -.44f, 0, .44f, .468f, .488f, .497f, .5f };
            var normals = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var normal in normals)
            {
                var u = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.up;
                var v = Vector3.Cross(normal, u);
                int offset = vertices.Count;
                int segments = axis.Length - 1;
                for (int y = 0; y <= segments; y++)
                    for (int x = 0; x <= segments; x++)
                    {
                        var p = normal * .5f + u * axis[x] + v * axis[y];
                        var core = new Vector3(Mathf.Clamp(p.x, -.44f, .44f), Mathf.Clamp(p.y, -.44f, .44f), Mathf.Clamp(p.z, -.44f, .44f));
                        var n = (p - core).normalized;
                        vertices.Add(core + n * .06f);
                        vertexNormals.Add(n);
                    }
                for (int y = 0; y < segments; y++)
                    for (int x = 0; x < segments; x++)
                    {
                        int a = offset + y * (segments + 1) + x, b = a + 1, c = a + segments + 1, d = c + 1;
                        triangles.Add(a); triangles.Add(b); triangles.Add(c);
                        triangles.Add(b); triangles.Add(d); triangles.Add(c);
                    }
            }
            bool created = mesh == null;
            if (created) mesh = new Mesh { name = "RoundedCube" }; else mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetNormals(vertexNormals); mesh.RecalculateBounds();
            if (created) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            roundedMesh = mesh;
            return mesh;
        }
        private static Color Hex(string html) { ColorUtility.TryParseHtmlString(html, out var color); return color; }
    }
}
