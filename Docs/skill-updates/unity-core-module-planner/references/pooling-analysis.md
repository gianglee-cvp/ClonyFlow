# Pooling contract analysis

Read when planning GameObject pooling from documentation or code. These are decision points, not mandatory architecture choices.

## Preserve and reconcile source contracts

Capture every supported path: direct prefab, key, typed component, implicit type key, sync/async, DI. Preserve defaults and parameter names, including named arguments in examples. Compare signatures with pseudocode: an Action<GameObject> event requires its instance payload. Label inconsistencies instead of presenting snippets as validated implementation.

When adapting to a project, list deliberate changes such as void/throw to bool/null, automatic deactivate, different namespace or missing dependency boundaries. A user's no-throw policy is project-specific; do not make it a universal pooling requirement. Do not silently replace UniTask with another async type or claim an adapter exists when its package was not inspected.

## Pool identity and asset ownership

- Distinguish prefab identity, lookup key, pool identity, borrowed instance and resource lease. Two keys may resolve to the same prefab and pool without having the same release ownership.
- Direct prefab references are normally borrowed assets. Define whether unloading a key removes only its mapping/lease or also destroys a pool, and what protects other keys/direct consumers.
- Record prefab-to-pool and instance-to-pool mappings and when entries are removed. Do not equate unloading one alias with permission to release all aliases.
- Choose scene/application lifetime from consumers; an inactive root and DontDestroyOnLoad from an example are not automatic requirements.

## State and callbacks

Define available versus borrowed invariants, destroyed-object pruning, foreign/duplicate recycle behavior and growth when available is empty. Prewarm count may mean minimum available, total capacity or additive creation; preserve the documented meaning and test repeated calls.

Specify transforms before activation: world/local coordinate policy, parent, omitted position/rotation defaults and original scale. Decide reset-hook order relative to SetActive, Awake, OnEnable and events. Prewarming under an inactive parent can defer Awake; a reset hook cannot blindly assume caches were initialized there.

Recycle without SetActive(false) may still trigger OnDisable when reparenting under an inactive root. Distinguish activeSelf from activeInHierarchy; neither implies that task data, carried visuals or scale were reset. A tween that ended at scale zero needs explicit scale restoration before reuse. Cancel on recycle and dispose on actual destruction are separate actions.

Define event payload/timing and whether state is consistent when callbacks run. Consider a Spawned handler immediately recycling, and bulk events changing collections. Use snapshot iteration where appropriate; either define reentrant Unload/Dispose behavior with guards or state that it is unsupported. Do not promise arbitrary callback safety without a design.

Cleanup usually affects available objects only; RecycleAll affects borrowed instances; Unload/Dispose may destroy both according to the chosen contract. Document event behavior for each, not just method names.

## Typed component helpers

A helper that calls GetComponent<T> every spawn is a repeated lookup. Propose cache by instance/type at creation or first bind when a consumer needs it, respecting root-only prefab composition and user restrictions. Define missing-component behavior and avoid leaving an acquired object borrowed when a typed spawn fails. Destroy/unload must remove component cache entries. Do not infer hierarchical discovery is acceptable.

## Borrowed references across operations

Trace who still holds an instance after it leaves the screen or slot. A visual slot being free does not mean an asynchronous task stopped referencing its source. Reusing a box before old ants resolve into it can mutate a new lease's counters. Choose delayed recycle, lease/generation validation or clearing references after resolution according to the concrete flow; do not add handles universally.

Reset fields should come from observed consumers: source/target/task ID, paths/reservations, visuals, labels, collider registration, timers, animations and scale. Separate reset responsibilities owned by the pool from gameplay bookkeeping owned by a session. Recycling children before destroying their old session parent prevents accidental pool destruction.

## Async key loading, when present

Define same-key operation coalescing, caller cancellation versus shared-load cancellation, progress for joined callers, repeated count semantics, failed-load caching and retry. Unload/Dispose during a pending load must prevent late completion resurrecting the old mapping/pool, for example through generation checks. Specify who releases an acquired resource if no caller retains the result.

Alias-key resource release and cancellation semantics depend on the Resource API; mark them unresolved when that API is unavailable. Do not invent a loader, cancellation implementation or ownership lease merely to fill a template.

## Focused verification

Use PlayMode for activation order, inactive-parent prewarm, transforms, destroyed Unity objects and tween lifecycle. Suggested scenarios: prewarm twice; spawn/recycle/reuse same identity; duplicate/foreign release; typed cache miss without acquisition leak; Cleanup while borrowers exist; zero-scale actor reuse; source retained by a pending task; restart during animation; two keys same prefab; concurrent loads and unload before completion. These are candidates to select by scope, not a mandatory checklist for every module.
