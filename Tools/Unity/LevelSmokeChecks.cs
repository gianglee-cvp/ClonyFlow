using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ColonyFlow.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DG.Tweening;

public static class LevelSmokeChecks
{
    private static int checks, failures;
    private static MapView map;
    private static AntGameplay game;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/_Game/_GamePlay/Scenes/MapDemo.unity");
        var savedMap = Object.FindFirstObjectByType<MapView>();
        var savedGame = Object.FindFirstObjectByType<AntGameplay>();
        bool savedSlotWorks = savedMap != null && savedGame != null && savedMap.LoadMap() && savedGame.Initialize() && savedGame.AddSlot() && savedGame.Slots.Count == 5;
        SessionState.SetBool("LevelSmoke.SavedSlot", savedSlotWorks);
        if (savedGame != null) savedGame.Restart();
        if (savedGame != null) savedGame.ConfigureSlotSurfaces(null);
        var migration = typeof(ColonyFlow.Gameplay.Editor.FixedLayoutSetup).Assembly.GetType("ColonyFlow.Gameplay.Editor.SlotSurfaceMigration");
        migration?.GetMethod("RepairOpenScenes").Invoke(null, null);
        SessionState.SetBool("LevelSmoke.SurfaceMigration", savedGame != null && savedGame.CanAddSlot && savedGame.AddSlot());
        ColonyFlow.Gameplay.Editor.FixedLayoutSetup.Apply();
        var editorMap = Object.FindFirstObjectByType<MapView>();
        var editorGame = Object.FindFirstObjectByType<AntGameplay>();
        SessionState.SetBool("LevelSmoke.EditorReferences", editorMap.LoadMap() && editorGame.Initialize());
        SessionState.SetBool("LevelSmoke.Pending", true);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void RegisterPlayModeChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("LevelSmoke.Pending", false)) return;
            SessionState.SetBool("LevelSmoke.Pending", false);
            EditorApplication.update += RunOnce;
        };
    }

    private static void RunOnce()
    {
        EditorApplication.update -= RunOnce;
        RunChecks();
    }

    private static void RunChecks()
    {
        PoolingSmokeChecks.Run(Check);
        map = Object.FindFirstObjectByType<MapView>();
        game = Object.FindFirstObjectByType<AntGameplay>();
        Check("scene components", map != null && game != null);
        Check("saved scene adds fifth slot without Editor rebuild", SessionState.GetBool("LevelSmoke.SavedSlot", false));
        Check("open scene missing new surface field repairs Add Slot", SessionState.GetBool("LevelSmoke.SurfaceMigration", false));
        Check("Editor rebuild writes complete references", SessionState.GetBool("LevelSmoke.EditorReferences", false));
        if (map == null || game == null) { Finish(); return; }
        var data = JsonUtility.FromJson<MapJsonData>(File.ReadAllText("Assets/_Game/Data/Maps/map1.json"));
        Check("optional cellScale uses default scale", MapJsonLoader.Load(File.ReadAllText("Assets/_Game/Data/Maps/map1.json")) != null);
        bool loaded = map.LoadMap();
        Check("serialized CellView prefab and renderer references", loaded && map.SpawnedCellCount == map.Model.ColoredCellCount);
        if (!loaded) { Finish(); return; }
        bool initialized = game.Initialize();
        Check("serialized box and ant references", initialized);
        if (!initialized) { Finish(); return; }
        Check("combined queues preserve authored order", QueueOrderMatches());
        Check("out-of-grid access returns null", map.Model.GetCell(-1, 0) == null);
        Check("invalid dimensions return null", MapJsonLoader.Load("{\"rows\":0,\"columns\":0}") == null);
        game.PickQueue(0);
        game.Advance(.01f);
        TestFailedInitialization();
        TestExhaustedBudget();
        game.Restart();
        TestBentRoute();
        BoosterSmokeChecks.RunSlots(map, game, Check);
        TestSessionReload();
        BoosterSmokeChecks.RunPickup(map, game, Check);
        BoosterSmokeChecks.RunBlow(map, game, Check);
        BoosterSmokeChecks.RunSelection(map, game, Check);
        TestQueueAndCentralReturn();
        TestNearestAndPointer();
        TestLevels();
        TestPoolGameplayReuse();
        TestFailedPickupBudget();
        TestRestart();
        Finish();
    }

    private static void Check(string name, bool pass)
    {
        checks++;
        if (pass) Debug.Log("PASS: " + name);
        else { failures++; Debug.LogError("FAIL: " + name); }
    }

    private static bool QueueOrderMatches()
    {
        var queues = Queues();
        if (queues.Count != map.Model.Queues.Count) return false;
        for (int q = 0; q < queues.Count; q++)
        {
            var boxes = map.Model.Queues[q].boxes;
            if (queues[q].Count != boxes.Length) return false;
            for (int b = 0; b < boxes.Length; b++)
                if (queues[q][b].ColorId != boxes[b].colorId || queues[q][b].AntCount != boxes[b].antCount) return false;
        }
        return true;
    }

    private static List<List<BoxActor>> Queues() =>
        (List<List<BoxActor>>)typeof(AntGameplay).GetField("queues", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);

    private static string Json(int rows, int columns, int[] cells, int count)
    {
        var data = new MapJsonData
        {
            rows = rows, columns = columns, cells = cells,
            cameraPosition = new MapVectorData { y = 25, z = -14.5f },
            cameraRotation = new MapVectorData { x = 60 },
            firstCellPosition = new MapVectorData(),
            cellSpacing = new MapSpacingData { x = 1.1f, z = 1.1f },
            palette = new[] { new MapPaletteEntry { id = 1, hex = "#FF000080" } },
            queues = new[] { new BoxQueueData { boxes = new[] { new BoxData { colorId = 1, antCount = count } } } }
        };
        return JsonUtility.ToJson(data);
    }

    private static void TestBentRoute()
    {
        var cells = new int[25];
        for (int i = 0; i < cells.Length; i++) cells[i] = 1;
        cells[21] = cells[16] = cells[17] = 0;
        var model = MapJsonLoader.Load(Json(5, 5, cells, 22));
        var nav = new GridNavigation(model);
        var owner = new GameObject("Navigation Test Root");
        var perimeter = new MapPerimeter(owner.transform, model, new Bounds(new Vector3(2.2f, 0, -2.2f), new Vector3(8, 1, 8)));
        var entry = perimeter.BottomEntry(0);
        var candidate = nav.EvaluateFrom(model.GetCell(2, 2), perimeter, entry, .2f);
        var route = nav.BuildInteriorRoute(candidate);
        Check("bent route traverses only adjacent empty cells", route.Count == 3 && ValidRoute(route));
        Check("cached search preserves route cost", Mathf.Abs(nav.EvaluateFrom(model.GetCell(2, 2), perimeter, entry, .2f).TravelDistance - candidate.TravelDistance) < .0001f);
        model.TryCollect(model.GetCell(4, 2));
        nav.Rebuild();
        candidate = nav.EvaluateFrom(model.GetCell(2, 2), perimeter, entry, .2f);
        Check("collection exposes shorter straight route", nav.BuildInteriorRoute(candidate).Count == 2 && nav.NavigationWeight(model.GetCell(4, 2)) == 1);
        Object.DestroyImmediate(owner);
    }

    private static bool ValidRoute(List<Cell> route)
    {
        for (int i = 0; i < route.Count; i++)
        {
            if (!route[i].IsEmpty) return false;
            if (i > 0 && Mathf.Abs(route[i].Row - route[i - 1].Row) + Mathf.Abs(route[i].Column - route[i - 1].Column) != 1) return false;
        }
        return true;
    }

    private static void TestQueueAndCentralReturn()
    {
        game.Restart();
        var next = Queues()[0][1];
        var before = next.transform.position;
        game.PickQueue(0);
        Check("queue does not snap after picking front box", next.transform.position == before);
        game.Advance(.05f);
        var midway = next.transform.position;
        Check("queue advances progressively", midway.z > before.z + .001f && midway.z < before.z + new SerializedObject(game).FindProperty("queueRowStep").floatValue - .001f);
        game.TogglePause();
        game.Advance(100);
        Check("pause freezes queue shift", next.transform.position == midway);
        game.TogglePause();
        game.Advance(.5f);
        Check("queue shift reaches next row", Mathf.Abs(next.transform.position.z - before.z - new SerializedObject(game).FindProperty("queueRowStep").floatValue) < .001f);
        var cell = map.Model.GetCell(map.Model.Rows - 1, map.Model.Columns / 2);
        var surface = (Renderer)new SerializedObject(game).FindProperty("mapCardSurface").objectReferenceValue;
        var perimeter = new MapPerimeter(map.Root, map.Model, MapPerimeter.CardBounds(map.Root, surface));
        var candidate = game.Navigation.EvaluateFrom(cell, perimeter, perimeter.BottomEntry(cell.Position.x));
        var route = (List<Vector3>)typeof(AntGameplay).GetMethod("BuildReturn", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(game,
            new object[] { candidate, game.Navigation.BuildInteriorRoute(candidate), perimeter.CellPoint(cell) });
        Check("central ants approach hole diagonally", Mathf.Abs(route[route.Count - 2].x - route[route.Count - 1].x) > .01f);
        var hole = ((Transform)new SerializedObject(game).FindProperty("holeReturn").objectReferenceValue).position;
        float centerX = map.Root.InverseTransformPoint(hole).x;
        foreach (float x in new[] { centerX - 2, centerX, centerX + 2 })
        {
            var approach = (Vector3)typeof(AntGameplay).GetMethod("HoleApproachPoint", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(game, new object[] { perimeter.BottomEntry(x) });
            Check("left center right entries all use diagonal hole approach", Mathf.Abs(approach.x - hole.x) > .01f && Mathf.Abs(approach.z - hole.z) > .01f);
        }
    }

    private static void TestNearestAndPointer()
    {
        Check("synthetic combined level loads", map.LoadJson(Json(3, 3, new[] { 1, 0, 0, 0, 0, 0, 1, 0, 0 }, 2)) && game.Initialize());
        var box = Queues()[0][0];
        var camera = (Camera)new SerializedObject(game).FindProperty("gameplayCamera").objectReferenceValue;
        Physics.SyncTransforms();
        var point = camera.WorldToScreenPoint(box.HitCollider.bounds.center);
        Check("registered collider picks queue box", game.HandlePointer(point));
        game.Advance(.01f);
        Check("box remains in flight before dispatch", game.ActiveTripCount == 0 && Vector3.Distance(box.transform.position, SlotPosition(box.SlotIndex)) > .001f);
        var boxPosition = box.transform.position;
        game.TogglePause();
        game.Advance(100);
        Check("pause freezes box jump", box.transform.position == boxPosition);
        game.TogglePause();
        game.Advance(.18f);
        Check("x2 advances box jump to landing", !box.IsLanding && Vector3.Distance(box.transform.position, SlotPosition(box.SlotIndex)) < .001f);
        Check("dispatch chooses near bottom cell before top row", game.ActiveTripCount == 1 && game.ActiveAnts[0].Target == map.Model.GetCell(2, 0));
        if (game.ActiveTripCount == 0) return;
        var returning = (List<Vector3>)typeof(AntActor).GetField("returnRoute", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game.ActiveAnts[0]);
        var gameData = new SerializedObject(game);
        var hole = ((Transform)gameData.FindProperty("holeReturn").objectReferenceValue).position;
        var turn = returning[returning.Count - 2];
        hole.y = turn.y;
        float radius = gameData.FindProperty("holeApproachDistance").floatValue * gameData.FindProperty("layoutUnit").floatValue;
        var stand = returning[returning.Count - 1];
        Check("ant leaves card diagonally at fixed hole distance", Mathf.Abs(Vector3.Distance(turn, hole) - radius) < .001f && Mathf.Abs(turn.x - hole.x) > .001f);
        Check("ant stops outside hole before jumping", Vector3.Distance(stand, hole) > .5f && Mathf.Abs(stand.x - hole.x) > .01f && Mathf.Abs(stand.z - hole.z) > .01f);
        var position = game.ActiveAnts[0].transform.position;
        game.TogglePause();
        game.Advance(100);
        Check("pause freezes trip", game.ActiveAnts[0].transform.position == position);
        game.TogglePause();
        var surface = (Renderer)new SerializedObject(game).FindProperty("mapCardSurface").objectReferenceValue;
        var scale = surface.transform.localScale;
        surface.transform.localScale *= 1.1f;
        game.Advance(.001f);
        var entries = new SerializedObject(game).FindProperty("perimeterEntries");
        var entry = (Transform)entries.GetArrayElementAtIndex(0).objectReferenceValue;
        var bounds = MapPerimeter.CardBounds(map.Root, surface);
        Check("resizing updates card entry", Mathf.Abs(map.Root.InverseTransformPoint(entry.position).z - bounds.min.z) < .0001f);
        surface.transform.localScale = scale;
        TestAntJump();
        Check("pickup, return and jump complete", PlayToCompletion(2000));
    }

    private static Vector3 SlotPosition(int slot) =>
        ((Transform)new SerializedObject(game).FindProperty("boxAnchors").GetArrayElementAtIndex(slot).objectReferenceValue).position;

    private static void TestAntJump()
    {
        var ant = game.ActiveAnts[0];
        var scale = ant.transform.localScale;
        for (int i = 0; i < 3000 && ant.State != AntTripState.Jumping; i++) game.Advance(.01f);
        Check("ant reaches hole jump", ant.State == AntTripState.Jumping);
        var jumpStart = ant.transform.position;
        game.Advance(.1f);
        Check("ant jump travels diagonally into hole", Mathf.Abs(ant.transform.position.x - jumpStart.x) > .01f && Mathf.Abs(ant.transform.position.z - jumpStart.z) > .01f);
        Check("hole jump enlarges ant first", ant.State == AntTripState.Jumping && ant.transform.localScale.sqrMagnitude > scale.sqrMagnitude);
        var enlargedScale = ant.transform.localScale;
        game.Advance(.1f);
        Check("hole jump then shrinks ant progressively", ant.State == AntTripState.Jumping && ant.transform.localScale.sqrMagnitude < enlargedScale.sqrMagnitude && ant.transform.localScale.sqrMagnitude > 0);
        var position = ant.transform.position;
        var shrinkingScale = ant.transform.localScale;
        game.TogglePause();
        game.Advance(100);
        Check("pause freezes hole jump and scale", ant.transform.position == position && ant.transform.localScale == shrinkingScale);
        game.TogglePause();
    }

    private static bool PlayToCompletion(int ticks)
    {
        for (int tick = 0; tick < ticks && !game.IsLevelComplete; tick++)
        {
            var queues = Queues();
            for (int q = 0; q < queues.Count; q++)
            {
                if (queues[q].Count > 0 && HasReachableColor(queues[q][0].ColorId)) game.PickQueue(q);
            }
            game.Advance(.2f);
        }
        return game.IsLevelComplete && game.RemainingCellCount == 0 && game.ActiveTripCount == 0;
    }

    private static bool HasReachableColor(int color)
    {
        foreach (var cell in map.Model.EnumerateCells())
            if (cell.ColorId == color && game.Navigation.CanReach(cell)) return true;
        return false;
    }

    private static void TestLevels()
    {
        foreach (var path in Directory.GetFiles("Assets/_Game/Data/Maps", "*.json"))
        {
            Check("load complete level " + Path.GetFileName(path), map.LoadJson(File.ReadAllText(path)) && game.Initialize());
            Check("complete level " + Path.GetFileName(path), PlayToCompletion(30000));
            if (!game.IsLevelComplete) Debug.Log("INCOMPLETE: " + Path.GetFileName(path) + " remaining=" + game.RemainingCellCount + " active=" + game.ActiveTripCount + " deadlocked=" + game.IsDeadlocked);
        }
    }

    private static void TestRestart()
    {
        game.Restart();
        Check("restart reloads configured combined level", game.RemainingCellCount == 676 && game.ActiveTripCount == 0 && QueueOrderMatches());
        game.PickQueue(0);
        game.Advance(.01f);
        int landingTweens = DOTween.TotalActiveTweens();
        game.Restart();
        Check("restart clears in-flight tasks", game.RemainingCellCount == 676 && game.ActiveTripCount == 0 && game.PickQueue(0));
        Check("restart cancels previous actor tweens", landingTweens == DOTween.TotalActiveTweens());
    }

    private static void TestPoolGameplayReuse()
    {
        game.Restart();
        var viewsField = typeof(MapView).GetField("cellViews", BindingFlags.NonPublic | BindingFlags.Instance);
        var views = (Dictionary<Cell, CellView>)viewsField.GetValue(map);
        var ids = new HashSet<int>();
        foreach (var view in views.Values) ids.Add(view.GetInstanceID());
        int cellCreated = 0, actorCreated = 0;
        var cellPool = (ColonyFlow.Core.Pooling.ObjectPoolManager)typeof(MapView).GetField("pool", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(map);
        var actorPool = (ColonyFlow.Core.Pooling.ObjectPoolManager)typeof(AntGameplay).GetField("pool", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);
        System.Action<GameObject> onCell = _ => cellCreated++;
        System.Action<GameObject> onActor = _ => actorCreated++;
        cellPool.Instantiated += onCell;
        actorPool.Instantiated += onActor;
        game.Restart(); game.Restart();
        bool reused = true;
        foreach (var view in ((Dictionary<Cell, CellView>)viewsField.GetValue(map)).Values) reused &= ids.Contains(view.GetInstanceID());
        Check("restart reuses cell identities and warm actor pools", reused && cellCreated == 0 && actorCreated == 0);
        cellPool.Instantiated -= onCell;
        actorPool.Instantiated -= onActor;
    }

    private static void TestFailedPickupBudget()
    {
        Check("single-box pooled level initializes", map.LoadJson(Json(1, 1, new[] { 1 }, 1)) && game.Initialize());
        var box = Queues()[0][0];
        var position = map.GetCellVisualPosition(map.Model.GetCell(0, 0));
        game.PickQueue(0); game.Advance(.4f);
        Check("last-budget box waits for source pickup instead of recycle", game.ActiveTripCount == 1 && box.AntCount == 0 && box.OutgoingCount == 1 && game.ActiveAnts[0].Source == box);
        var ant = game.ActiveAnts[0];
        typeof(AntGameplay).GetMethod("CancelPickup", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(game, new object[] { ant });
        Check("failed pickup refunds the original box lease", box.AntCount == 1 && box.OutgoingCount == 0 && game.RemainingCellCount == 1);
        game.Advance(.01f);
        bool restored = false;
        foreach (var slot in game.Slots) restored |= slot == box;
        Check("failed pickup box returns to slot without losing budget", restored && box.AntCount == 1);
        Check("refunded pickup level still completes", PlayToCompletion(2000));
        Check("collected cell preserves visual snapshot after recycle", map.GetCellVisualPosition(map.Model.GetCell(0, 0)) == position);
    }

    private static void TestFailedInitialization()
    {
        var data = JsonUtility.FromJson<MapJsonData>(Json(1, 1, new[] { 1 }, 1));
        data.queues = new[] { data.queues[0], data.queues[0], data.queues[0], data.queues[0] };
        Check("replacement level loads at map boundary", map.LoadJson(JsonUtility.ToJson(data)));
        Check("failed initialization clears stale tasks and queue picks", !game.Initialize() && game.ActiveTripCount == 0 && !game.PickQueue(0));
    }

    private static void TestExhaustedBudget()
    {
        var data = JsonUtility.FromJson<MapJsonData>(Json(1, 1, new[] { 1 }, 1));
        data.queues = new BoxQueueData[0];
        map.LoadJson(JsonUtility.ToJson(data)); game.Initialize(); game.Advance(.01f);
        Check("exhausted supply reports deadlock with empty slots", game.IsDeadlocked && !game.IsLevelComplete);
    }

    private static void TestSessionReload()
    {
        game.Restart(); game.AddSlot(); game.PickQueue(0); game.Advance(.01f);
        game.enabled = false;
        Check("disable clears runtime session before script reload", game.Navigation == null && game.Slots == null && !game.HasAddedSlot && game.ActiveTripCount == 0);
        map.enabled = false;
        Check("disable clears map before script reload", map.Model == null && map.SpawnedCellCount == 0);
        map.enabled = true; game.enabled = true;
        Check("enable rebuilds usable four-slot session", game.Navigation != null && game.Slots != null && game.Slots.Count == 4 && game.RemainingCellCount == 676 && game.AddSlot());
        game.Restart();
        game.enabled = false; map.enabled = false; game.enabled = true;
        typeof(AntGameplay).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
        game.Advance(.01f);
        Check("game waits for disabled map during reverse enable order", map.Model == null && game.Navigation == null && !game.CanAddSlot);
        map.enabled = true; game.Advance(.01f);
        Check("reverse enable order restores Add Slot", game.Navigation != null && game.Slots.Count == 4 && game.AddSlot());
        game.Restart(); game.BeginBlowSelection(); map.enabled = false; game.Advance(.01f);
        Check("map disable clears stale palette and ordinary input", !game.IsSelectingBlow && game.AvailableBlowColors.Count == 0 && !game.PickQueue(0) && !game.HandlePointer(Vector2.zero));
        // Invalid-session GUI must return before touching IMGUI or the cleared Model.
        typeof(AntGameplay).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
        Check("disabled map rejects booster selection", map.Model == null && !game.CanAddSlot && !game.BeginPickupSelection() && !game.BeginBlowSelection());
        map.enabled = true; game.Advance(.01f);
        Check("map reenable restores usable session", game.Navigation.Map == map.Model && game.Slots.Count == 4 && game.AddSlot());
        game.Restart();
        map.LoadJson(Json(1, 1, new[] { 1 }, 1)); game.Initialize(); game.BlowColor(1);
        map.enabled = false; map.enabled = true; game.Advance(.01f);
        Check("map replacement after WIN resets completion and Add Slot", !game.IsLevelComplete && game.RemainingCellCount == 676 && game.Navigation.Map == map.Model && game.AddSlot());
        game.Restart();
    }

    private static void Finish()
    {
        File.WriteAllText("../smoke-results.txt", checks + " checks, " + failures + " failures\n");
        Debug.Log("LEVEL_SMOKE_RESULT: " + checks + " checks, " + failures + " failures");
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }
}
