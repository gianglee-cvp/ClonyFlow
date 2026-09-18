# Gameplay boosters

Booster partials own their gameplay operations and session state. Actor movement uses Core Tweening; actor and cell lifetime uses Core Pooling. UI selects a target and invokes a gameplay command. Core modules do not depend on this folder.

- `AntGameplay.Slots.cs`: adds one slot per session and restores the original layout.
- `AntGameplay.Pickup.cs`: validates box identity, visible row and eligibility before acquisition.
- `AntGameplay.Blow.cs`: resolves cancellation, reservations, cells and budgets for one color.
- `AntGameplay.BoosterUI.cs`: presents actions and mutually exclusive selections; scales drawing and pointer ownership consistently.

See `Docs/map-usage.md` for public APIs and schema, and `Tools/Unity/BoosterSmokeChecks.cs` for PlayMode verification.
