# Pickup Booster Focus Design

## Goal

When the player activates the Pickup booster, darken the entire game presentation and leave only valid pickup boxes, the selection instruction, and the Cancel button at full brightness. The effect must not move gameplay transforms or alter physics.

## Interaction

- Pressing the Pickup button enters pickup selection and fades in the focus treatment.
- A valid target is a normal box with ants remaining, outside a slot, and within the first three visible rows of a queue.
- Valid boxes appear above the dark treatment at their normal color, with the existing outline treatment retained.
- The rest of the world, HUD, and booster bar appear dimmed.
- The selection instruction and Cancel button remain bright and interactive.
- Selecting a valid box, pressing Cancel, restarting, pausing, completing the board, or switching boosters clears the treatment immediately.
- Target eligibility is refreshed while selection is active so a box that changes state is no longer presented as selectable.

## Rendering Architecture

The visual stack is conceptual rather than physical:

1. Render the game world normally.
2. Composite a translucent black fullscreen layer over the world.
3. Draw the eligible box renderers again over that layer using their original materials.
4. Dim the ordinary gameplay UI while leaving the selection instruction and Cancel control undimmed.

The boxes are not translated upward in world space. Re-rendering them above the dim layer produces the requested lifted appearance without changing layout, collider positions, navigation, animation destinations, or pointer hit testing.

The existing `QueueOutlineFeature` already performs targeted box rendering through URP RenderGraph. It will be extended with a separate pickup-focus pass rather than introducing another camera or mutating material assets. Both PC and Mobile renderer assets already reference this feature, so one implementation covers both pipelines.

## Gameplay State and Data Flow

`AntGameplay` remains the authority for target eligibility. A single predicate will be used by both `PickupBox` and the focus-target refresh so presentation cannot disagree with gameplay validation.

While `IsSelectingPickup` is true, `AntGameplay` refreshes a small focus set from the three visible rows in each queue. `BoxActor` exposes the renderers needed by the renderer feature, following the same pattern as the existing outline set. Cancelling selection clears the focus set synchronously.

`CanvasGamePlay` observes the current selection state during its existing refresh loop. It applies a reduced alpha to ordinary HUD roots and keeps the selection panel on an undimmed sorting layer. The fullscreen world treatment does not intercept raycasts; the existing pointer surface continues routing clicks to `AntGameplay.HandlePointer`.

## Failure and Cleanup Behavior

- If no eligible target remains, pickup selection exits instead of displaying an empty focus state.
- Missing optional UI references degrade to the world focus effect without throwing exceptions.
- Disabled or destroyed boxes are ignored by the render pass.
- Focus state is cleared from all existing booster-cancellation and session-reset paths.
- Renderer materials and object transforms are never mutated, avoiding restoration failures.

## Verification

Automated smoke coverage will verify:

- only boxes accepted by `PickupBox` enter the focus set;
- hidden rows, slot boxes, empty boxes, and special box kinds are excluded;
- cancellation, successful pickup, restart, pause, and booster switching clear focus state;
- changing a focused box to an invalid state removes it during refresh.

Unity batch-mode smoke checks will cover compilation and gameplay behavior. A rendered MapDemo check will confirm the fullscreen dim, bright eligible boxes, retained pointer selection, and bright Cancel UI on the active URP renderer.
