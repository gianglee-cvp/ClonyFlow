# Pickup Booster Focus Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a Pickup-booster focus mode that darkens the scene and ordinary HUD while rendering only valid pickup boxes, their counts, and the Cancel prompt at full brightness.

**Architecture:** `AntGameplay` owns one reusable pickup-eligibility predicate and maintains a small static focus set on `BoxActor`. The existing URP `QueueOutlineFeature` composites a fullscreen dim layer and redraws focused box renderers without moving their transforms. `CanvasGamePlay` dims a dedicated ordinary-UI group, while `GameplayBoxCounts` keeps focused counts bright.

**Tech Stack:** Unity 6, C#, URP RenderGraph, HLSL, Unity UI, existing batch-mode smoke-check harness.

---

### Task 1: Define pickup focus eligibility and lifecycle

**Files:**
- Modify: `Tools/Unity/BoosterSmokeChecks.cs`
- Modify: `Assets/_Game/_GamePlay/Script/Map/Booster/AntGameplay.Pickup.cs`
- Modify: `Assets/_Game/_GamePlay/Script/Map/Ant/AntGameplay.cs`
- Modify: `Assets/_Game/_GamePlay/Script/Map/Ant/BoxActor.cs`

**Step 1: Write the failing smoke checks**

Extend `RunPickup` to reflect `BoxActor.PickupFocusBoxes` and verify:

```csharp
var focusField = typeof(BoxActor).GetField("PickupFocusBoxes",
    BindingFlags.Static | BindingFlags.NonPublic);
var focus = (System.Collections.ICollection)focusField.GetValue(null);
check("Pickup focus API exists", focusField != null);
check("Pickup focus includes three visible valid rows",
    game.BeginPickupSelection() && focus.Count == 3);
check("Pickup focus clears on cancel", CancelAndCount(game, focus) == 0);
```

Add fixtures/checks for a hidden fourth row, special kinds, successful pickup, restart, switching to Blow, and pausing. Each exit must leave the set empty.

**Step 2: Run the isolated smoke test and verify RED**

Run:

```powershell
python -B Tools/prepare_level_verification.py
& 'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/Project_unity/ColonyFlow/Temp/LevelVerification/Project' -executeMethod LevelSmokeChecks.Run -logFile 'D:/Project_unity/ColonyFlow/Temp/LevelVerification/smoke.log'
```

Expected: compilation or smoke failure because `PickupFocusBoxes` and focus lifecycle do not exist.

**Step 3: Implement the shared predicate and focus set**

In `BoxActor`, add:

```csharp
internal static readonly HashSet<BoxActor> PickupFocusBoxes = new();
internal static bool PickupFocusActive => PickupFocusBoxes.Count > 0;
```

Remove the actor from the set in reset/disable paths. Reuse `CollectOutlineRenderers` for focus rendering.

In `AntGameplay.Pickup.cs`, extract the current validation into one method:

```csharp
private bool CanPickupBox(BoxActor box)
{
    if (!CanUseBooster() || box == null || !box.CanPickup || box.SlotIndex >= 0 ||
        box.QueueIndex < 0 || box.QueueIndex >= queues.Count ||
        Array.FindIndex(slots, item => item == null) < 0) return false;
    int row = queues[box.QueueIndex].IndexOf(box);
    return row >= 0 && row < VisibleQueueRows;
}
```

Add `RefreshPickupFocus` that clears the set, adds only candidates from the first three queue rows, and cancels pickup selection if none remain. Call it when selection begins, once per `Advance` while selection is active, and after queue refreshes. Make `PickupBox` call `CanPickupBox` instead of duplicating rules. Make `CancelBoosterSelection` clear the set synchronously.

Update `TogglePause` so entering pause cancels booster selection.

**Step 4: Run the isolated smoke suite and verify GREEN**

Run the Task 1 command again.

Expected: all pickup focus lifecycle checks pass with zero `CHECK_FAILED` entries in `smoke.log`.

**Step 5: Commit**

```powershell
git add -- Assets/_Game/_GamePlay/Script/Map/Booster/AntGameplay.Pickup.cs Assets/_Game/_GamePlay/Script/Map/Ant/AntGameplay.cs Assets/_Game/_GamePlay/Script/Map/Ant/BoxActor.cs Tools/Unity/BoosterSmokeChecks.cs
git commit -m "feat: track valid pickup booster targets"
```

### Task 2: Composite the world dim layer and redraw focused boxes

**Files:**
- Modify: `Tools/Unity/LevelSmokeChecks.cs`
- Modify: `Assets/_Game/_GamePlay/Materials/QueueScreenOutline.shader`
- Modify: `Assets/_Game/_GamePlay/Script/Map/Ant/QueueOutlineFeature.cs`

**Step 1: Add failing render-contract checks**

Add startup checks that find `Hidden/ColonyFlow/QueueScreenOutline`, require a third `FocusDim` pass, and verify that `QueueOutlineFeature` exposes serialized focus opacity/color configuration.

```csharp
var shader = Shader.Find("Hidden/ColonyFlow/QueueScreenOutline");
Check("pickup focus shader is available", shader != null);
Check("pickup focus shader has dim pass", shader != null && shader.FindPass("FocusDim") >= 0);
```

**Step 2: Run smoke checks and verify RED**

Run the Task 1 batch command.

Expected: `pickup focus shader has dim pass` fails.

**Step 3: Add the fullscreen dim shader pass**

Add a `FocusDim` pass to `QueueScreenOutline.shader`. It draws a fullscreen triangle, returns `_FocusDimColor`, uses `Blend SrcAlpha OneMinusSrcAlpha`, disables depth writes/testing, and does not sample the active color texture.

