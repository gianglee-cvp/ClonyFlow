# CongLy.Audio: reference analysis

This reference summarizes the user's supplied documentation, not inspected implementation. Use it to recognize contract and planning issues; do not copy audio-specific constraints into unrelated modules.

## Baseline supplied by the user

```text
CongLy Audio/Scripts/
  CongLy.Audio.asmdef
  IAudioManager.cs
  IAudioManagerSettings.cs
  AudioManager.cs
  DI/
    CongLy.Audio.DI.asmdef
    AudioManagerDI.cs
    AudioManagerVContainer.cs
```

`IAudioManager` owns playback/load/unload operations; `IAudioManagerSettings` owns volume/mute and external sound-source registration. `AudioManager` implements both. Two internal `AudioPool` objects contain key-to-clip and clip-to-source dictionaries and borrowed external sources. Three `AudioSettings` objects represent master, sound, and music. A shared queue reuses AudioSource components under a persistent container.

Preserve these contracts when planning from the full source document:

- Clip and object-key overloads; key loading through `IAssetsManager`.
- Async key loads use UniTask, optional progress, and cancellation tokens.
- Sound playback defaults to `loop=false`; music defaults to `loop=true`; `force=false` avoids restarting already playing content.
- Sound pause/resume/stop per item and for all; music pause/resume/stop plus `object? CurrentMusic { get; }` (clip or key, not a clip-only property) and `float MusicTime { get; set; }`.
- Preloading, auto-load warnings, unloading per item/all, and asset release for key loads.
- Volume settings in range 0–1; effective volume = pool volume × master volume; effective mute = pool mute OR master mute.
- Settings events and effective-output events; propagation to internal and registered external sources.
- CongLy.DI and VContainer registration expose both interfaces from the same instance, with dependency registration and duplicate checks. The supplied VContainer registration uses `Lifetime.Singleton`; do not replace it with scoped registration. Confirm CongLy.DI lifetime semantics from its implementation rather than infer them.

## Ambiguities that must remain visible

| Documentation issue | Planning response |
|---|---|
| One-shot summary says “not using pool”; detailed flow obtains a pooled clip source. | Flag the contradiction. Preserve the detailed flow as a labeled provisional reading if needed; do not silently invent temporary-source scheduling. |
| Multiple keys may resolve to one clip; ownership/release semantics are unspecified. | Inspect the asset manager contract before choosing reference counting, per-key ownership, or another release policy. Include direct borrowed clips in the decision. |
| Events promise effective-output changes, but handlers run whenever a parent setting changes. | Specify whether output comparison suppresses redundant events, e.g. master changes while pool volume is zero or pool mute is already true. |
| Two current-music fields identify clip and key selections. | Define cross-overload identity, switching, stop/unload behavior, and clearing of the previous selection. |
| Unload queues sources but reset and shutdown are incomplete. | Plan source reset, queue ownership, container destruction, event unsubscription, and borrowed-source treatment without claiming these already exist. |
| Async load/cache helpers are named but no race/cancellation contract is supplied. | Identify same-key concurrent loads, per-caller cancellation, failed loads, and unload/dispose while pending. Do not infer helper guarantees from names. |
| Volume validation specifies lower/upper bounds only. | Define non-finite-value handling if relevant, clearly labeled as a proposal rather than an existing rule. |
| One DI assembly references both frameworks. | Preserve supplied layout; note both-package requirement. Offer separate optional integration assemblies only when optional installation is needed. |

## Example task with an observable contract

**Task: Settings propagation in the existing AudioManager topology**

- **Files:** proposed `CongLy Audio/Scripts/AudioManager.cs`; test path selected from the actual repository's test layout.
- **Prerequisite:** decide whether effective-output events suppress unchanged output.
- **Changes:** implement three internal settings objects and two pool subscriptions; update internal and registered sources from computed output; preserve independent setting events.
- **Acceptance:** sound 0.5 × master 0.4 outputs 0.2; music uses its own setting; master mute overrides both pools; unregister prevents further updates to that borrowed source.
- **Verification:** EditMode checks for calculation/event rules if isolated logic allows; PlayMode checks for AudioSource propagation and borrowed-source lifecycle. Master-volume changes with sound volume zero must follow the selected event policy.

Other tasks should cover complete interface contracts, load ownership, source reuse, playback/music transitions, async failure/cancellation, unload/teardown, and both DI adapters. Additional classes require a demonstrated need; the template does not demand an asset adapter or playback-handle class.
