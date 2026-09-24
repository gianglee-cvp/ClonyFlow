using UnityEditor;
using UnityEngine;

namespace ColonyFlow.Gameplay.Editor
{
    [InitializeOnLoad]
    public static class ColorBoxPrefabUpgrader
    {
        private const string PrefabPath = "Assets/_Game/_GamePlay/Prefabs/ColorBox.prefab";
        private const string MaterialFolder = "Assets/_Game/_GamePlay/Materials/";

        static ColorBoxPrefabUpgrader()
        {
            EditorApplication.delayCall += UpgradeIfNeeded;
        }

        [MenuItem("ColonyFlow/Map/Upgrade ColorBox 3D Visual")]
        public static void UpgradeFromMenu()
        {
            Upgrade(true);
        }

        private static void UpgradeIfNeeded()
        {
            Upgrade(false);
        }

        private static void Upgrade(bool force)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return;
            OutlineMaterial();
            if (!force && prefab.transform.Find("Body") != null &&
                prefab.transform.Find("Highlight Rim") != null && prefab.transform.Find("Lid") != null &&
                prefab.transform.Find("White Outline") != null) return;

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null) return;
            try
            {
                var actor = root.GetComponent<BoxActor>();
                Transform lid = root.transform.Find("Lid") ?? root.transform.Find("Face");
                if (actor == null || lid == null || lid.GetComponent<Renderer>() == null)
                {
                    Debug.LogError("Cannot upgrade ColorBox: BoxActor or Lid mesh is missing.", root);
                    return;
                }

                RemoveLayer(root.transform, "Body");
                RemoveLayer(root.transform, "Colored Side");
                RemoveLayer(root.transform, "Highlight Rim");
                RemoveLayer(root.transform, "Border");
                RemoveLayer(root.transform, "Body Outline");
                RemoveLayer(root.transform, "Lid Outline");
                RemoveLayer(root.transform, "White Outline");

                lid.name = "Lid";
                Configure(lid, new Vector3(0f, .32f, 0f), new Vector3(1.3820624f, .22f, 1.511f));
                var body = CloneLayer(lid, root.transform, "Body",
                    new Vector3(0f, .025f, 0f), new Vector3(1.2504375f, .374f, 1.3670932f));
                var rim = CloneLayer(lid, root.transform, "Highlight Rim",
                    new Vector3(0f, .20f, 0f), new Vector3(1.3689f, .08f, 1.4966073f));
                var outline = CloneLayer(lid, root.transform, "White Outline",
                    new Vector3(0f, .071f, .057f), new Vector3(1.465f, .655f, 1.602f));

                var lidMaterial = PreviewMaterial("ColorBoxLidPreview", new Color(1f, .847f, .239f), .52f);
                var bodyMaterial = PreviewMaterial("ColorBoxBodyPreview", new Color(.953f, .663f, .11f), .42f);
                var rimMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "Gameplay-FFF5DCFF.mat");
                var outlineMaterial = OutlineMaterial();
                lid.GetComponent<Renderer>().sharedMaterial = lidMaterial;
                body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
                rim.GetComponent<Renderer>().sharedMaterial = rimMaterial;
                outline.GetComponent<Renderer>().sharedMaterial = outlineMaterial;

                var serialized = new SerializedObject(actor);
                serialized.FindProperty("face").objectReferenceValue = lid.GetComponent<Renderer>();
                serialized.FindProperty("border").objectReferenceValue = rim.GetComponent<Renderer>();
                serialized.FindProperty("side").objectReferenceValue = body.GetComponent<Renderer>();
                serialized.FindProperty("outlineBackdrop").objectReferenceValue = outline.GetComponent<Renderer>();
                var renderers = serialized.FindProperty("bodyRenderers");
                renderers.arraySize = 4;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = outline.GetComponent<Renderer>();
                renderers.GetArrayElementAtIndex(1).objectReferenceValue = body.GetComponent<Renderer>();
                renderers.GetArrayElementAtIndex(2).objectReferenceValue = rim.GetComponent<Renderer>();
                renderers.GetArrayElementAtIndex(3).objectReferenceValue = lid.GetComponent<Renderer>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (root.TryGetComponent(out BoxCollider collider))
                {
                    collider.center = new Vector3(0f, .1275f, 0f);
                    collider.size = new Vector3(1.3820624f, .605f, 1.511f);
                }

                outline.SetSiblingIndex(0);
                body.SetSiblingIndex(1);
                rim.SetSiblingIndex(2);
                lid.SetSiblingIndex(3);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("ColorBox prefab upgraded with queue-front 3D outline structure.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform CloneLayer(Transform source, Transform parent, string name,
            Vector3 position, Vector3 scale)
        {
            Transform clone = Object.Instantiate(source.gameObject, parent).transform;
            clone.name = name;
            Configure(clone, position, scale);
            return clone;
        }

        private static void Configure(Transform target, Vector3 position, Vector3 scale)
        {
            target.localPosition = position;
            target.localRotation = Quaternion.identity;
            target.localScale = scale;
        }

        private static void RemoveLayer(Transform root, string name)
        {
            Transform layer = root.Find(name);
            if (layer != null) Object.DestroyImmediate(layer.gameObject);
        }

        private static Material PreviewMaterial(string name, Color color, float smoothness)
        {
            string path = MaterialFolder + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var template = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "Gameplay-FFFFFFFF.mat");
            var material = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material OutlineMaterial()
        {
            const string name = "ColorBoxOutline";
            string path = MaterialFolder + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.SetFloat("_Cull", 2f);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
                return existing;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { name = name, enableInstancing = true };
            Color cream = new Color(1f, .965f, .82f, 1f);
            material.SetColor("_BaseColor", cream);
            material.SetColor("_Color", cream);
            material.SetFloat("_Cull", 2f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_ZWrite", 1f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