**Step 4: Extend the renderer feature**

Add serialized defaults:

```csharp
[SerializeField, Range(0f, 1f)] private float focusDimAlpha = .68f;
[SerializeField] private Color focusDimColor = Color.black;
```

Create a `FocusPass` at `BeforeRenderingPostProcessing`. In RenderGraph it must:

1. gather renderers from `BoxActor.PickupFocusBoxes`;
2. blend the fullscreen dim pass into `resources.activeColorTexture`;
3. attach `resources.activeDepthTexture` read-only;
4. redraw each focused renderer with its original shared material/submesh;
5. ignore null, disabled, inactive, or camera-culled renderers.

Enqueue focus before the existing outline pass so the retained white outline is drawn last.

**Step 5: Run smoke checks and verify GREEN**

Run the Task 1 batch command.

Expected: shader/feature contract checks and all prior smoke checks pass.

**Step 6: Commit**

```powershell
git add -- Assets/_Game/_GamePlay/Materials/QueueScreenOutline.shader Assets/_Game/_GamePlay/Script/Map/Ant/QueueOutlineFeature.cs Tools/Unity/LevelSmokeChecks.cs
git commit -m "feat: render pickup targets above scene dimmer"
```

### Task 3: Dim ordinary UI and preserve focused counts

**Files:**
- Modify: `Tools/Unity/LevelSmokeChecks.cs`
- Modify: `Assets/_Game/_GamePlay/Script/UI/CanvasGamePlay.cs`
- Modify: `Assets/_Game/_GamePlay/Script/Map/Ant/GameplayBoxCounts.cs`
- Modify: `Assets/_Game/_GamePlay/Script/Map/Editor/UIFlowSetup.cs`
- Regenerate: `Assets/_Game/_GamePlay/Prefabs/UI/CanvasGamePlay.prefab` (use the actual existing prefab path resolved by `UIFlowSetup.Save`)

**Step 1: Add failing UI structure checks**

Require the generated gameplay canvas to contain a full-stretch `Pickup Dim Root` with a `CanvasGroup`, and require `Selection Panel` plus `Gameplay Pointer Surface` to remain outside it.

Expected state:

```text
CanvasGamePlay
├─ Pickup Dim Root (CanvasGroup)
│  ├─ Header / header buttons
│  ├─ Status Text
│  └─ Booster Bar / booster buttons
├─ Gameplay Pointer Surface
└─ Selection Panel
```

**Step 2: Run UI setup/smoke checks and verify RED**

Run `UIFlowSetup.Apply` in the isolated verification project, then run `LevelSmokeChecks.Run`.

Expected: the `Pickup Dim Root` structure check fails.

**Step 3: Build and bind the dim root**

Update `UIFlowSetup.CreateGameplayPrefab` to create the root before ordinary HUD controls and parent those controls beneath it. Keep the pointer surface and selection panel direct children of the main canvas. Bind the root `CanvasGroup` to a new `pickupDimGroup` field on `CanvasGamePlay`.

In `CanvasGamePlay.RefreshSelection`, set:

```csharp
pickupDimGroup.alpha = gameplay.IsSelectingPickup ? .32f : 1f;
```

Do not change `interactable` or `blocksRaycasts`; the gameplay layer remains responsible for rejecting invalid actions while selection is active.

**Step 4: Keep target counts consistent**

In `GameplayBoxCounts`, attach/reuse a `CanvasGroup` per generated count label and set alpha each update:

```csharp
group.alpha = !BoxActor.PickupFocusActive || BoxActor.PickupFocusBoxes.Contains(box)
    ? 1f : .32f;
```

This leaves counts on focused boxes bright while other box counts match the dimmed scene.

**Step 5: Regenerate UI assets and verify GREEN**

Run `UIFlowSetup.Apply`, then `LevelSmokeChecks.Run` in the isolated project.

Expected: the hierarchy checks pass; prior pointer-routing and booster checks remain green.

**Step 6: Commit**

Stage only the four source files, the regenerated gameplay UI prefab/meta if changed, and relevant smoke checks. Commit:

```powershell
git commit -m "feat: dim gameplay UI during pickup selection"
```

### Task 4: Final verification and visual acceptance

**Files:**
- Modify only if a verification failure requires a scoped fix.

**Step 1: Run the complete isolated smoke suite**

Run the Task 1 batch command and inspect `Temp/LevelVerification/smoke.log`.

Expected: Unity exits successfully, no compiler errors, no `CHECK_FAILED`, and the final summary reports zero failures.

**Step 2: Apply generated UI to the main project**

Run:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/Project_unity/ColonyFlow' -executeMethod ColonyFlow.Gameplay.Editor.UIFlowSetup.Apply -quit -logFile 'D:/Project_unity/ColonyFlow/Temp/ui-flow-setup.log'
```

Expected: successful exit without compile or serialization errors.

**Step 3: Inspect MapDemo visually**

Verify in Play Mode:

- entering Pickup selection dims the world and ordinary HUD;
- exactly the valid first-three-row normal boxes remain bright;
- box transforms and colliders do not move;
- focused box counts and the Selection/Cancel panel remain bright;
- invalid areas do not perform ordinary queue picks;
- valid selection and Cancel both restore the frame immediately;
- behavior matches on the configured PC and Mobile renderer assets.

**Step 4: Review the final diff**

Run `git diff --check` and `git status --short`. Confirm unrelated pre-existing changes are not staged or modified by this work.

**Step 5: Final commit if verification required fixes**

Stage only scoped fixes and commit:

```powershell
git commit -m "fix: harden pickup focus presentation"
```
