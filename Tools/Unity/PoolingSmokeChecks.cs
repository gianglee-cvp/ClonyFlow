using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PoolingSmokeChecks
{
    public static void Run(Action<string, bool> check)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ColonyFlow.Core.Pooling.ObjectPoolManager")).FirstOrDefault(t => t != null);
        check("Core Pooling assembly is available", type != null);
        if (type == null) return;
        var owner = new GameObject("Pool checks");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/_GamePlay/Prefabs/Ant.prefab");
        var manager = Activator.CreateInstance(type, new object[] { owner.transform });
        int created = 0;
        type.GetEvent("Instantiated").AddEventHandler(manager, new Action<GameObject>(_ => created++));
        var load = type.GetMethods().Single(m => m.Name == "Load" && !m.IsGenericMethod);
        var spawn = type.GetMethods().Single(m => m.Name == "Spawn" && !m.IsGenericMethod);
        var recycle = type.GetMethod("Recycle", new[] { typeof(GameObject) });
        load.Invoke(manager, new object[] { prefab, 3 });
        load.Invoke(manager, new object[] { prefab, 3 });
        check("prewarm is a minimum and repeated load creates no extras", created == 3);
        Func<GameObject> acquire = () => (GameObject)spawn.Invoke(manager, new object[] { prefab, null, null, owner.transform, false });
        var a = acquire(); var b = acquire(); var c = acquire();
        a.transform.localScale = Vector3.zero;
        check("valid recycle returns true and deactivates instance", (bool)recycle.Invoke(manager, new object[] { a }) && !a.activeSelf);
        check("duplicate recycle returns false", !(bool)recycle.Invoke(manager, new object[] { a }));
        check("foreign recycle returns false", !(bool)recycle.Invoke(manager, new object[] { owner }));
        var reused = acquire();
        check("pool reuses identity and restores scale", reused == a && reused.transform.localScale == prefab.transform.localScale && created == 3);
        type.GetMethod("Cleanup", new[] { typeof(GameObject), typeof(int) }).Invoke(manager, new object[] { prefab, 0 });
        check("cleanup preserves borrowed instances", reused != null && b != null && c != null && reused.activeSelf);
        ((IDisposable)manager).Dispose();
        check("dispose deactivates all borrowed instances", !reused.activeSelf && !b.activeSelf && !c.activeSelf);
        check("disposed manager cannot spawn", acquire() == null);
        ((IDisposable)manager).Dispose();
        UnityEngine.Object.Destroy(owner);
    }
}
