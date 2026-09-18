using System;
using System.Reflection;
using ColonyFlow.Gameplay;
using UnityEngine;

public static class BoosterSmokeChecks
{
    public static void RunSlots(MapView map, AntGameplay game, Action<string, bool> check)
    {
        game.Restart();
        var method = typeof(AntGameplay).GetMethod("AddSlot");
        check("Add Slot API exists", method != null);
        if (method == null) return;
        int count = game.Slots.Count;
        var anchors = Points(game, "boxAnchors");
        var firstPosition = anchors[0].position;
        game.TogglePause();
        check("paused level rejects Add Slot", !(bool)method.Invoke(game, null));
        game.TogglePause();
        game.PickQueue(0);
        var existing = game.Slots[0];
        check("Add Slot creates one extra slot", (bool)method.Invoke(game, null) && game.Slots.Count == count + 1);
        check("Add Slot can only be used once", !(bool)method.Invoke(game, null) && game.Slots.Count == count + 1);
        var fields = new[] { "boxAnchors", "antSpawnPoints", "perimeterEntries" };
        foreach (var name in fields)
            check("slot indexed layout expands atomically: " + name, ((Transform[])typeof(AntGameplay).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game)).Length == count + 1);
        check("Add Slot preserves occupied indices and landing gate", game.Slots[0] == existing && existing.IsLanding && game.ActiveTripCount == 0);
        var surfaces = (Renderer[])typeof(AntGameplay).GetField("slotSurfaces", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        check("slot surfaces remain aligned with anchors", Array.TrueForAll(Array.ConvertAll(new[] { 0, 1, 2, 3 }, i => Mathf.Abs(surfaces[i].transform.position.x - Points(game, "boxAnchors")[i].position.x)), distance => distance < .001f));
        game.Restart();
        check("restart restores original anchor positions", Points(game, "boxAnchors")[0].position == firstPosition);
        check("restart restores original slot count and booster use", game.Slots.Count == count && (bool)method.Invoke(game, null));
        var data = Data(new[] { 1 }, new[] { 2, 2, 2, 2, 1 });
        check("Add Slot deadlock fixture loads", map.LoadJson(JsonUtility.ToJson(data)) && game.Initialize());
        for (int i = 0; i < count; i++) game.PickQueue(0);
        game.Advance(.4f);
        check("full unmatched slots report deadlock", game.IsDeadlocked);
        check("Add Slot unlocks a full deadlocked row", (bool)method.Invoke(game, null) && !game.IsDeadlocked && game.PickQueue(0));
        var fifth = game.Slots[count];
        game.Advance(.4f);
        check("fifth slot dispatches through its own entry", game.ActiveTripCount == 1 && game.ActiveAnts[0].Source == fifth);
        game.Restart();
    }

    private static Transform[] Points(AntGameplay game, string name) =>
        (Transform[])typeof(AntGameplay).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);

    public static void RunPickup(MapView map, AntGameplay game, Action<string, bool> check)
    {
        var pick = typeof(AntGameplay).GetMethod("PickupBox");
        check("Pickup API exists", pick != null);
        if (pick == null) return;
        game.Restart();
        var queues = (System.Collections.Generic.List<System.Collections.Generic.List<BoxActor>>)typeof(AntGameplay).GetField("queues", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        var first = queues[0][0]; var middle = queues[0][1]; var back = queues[0][2]; var hiddenRow = queues[0][3];
        check("Pickup selection starts and cancels", game.BeginPickupSelection() && game.IsSelectingPickup);
        game.CancelBoosterSelection();
        check("Pickup cancellation exits selection", !game.IsSelectingPickup);
        check("Pickup rejects nonvisible queue row", !(bool)pick.Invoke(game, new object[] { hiddenRow }));
        game.TogglePause();
        check("Pickup rejects pause", !(bool)pick.Invoke(game, new object[] { middle }));
        game.TogglePause();
        check("Pickup acquires middle box by identity", (bool)pick.Invoke(game, new object[] { middle }) && game.Slots[0] == middle && queues[0][0] == first && !queues[0].Contains(middle));
        check("Pickup landing blocks dispatch", middle.IsLanding && game.ActiveTripCount == 0);
        check("Pickup rejects box already in slot", !(bool)pick.Invoke(game, new object[] { middle }));
        check("Pickup acquires visible back then front", (bool)pick.Invoke(game, new object[] { back }) && (bool)pick.Invoke(game, new object[] { first }));
        check("Pickup fourth box fills last slot", (bool)pick.Invoke(game, new object[] { queues[0][0] }));
        check("Pickup rejects full slots", !(bool)pick.Invoke(game, new object[] { queues[0][0] }));
        check("Pickup selection rejects full slots", !game.BeginPickupSelection());
        var json = JsonUtility.ToJson(Data(new[] { 1 }, new[] { 1, 1, 1 }));
        foreach (int kind in new[] { 1, 2 })
        {
            map.LoadJson(json.Replace("\"kind\":0", "\"kind\":" + kind)); game.Initialize();
            queues = (System.Collections.Generic.List<System.Collections.Generic.List<BoxActor>>)typeof(AntGameplay).GetField("queues", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            check("Pickup excludes box kind " + kind, !(bool)pick.Invoke(game, new object[] { queues[0][1] }));
        }
        check("loader rejects unknown box kind", MapJsonLoader.Load(json.Replace("\"kind\":0", "\"kind\":3")) == null);
        game.Restart();
        queues = (System.Collections.Generic.List<System.Collections.Generic.List<BoxActor>>)typeof(AntGameplay).GetField("queues", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        check("pooled box kind resets for ordinary JSON", (bool)pick.Invoke(game, new object[] { queues[0][1] }));
        game.Restart();
        queues = (System.Collections.Generic.List<System.Collections.Generic.List<BoxActor>>)typeof(AntGameplay).GetField("queues", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        middle = queues[0][1];
        var camera = (Camera)typeof(AntGameplay).GetField("gameplayCamera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        Physics.SyncTransforms();
        var pointer = camera.WorldToScreenPoint(middle.HitCollider.bounds.center);
        check("Pickup selection routes pointer to middle box", game.BeginPickupSelection() && game.HandlePointer(pointer) && game.Slots[0] == middle && !game.IsSelectingPickup);
        game.BeginPickupSelection(); game.Restart();
        check("restart cancels Pickup selection", !game.IsSelectingPickup);
    }

    private static MapJsonData Data(int[] cells, int[] boxColors) => new MapJsonData
    {
        rows = 1, columns = cells.Length, cells = cells,
        cameraPosition = new MapVectorData { y = 25, z = -14.5f },
        cameraRotation = new MapVectorData { x = 60 }, firstCellPosition = new MapVectorData(),
        cellSpacing = new MapSpacingData { x = 1.1f, z = 1.1f },
        palette = new[] { new MapPaletteEntry { id = 1, hex = "#FF0000" }, new MapPaletteEntry { id = 2, hex = "#00FF00" } },
        queues = new[] { new BoxQueueData { boxes = Array.ConvertAll(boxColors, id => new BoxData { colorId = id, antCount = 1 }) } }
    };
}
