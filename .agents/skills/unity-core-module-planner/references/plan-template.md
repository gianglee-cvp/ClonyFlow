# Core module plan template

Adapt this structure to the requested module. Omit irrelevant sections; fill meaningful fields rather than copying empty placeholders into the final plan.

## 1. Goal, scope, and evidence

State the problem, consumers, required behavior, and explicit exclusions. List supplied documents and inspected source paths. Mark repository/package checks that could not be performed. Record confirmed requirements, proposals, assumptions, and questions separately.

## 2. Module structure and dependencies

Show the directory tree with exact paths, distinguishing existing files from proposed files. Include namespace, assembly names, references, and optional integration boundaries. Preserve the supplied topology unless a justified change is proposed.

| File/component | Visibility | Responsibility | Dependencies | Lifetime/owner |
|---|---|---|---|---|

Use a small dependency diagram if it clarifies direction. Core should not require a DI adapter that already depends on core. If the user requests two DI frameworks, plan both; do not silently remove one to simplify assembly design.

## 3. Public API contracts

Provide C# interface signatures grouped by responsibility, preserving overloads and defaults. For each group explain:

- Validation, null/invalid inputs, supported value ranges.
- Return meaning and failure behavior.
- State transitions and idempotency/repeated calls.
- Observable side effects, events, resource acquisition/release.
- Cancellation and progress semantics for async operations.

Document event payloads and timing; distinguish setting changes from effective-output changes. Flag contradictions between summaries, signatures, and pseudocode.

## 4. Internal design and invariants

Describe state and collections without adding abstraction layers unnecessarily. Define formulas and update propagation for settings where applicable. State invariants in observable terms, such as “at most one current music selection” or “unloading one owner does not release another owner's resource.”

## 5. Lifecycle and ownership

| Resource/state | Created/acquired by | Owned or borrowed | Shared identity policy | Released/reset by |
|---|---|---|---|---|

Cover initialization, normal operation, scene/scope transition, unload, and shutdown. Define cleanup of internal objects, subscriptions, and pending operations. State what disposal may do to external objects. For pools, specify reset fields and when an object is eligible for reuse.

## 6. Main flows and failure cases

Use short pseudocode for representative load/operate/update/unload flows. Include failed operations, overlapping requests, cancellation, and unload while pending where relevant. Ensure flows implement the public contracts and ownership rules.

## 7. Integration and usage

Show constructor dependencies and minimal consumer examples. Explain registration lifetime, duplicate checks, resolving multiple interfaces to one instance, and teardown responsibility. Verify exact framework methods against inspected source or current official documentation if needed; otherwise label pseudocode/proposals.

## 8. Ordered implementation tasks

Repeat this task form with concrete values:

### Task N: Concrete behavior or component

- **Prerequisites:** earlier tasks or decisions.
- **Files:** exact create/modify paths; do not invent line numbers.
- **Changes:** responsibilities and behavior to add, with contract details or focused pseudocode.
- **Acceptance:** observable outcomes, including relevant failures.
- **Verification:** existing test/command when known, or a proposed EditMode/PlayMode/manual scenario with expected results. State prerequisites for running it.

Keep tasks small enough to implement and review independently. Include assembly wiring, consumer integration, and documentation only when in scope.

## 9. Verification and remaining decisions

| Required capability/invariant | API/component | Task | Verification scenario | Expected result |
|---|---|---|---|---|

Separate planned verification from any checks actually executed during planning. End with material unresolved choices and assumptions; identify only the tasks they block.
