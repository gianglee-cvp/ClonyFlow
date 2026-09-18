using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonyFlow.Gameplay.Editor
{
    internal static class SceneObjects
    {
        public static GameObject FindRoot(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        public static void RemoveRoots(params string[] names)
        {
            foreach (var name in names)
            {
                var root = FindRoot(name);
                if (root != null) Object.DestroyImmediate(root);
            }
        }
    }
}
