---
name: unity-core-module-planner
description: Use when the user asks to plan or design a reusable Unity core service from module documentation, API notes, or existing code, including Audio, Save, Resource, Logging, and Pool modules. Does not apply to implementation-only requests or gameplay feature scaffolding.
---

# Unity Core Module Planner

Turn module documentation into an implementable architecture and file-level plan. Write in the user's language; preserve C# identifiers. Produce plans, not production C# changes, unless implementation is separately requested.

## Ground the design

- Read supplied Markdown and relevant source, repository instructions, assembly definitions, and package manifests when available. A document describes intended behavior; it does not prove implementation behavior.
- Separate **confirmed requirements**, **observed code**, **proposed decisions**, and **open questions**. Do not claim package availability or API compatibility without evidence.
- Preserve supplied names, paths, interfaces, return types (including nullability), defaults, overloads, DI lifetime, and integration choices. Never narrow a key-or-clip property to a clip-only type. Proposed paths must be labeled when no source exists.
- Use the existing topology as the baseline. Do not introduce a registry, adapter, scheduler, playback handle, singleton, or persistence layer merely because another module uses one. Explain a concrete need before proposing a new layer.
- Resolve routine choices with explicit assumptions. Ask only about ambiguities that materially change public contracts, ownership, or scope; continue independent sections while answers are pending.

## Specify contracts before tasks

1. Identify consumers, supported operations, scope, and exclusions. Separate playback/operation APIs from configuration only where the module benefits from it.
2. For each API group, state input validation, defaults, state transitions, repeated-call behavior, errors, and side effects. Include overload consistency and read-only properties.
3. Describe internal responsibilities, collections, state invariants, and formulas. Trace settings propagation and define whether events mean a setting changed or its effective output changed.
4. Define ownership: creator, lifetime, acquire/release pairing, borrowed external objects, shared keys/resources, and disposal. Include subscription removal, pooled-object reset, and scene/scope lifetime where relevant.
5. For async operations specify concurrent calls, cancellation ownership, failed-load caching, and unload/dispose during loading. Avoid inventing thread-safety or package-specific behavior.
6. Plan DI against the actual frameworks: shared instance across interfaces, lifetime, duplicate registration, dependency ordering, assembly isolation, and disposal ownership. Do not install dependencies as part of planning.

Read [plan-template.md](references/plan-template.md) when producing the deliverable. Read [audio-analysis.md](references/audio-analysis.md) for the CongLy.Audio example or analogous key/resource pooling questions. Audio formulas and classes are examples, not requirements for Save or Logging.

## Build a reviewable plan

When the user requests phased implementation, review and commits, order tasks by dependencies and state build/test/review acceptance before each commit. Clarify material contract questions before code; preserve previous authorization instead of repeating approvals. Keep baseline fixes separate from new features and exclude unrelated staged/user changes. Project naming, one-responsibility functions, component caching and documentation topology should be reflected in the plan when requested, without imposing one project's rules on unrelated modules. Planned checks are not executed evidence.

For GameObject pooling, read [pooling-analysis.md](references/pooling-analysis.md). It covers prefab/key aliases, resource ownership, inactive-parent reset, typed component caching, event timing, borrowed task references and async unload. Preserve source APIs and label adaptations required by the user's project rather than turning example-specific conventions into universal rules.

Create one Markdown artifact in the user's location or `docs/plans/YYYY-MM-DD-<module>-plan.md`, unless the user requests an inline/read-only plan or excerpt. Include interface signatures and focused pseudocode where they resolve a contract; full method bodies are unnecessary for planning.

Order tasks by dependencies. Each task names exact proposed/existing files, responsibilities, behavior to implement, prerequisites, acceptance criteria, and suitable verification. Avoid tasks such as “build manager” without an observable outcome. Reuse existing tests and distinguish EditMode logic checks from PlayMode engine behavior. Never report planned checks as executed or invent successful results.

Before delivery, check that every requested capability maps to an API, component, task, and verification; that signatures and flows agree; and that unresolved decisions are visible. Preserve explicit user scope and authorization. Do not commit, install, or execute the planned module without a request.
