using UnityEditor;
using UnityEngine;

namespace ColonyFlow.Gameplay.Editor
{
    // Upgrade already-open scenes after the booster fields are added to the script.
    public static class SlotSurfaceMigration
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.delayCall += RepairOpenScenes;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
                    RepairOpenScenes();
            };
        }

        public static void RepairOpenScenes()
        {
            var games = Object.FindObjectsByType<AntGameplay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var surfaces = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var game in games)
            {
                RepairSurfaces(game, surfaces);
                RemoveLegacyFooter(game, rects);
            }
        }

        private static void RepairSurfaces(AntGameplay game, Renderer[] candidates)
        {
            var serialized = new SerializedObject(game);
            var anchors = serialized.FindProperty("boxAnchors");
            var surfaces = serialized.FindProperty("slotSurfaces");
            if (anchors.arraySize < 2 || HasValidSurfaces(surfaces, anchors.arraySize)) return;
            var recovered = new Renderer[anchors.arraySize];
            for (int i = 0; i < recovered.Length; i++)
            {
                var anchor = anchors.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (anchor == null) return;
                foreach (var candidate in candidates)
                {
                    if (candidate.gameObject.scene != game.gameObject.scene || candidate.name != "Empty Slot" ||
                        candidate.transform.parent != anchor.parent) continue;
                    recovered[i] = candidate;
                    break;
                }
                if (recovered[i] == null) return;
            }
            surfaces.arraySize = recovered.Length;
            for (int i = 0; i < recovered.Length; i++) surfaces.GetArrayElementAtIndex(i).objectReferenceValue = recovered[i];
            serialized.ApplyModifiedProperties();
        }

        private static bool HasValidSurfaces(SerializedProperty surfaces, int count)
        {
            if (surfaces.arraySize != count) return false;
            for (int i = 0; i < count; i++)
                if (surfaces.GetArrayElementAtIndex(i).objectReferenceValue == null) return false;
            return true;
        }

        private static void RemoveLegacyFooter(AntGameplay game, RectTransform[] rects)
        {
            foreach (var rect in rects)
            {
                if (rect == null || rect.gameObject.scene != game.gameObject.scene || rect.parent == null ||
                    rect.parent.name != "FooterArea" ||
                    (rect.name != "LockSlot 1" && rect.name != "LockSlot 2" && rect.name != "LockSlot 3")) continue;
                rect.gameObject.SetActive(false);
                if (Application.isPlaying) Object.Destroy(rect.gameObject);
                else Undo.DestroyObjectImmediate(rect.gameObject);
            }
        }
    }
}
