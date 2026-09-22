using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColonyFlow.Gameplay.Editor
{
    public static class ProceduralAntModelBuilder
    {
        private const string PrefabPath = "Assets/_Game/_GamePlay/Prefabs/AntModelPreview.prefab";
        private const string MaterialRoot = "Assets/_Game/_GamePlay/Materials/AntPreview";

        [MenuItem("ColonyFlow/Ant/Build Model Preview")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(MaterialRoot);

            Material body = Material("Body", new Color(1f, .58f, .025f));
            Material belly = Material("Belly", new Color(.92f, .38f, .015f));
            Material dark = Material("Dark", new Color(.12f, .055f, .025f));
            Material white = Material("EyeWhite", new Color(1f, .98f, .9f));
            Material blue = Material("EyeBlue", new Color(.025f, .32f, .72f));
            Material shine = Material("EyeShine", Color.white);

            var root = new GameObject("AntModelPreview");
            var visual = Child(root.transform, "Visual");

            Sphere(visual, "Abdomen", new Vector3(0, .42f, -.27f), new Vector3(.48f, .44f, .62f), belly);
            Sphere(visual, "Thorax", new Vector3(0, .43f, .16f), new Vector3(.36f, .38f, .42f), body);
            Sphere(visual, "Head", new Vector3(0, .59f, .52f), new Vector3(.57f, .54f, .52f), body);

            CreateFace(visual, white, blue, shine, dark);
            CreateAntenna(visual, -1, body, dark);
            CreateAntenna(visual, 1, body, dark);
            CreateLegPair(visual, "Front", .30f, .24f, body, dark);
            CreateLegPair(visual, "Middle", .08f, .02f, body, dark);
            CreateLegPair(visual, "Back", -.14f, -.20f, body, dark);

            var carryPoint = Child(visual, "CarryPoint");
            carryPoint.localPosition = new Vector3(0, .92f, .43f);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }
        private static void CreateFace(Transform parent, Material white, Material blue, Material shine, Material dark)
        {
            foreach (float side in new[] { -.145f, .145f })
            {
                Sphere(parent, side < 0 ? "Eye L" : "Eye R", new Vector3(side, .66f, .735f),
                    new Vector3(.205f, .235f, .09f), white);
                Sphere(parent, side < 0 ? "Pupil L" : "Pupil R", new Vector3(side, .66f, .783f),
                    new Vector3(.115f, .15f, .045f), blue);
                Sphere(parent, side < 0 ? "Eye Shine L" : "Eye Shine R", new Vector3(side - .025f, .705f, .81f),
                    Vector3.one * .035f, shine);
            }
            Sphere(parent, "Smile", new Vector3(0, .515f, .785f), new Vector3(.12f, .035f, .025f), dark);
            var mandibleL = Sphere(parent, "Mandible L", new Vector3(-.065f, .49f, .82f),
                new Vector3(.105f, .038f, .07f), dark);
            var mandibleR = Sphere(parent, "Mandible R", new Vector3(.065f, .49f, .82f),
                new Vector3(.105f, .038f, .07f), dark);
            mandibleL.transform.localRotation = Quaternion.Euler(0, 0, -12f);
            mandibleR.transform.localRotation = Quaternion.Euler(0, 0, 12f);
        }

        private static void CreateAntenna(Transform parent, int side, Material body, Material dark)
        {
            var root = Child(parent, side < 0 ? "Antenna Root L" : "Antenna Root R");
            root.localPosition = new Vector3(side * .16f, .82f, .55f);
            Vector3 bend = new Vector3(side * .28f, 1.03f, .59f);
            Vector3 tip = new Vector3(side * .34f, 1.16f, .68f);
            Capsule(root, "Antenna Lower", Vector3.zero, bend - root.localPosition, .035f, body);
            Capsule(root, "Antenna Upper", bend - root.localPosition, tip - root.localPosition, .03f, body);
            Sphere(root, "Antenna Tip", tip - root.localPosition, Vector3.one * .09f, dark);
        }

        private static void CreateLegPair(Transform parent, string name, float rootZ, float footZ,
            Material body, Material dark)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var root = Child(parent, $"Leg Root {name} {(side < 0 ? "L" : "R")}");
                root.localPosition = new Vector3(side * .13f, .40f, rootZ);
                Vector3 knee = new Vector3(side * .22f, -.13f, .02f);
                Vector3 foot = new Vector3(side * .34f, -.34f, footZ - rootZ);
                Capsule(root, "Upper", Vector3.zero, knee, .04f, body);
                Capsule(root, "Lower", knee, foot, .032f, body);
                Sphere(root, "Foot", foot, new Vector3(.08f, .045f, .10f), dark);
            }
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static GameObject Sphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ConfigurePrimitive(value, parent, name, position, scale, material);
            return value;
        }

        private static GameObject Capsule(Transform parent, string name, Vector3 from, Vector3 to,
            float radius, Material material)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Vector3 delta = to - from;
            ConfigurePrimitive(value, parent, name, (from + to) * .5f,
                new Vector3(radius * 2, delta.magnitude * .5f, radius * 2), material);
            value.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            return value;
        }

        private static void ConfigurePrimitive(GameObject value, Transform parent, string name,
            Vector3 position, Vector3 scale, Material material)
        {
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localScale = scale;
            var collider = value.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            var renderer = value.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static Material Material(string name, Color color)
        {
            string path = MaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}