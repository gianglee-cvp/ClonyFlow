# Unified Level Data Implementation Plan

> Execute the approved design in the current IDE workspace. Preserve the existing nearest-target fix and unrelated local files.

**Goal:** One JSON per level, explicit component references, focused methods, no exception control flow in project-owned C#.

**Architecture:** Extend MapJsonData with authored queues and store them on MapModel. MapView loads once; AntGameplay creates boxes from that model. CellView, BoxActor and AntActor hold serialized renderers/colliders/canvas references; collider-to-box lookup is registered at initialization.

**Tech Stack:** Unity 6000.3, C#, JsonUtility, Unity serialized assets, Python unittest for repository invariants, Unity Editor smoke checks.

## Task 1: Regression checks

- Create Tools/Tests/test_level_assets.py.
- Run `python -m unittest discover -s Tools/Tests -v`; confirm failures for missing combined queues, legacy loaders, exceptions, and hierarchy searches.
- Add runtime smoke checks for authored queue order, nearest dispatch, collection, pause, restart, card resizing and completion.

## Task 2: Unified JSON and loading

- Modify Assets/_Game/_GamePlay/Script/Map/Data/MapJsonData.cs, MapJsonLoader.cs and MapModel.cs.
- Add queues to each existing map JSON, preserving cell/palette data and the currently assigned map/queue pair.
- Preserve the small queue variant as a complete level JSON.
- Remove BoxQueueJsonLoader.cs and its meta plus standalone BoxQueues data after migration.
- Verify palette references and per-color box budgets for all migrated levels.

## Task 3: Views and gameplay

- Modify MapView.cs, CellView.cs, Ant/BoxActor.cs, Ant/AntActor.cs and Ant/AntGameplay.cs.
- Replace hierarchy searches with serialized references and a registered collider dictionary.
- Split loading, spawning, movement, pickup resolution, slot timing, dispatch, routes and status into focused helpers.
- Remove legacy fallback fields/configuration and exception handling; keep explicit bool/null guards where useful.
- Preserve all existing map local transforms, visual heights, queue visibility, nearest dispatch and dynamic perimeter behavior.

## Task 4: Editor and assets

- Modify Editor/MapDemoSetup.cs, GameplayDemoSetup.cs and FixedLayoutSetup.cs to write the new references.
- Delete GameplaySceneGuard.cs and its meta after confirming it only upgrades legacy scenes.
- Migrate scene and prefab serialized fields while retaining GUIDs/fileIDs.
- Audit unused assets by code, GUID, build/settings and intentional reusable-core references. Remove proven obsolete assets only.

## Task 5: Core, documentation and verification

- Refactor exception branches in Assets/_Core/Tween/Runtime without removing its documented reusable service.
- Update Docs/map-usage.md and Assets/_Core/Tween/Docs/core-tween-usage.md.
- Compile runtime and Editor scripts using Unity references.
- Run repository checks and Unity runtime/scene/prefab smoke checks.
- Review diff and `git diff --check`; report exact verification and remaining limitations.
