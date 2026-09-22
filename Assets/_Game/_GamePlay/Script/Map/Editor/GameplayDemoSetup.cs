using System.Collections.Generic;
using System.IO;
using UnityEditor;
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
            BuildIntoScene(map, camera, GameplayLayoutSettings.LoadOrCreate());
        }

        public static void BuildIntoScene(MapView map, Camera camera, GameplayLayoutSettings settings)
        {
            if (!PrepareMaterials()) return;
            SceneObjects.RemoveRoots("GameplayRoot", "ReserveSlots", "ColorButtonsArea", "Hole Rim",
                "Map Card Frame", "Map Card Shadow");
            CalculateBoxLayout(camera);
            var box = CreateBoxPrefab(camera);
            var ant = CreateAntPrefab();
            var root = new GameObject("GameplayRoot").transform;
            var surface = CreateCard(root, camera, settings);
            var bounds = MapPerimeter.CardBounds(map.Root, surface);
            CreateSlots(root, camera, bounds, settings, out var anchors, out var spawns, out var entries, out var surfaces);
            var queues = CreateQueueAnchors(root, camera, settings);
            CreateHole(root, camera, bounds, settings, out var returning, out var jumping, out var exit);
            var gameplay = root.gameObject.AddComponent<AntGameplay>();
            gameplay.Configure(map, camera, box, ant, anchors, spawns, entries, queues, returning, jumping, exit);
            gameplay.ConfigureCard(surface);
            gameplay.ConfigureSlotSurfaces(surfaces);
            var colorMaterial = TintedMaterial(Color.white, true);
            gameplay.ConfigureColorMaterial(colorMaterial);
            gameplay.ConfigureLayoutUnit(LayoutUnit);
            gameplay.ConfigureBoxLayout(queueColumnStep, queueRowStep);
            map.ConfigureCellSurface(surface);
            map.ConfigureColorMaterial(colorMaterial);
        }

        private static bool PrepareMaterials()
        {
            Directory.CreateDirectory(BasePath + "Prefabs");
            Directory.CreateDirectory(BasePath + "Materials");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return false;
            string path = BasePath + "Materials/GameplayUnlit.mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "GameplayUnlit" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return true;
        }

        private static void CalculateBoxLayout(Camera camera)
        {
            float width = 2 * camera.orthographicSize * 1080 / 1920;
            boxWidth = width * .13f;
            boxDepth = (boxWidth - .14f * Mathf.Abs(camera.transform.up.y)) / Mathf.Abs(camera.transform.up.z);
            queueColumnStep = width * .155f;
            queueRowStep = 2 * camera.orthographicSize * .073f / Mathf.Abs(camera.transform.up.z);
        }

        private static Renderer CreateCard(Transform parent, Camera camera, GameplayLayoutSettings settings)
        {
            var root = new GameObject("MapCard").transform;
            root.SetParent(parent, false);
            var bottom = ScreenWorld(camera, settings.cardViewportMin.x, settings.cardViewportMin.y);
            var top = ScreenWorld(camera, settings.cardViewportMax.x, settings.cardViewportMax.y);
            root.localPosition = (bottom + top) * .5f + Vector3.down * (.65f * LayoutUnit);
            var size = new Vector2(top.x - bottom.x, top.z - bottom.z);
            float inset = 44 * (2 * camera.orthographicSize / 1920);
            CreateCardLayer(root, "Shadow", size, new Vector3(0, -.06f * LayoutUnit, -.7f * LayoutUnit), Hex("#DEAF6E"));
            CreateCardLayer(root, "Frame", size, Vector3.zero, Hex("#EBC68B"));
            return CreateCardLayer(root, "Surface", size - Vector2.one * inset,
                new Vector3(0, .04f * LayoutUnit, 0), Hex("#F5E9D5"));
        }

        private static void CreateSlots(Transform parent, Camera camera, Bounds bounds, GameplayLayoutSettings settings,
            out Transform[] anchors, out Transform[] spawns, out Transform[] entries, out Renderer[] surfaces)
        {
            var root = new GameObject("ActiveSlots").transform;
            root.SetParent(parent, false);
            anchors = new Transform[4];
            spawns = new Transform[4];
            entries = new Transform[4];
            surfaces = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                var slot = new GameObject($"Slot {i}").transform;
                slot.SetParent(root, false);
                var position = ScreenWorld(camera,
                    settings.slotCenterViewport.x + (i - 1.5f) * settings.slotHorizontalSpacing,
                    settings.slotCenterViewport.y);
                anchors[i] = Point(slot, "BoxAnchor", position);
                spawns[i] = Point(slot, "AntSpawnPoint", position + Vector3.forward * (boxDepth * .5f));
                entries[i] = Point(slot, "PerimeterEntry",
                    new Vector3(Mathf.Clamp(position.x, bounds.min.x, bounds.max.x), 0, bounds.min.z));
                surfaces[i] = Primitive(slot, "Empty Slot", PrimitiveType.Cube, position + Vector3.down * (.35f * LayoutUnit),
                    new Vector3(boxWidth, .10f, boxDepth) / LayoutUnit, Hex("#FFF7E7"), false).GetComponent<Renderer>();
            }
        }

        private static Transform[] CreateQueueAnchors(Transform parent, Camera camera, GameplayLayoutSettings settings)
        {
            var root = new GameObject("BoxQueues").transform;
            root.SetParent(parent, false);
            var anchors = new Transform[3];
            for (int i = 0; i < anchors.Length; i++)
                anchors[i] = Point(root, $"Queue {i} Anchor", ScreenWorld(camera,
                    settings.queueCenterViewport.x + (i - 1) * settings.queueHorizontalSpacing,
                    settings.queueCenterViewport.y));
            return anchors;
        }

        private static void CreateHole(Transform parent, Camera camera, Bounds bounds, GameplayLayoutSettings settings,
            out Transform returning, out Transform jumping, out Transform exit)
        {
            var root = new GameObject("AntHole").transform;
            root.SetParent(parent, false);
            var position = ScreenWorld(camera, settings.holeViewport.x, settings.holeViewport.y);
            float width = 2 * camera.orthographicSize * 1080 / 1920;
            var rim = Primitive(root, "Hole Rim", PrimitiveType.Cylinder, position + Vector3.down * (.25f * LayoutUnit),
                new Vector3(width * .12f, .045f, width * .075f) / LayoutUnit, Hex("#B98243"), false, false);
            var rimCollider = rim.AddComponent<MeshCollider>();
            rimCollider.sharedMesh = rim.GetComponent<MeshFilter>().sharedMesh;
            Primitive(root, "Dark Hole", PrimitiveType.Cylinder, position + Vector3.down * (.1f * LayoutUnit),
                new Vector3(width * .105f, .05f, width * .063f) / LayoutUnit, Hex("#59341C"), false, false);
            returning = Point(root, "ReturnPoint", position);
            jumping = Point(root, "JumpTarget", position + Vector3.down * (2 * LayoutUnit));
            exit = Point(parent, "HoleExit", new Vector3(0, 0, bounds.min.z));
        }

        private static Renderer CreateCardLayer(Transform root, string name,
            Vector2 size, Vector3 position, Color color)
        {
            return Primitive(root, name, PrimitiveType.Cube, position,
                new Vector3(size.x / LayoutUnit, .12f, size.y / LayoutUnit), color, false, false).GetComponent<Renderer>();
        }
        private static BoxActor CreateBoxPrefab(Camera camera)
        {
            var root = new GameObject("ColorBox");
            var actor = root.AddComponent<BoxActor>();
            var border = Primitive(root.transform, "Border", PrimitiveType.Cube, Vector3.zero,
                new Vector3(boxWidth, .14f, boxDepth) / LayoutUnit, Hex("#FFF5DC"), false).GetComponent<Renderer>();
            var face = Primitive(root.transform, "Face", PrimitiveType.Cube, new Vector3(0, .115f / LayoutUnit, 0),
                new Vector3(boxWidth - .10f, .10f, boxDepth - .10f) / LayoutUnit, Color.white, false).GetComponent<Renderer>();
            var collider = CreateBoxCollider(root);
            var label = CreateCountLabel(root.transform, out var canvas);
            actor.Configure(label, face, canvas, collider, new[] { border, face });
            actor.AlignCount(camera);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BasePath + "Prefabs/ColorBox.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<BoxActor>();
        }

        private static BoxCollider CreateBoxCollider(GameObject root)
        {
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .10f, 0);
            collider.size = new Vector3(boxWidth, .35f, boxDepth);
            return collider;
        }

        private static Text CreateCountLabel(Transform parent, out RectTransform rect)
        {
            var owner = new GameObject("Count", typeof(RectTransform));
            owner.transform.SetParent(parent, false);
            owner.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            rect = (RectTransform)owner.transform;
            rect.sizeDelta = new Vector2(200, 200);
            rect.localScale = Vector3.one * (boxWidth * .9f / 200);
            rect.localPosition = new Vector3(0, .23f, 0);
            rect.localRotation = Quaternion.Euler(45, 0, 0);
            var labelOwner = new GameObject("Label", typeof(RectTransform));
            labelOwner.transform.SetParent(rect, false);
            var labelRect = (RectTransform)labelOwner.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = labelOwner.AddComponent<Text>();
            label.font = font;
            label.fontSize = 90;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = "30";
            var outline = labelOwner.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3, -3);
            return label;
        }

        private static AntActor CreateAntPrefab()
        {
            var previous = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath + "Prefabs/Ant.prefab");
            var root = new GameObject("Ant");
            root.transform.localScale = previous != null ? previous.transform.localScale : Vector3.one * 2;
            var actor = root.AddComponent<AntActor>();
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            Color dark = Hex("#302435");
            var bodyParts = new List<MeshFilter>
            {
                Primitive(visual, "Head", PrimitiveType.Sphere, new Vector3(0, .17f, .35f),
                    new Vector3(.42f, .3f, .42f), dark, false).GetComponent<MeshFilter>(),
                Primitive(visual, "Thorax", PrimitiveType.Sphere, new Vector3(0, .14f, 0),
                    new Vector3(.27f, .25f, .36f), dark, false).GetComponent<MeshFilter>()
            };
            var abdomen = Primitive(visual, "Abdomen", PrimitiveType.Sphere, new Vector3(0, .16f, -.4f),
                new Vector3(.47f, .3f, .65f), dark, false).GetComponent<Renderer>();
            for (int i = 0; i < 3; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var leg = Primitive(visual, $"Leg {i} {side}", PrimitiveType.Cube,
                        new Vector3(side * .27f, .05f, .2f - i * .2f), new Vector3(.48f, .055f, .06f), dark, false);
                    leg.transform.localRotation = Quaternion.Euler(0, side * (i - 1) * 25, 0);
                    bodyParts.Add(leg.GetComponent<MeshFilter>());
                }
            CreateCombinedAntBody(visual, bodyParts, TintedMaterial(dark, true));
            var brick = Primitive(visual, "Carried Brick", PrimitiveType.Cube, new Vector3(0, .55f, .24f),
                new Vector3(.7f, .24f, .7f), Color.white, false);
            actor.Configure(visual, brick.GetComponent<Renderer>(), abdomen);
            actor.ConfigureJumpHeight(1.4f * LayoutUnit);
            brick.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BasePath + "Prefabs/Ant.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<AntActor>();
        }

        private static void CreateCombinedAntBody(Transform parent, List<MeshFilter> parts, Material bodyMaterial)
        {
            var combine = new CombineInstance[parts.Count];
            for (int i = 0; i < parts.Count; i++)
            {
                combine[i].mesh = parts[i].sharedMesh;
                combine[i].transform = parent.worldToLocalMatrix * parts[i].transform.localToWorldMatrix;
            }
            var generated = new Mesh { name = "AntBody" };
            generated.CombineMeshes(combine, true, true, false);
            const string path = BasePath + "Meshes/AntBody.asset";
            Directory.CreateDirectory(BasePath + "Meshes");
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = generated;
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.CopySerialized(generated, mesh);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(mesh);
            }
            foreach (var part in parts) UnityEngine.Object.DestroyImmediate(part.gameObject);
            var body = new GameObject("Ant Body");
            body.transform.SetParent(parent, false);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            ConfigurePrimitiveRenderer(body.AddComponent<MeshRenderer>(), Color.white, true);
            body.GetComponent<MeshRenderer>().sharedMaterial = bodyMaterial;
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Color color, bool keepCollider, bool affectedByLight = true)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            if (parent.root.name == "ColorBox" || parent.root.name == "Ant") position *= LayoutUnit;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale * LayoutUnit;
            ConfigurePrimitiveRenderer(obj.GetComponent<Renderer>(), color, affectedByLight);
            if (type == PrimitiveType.Cube) obj.GetComponent<MeshFilter>().sharedMesh = RoundedCube();
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        private static void ConfigurePrimitiveRenderer(Renderer renderer, Color color, bool affectedByLight)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = UnityEngine.MotionVectorGenerationMode.ForceNoMotion;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.sharedMaterial = TintedMaterial(color, affectedByLight);
        }

        private static Material TintedMaterial(Color color, bool affectedByLight)
        {
            string path = BasePath + "Materials/Gameplay-" + ColorUtility.ToHtmlStringRGBA(color) + ".mat";
            var tinted = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (tinted == null)
            {
                tinted = new Material(material);
                AssetDatabase.CreateAsset(tinted, path);
            }
            tinted.shader = Shader.Find(affectedByLight ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit");
            tinted.enableInstancing = true;
            tinted.SetColor("_BaseColor", color);
            if (affectedByLight) ConfigureSoftLit(tinted);
            EditorUtility.SetDirty(tinted);
            return tinted;
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
            var origin = camera.transform.position + camera.transform.right * ((x - .5f) * 2 * size * 1080 / 1920)
                + camera.transform.up * ((y - .5f) * 2 * size);
            var ray = new Ray(origin, camera.transform.forward);
            new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance);
            return ray.GetPoint(distance);
        }
        public static CellView PrepareRoundedCell()
        {
            const string prefabPath = BasePath + "Prefabs/RoundedMapCell.prefab";
            var mesh = LoadRoundedMesh();
            var cellMaterial = AssetDatabase.LoadAssetAtPath<Material>(BasePath + "Materials/MapCell.mat");
            if (mesh == null || cellMaterial == null) return null;
            var root = new GameObject("RoundedMapCell");
            var view = root.AddComponent<CellView>();
            var visual = new GameObject("Rounded Cube");
            visual.transform.SetParent(root.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = cellMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = UnityEngine.MotionVectorGenerationMode.ForceNoMotion;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            var size = mesh.bounds.size;
            var scale = new Vector3(1 / size.x, 1 / size.y, 1 / size.z);
            visual.transform.localScale = scale;
            visual.transform.localPosition = -Vector3.Scale(mesh.bounds.center, scale);
            view.Configure(new[] { renderer });
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<CellView>();
        }

        private static Mesh LoadRoundedMesh()
        {
            const string path = BasePath + "Prefabs/rounded_cube.glb";
            AssetDatabase.ImportAsset(path);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Mesh mesh) return mesh;
            return null;
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
