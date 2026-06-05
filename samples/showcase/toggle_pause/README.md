# Toggle Pause

A two-element HUD that demonstrates the single most important V1.5 concept for interactive widgets: **TwoWay binding on a `Toggle`'s `Checked` property**. The toggle owns the source of truth (`IsPaused`), the controller mirrors that boolean into a human-readable status label (`StatusText`), and the entire interaction loop runs without a single `Events` entry. If you understand this sample, you understand how every checkbox, switch, and on/off control in a V1.5 HUD is supposed to talk to its host `Component` — through Variables and bindings, not through Razor event handlers.

## What you'll see

A compact dark bar floats at the top-center of the screen, anchored 24px below the top edge and roughly 224×32 pixels wide. The bar has rounded corners and a near-black `#161618` fill. Inside it, on a flex row, sit two children: a small s&box `Toggle` labeled "Pause" on the left, and a green status text ("Running" or "Paused" in `#4ade80`) on the right.

Click the toggle once and the label switches to "Paused"; click again and it flips back to "Running". The rest of the screen is fully click-through — the canvas root has `PointerEvents = None` and only the `PauseBar` panel re-enables pointer events, so the HUD never blocks gameplay clicks outside that 224-pixel strip.

## Behavior

1. **Mount.** `OnStart` calls `Hud.Show( GameObject, SuiInputMode.MouseOnly )`. The HUD attaches to the player's screen panel stack with mouse input only — keyboard focus stays with the game so WASD and other actions keep working while the toggle is visible.
2. **Seed the label.** Still in `OnStart`, the controller reads `Hud.IsPaused` (default `false`) into `_lastIsPaused` and writes `Hud.StatusText = "Running"` so frame zero is consistent with the toggle state.
3. **User clicks the toggle.** The TwoWay binding pushes the new `Checked` value back into `Hud.IsPaused` immediately.
4. **`OnUpdate` edge-detects the change.** Every frame the controller compares `Hud.IsPaused` to `_lastIsPaused`; when they differ it updates the cache and rewrites `Hud.StatusText` to `"Paused"` or `"Running"`.
5. **The OneWay binding refreshes the label.** Because `StatusText`'s binding fires `OnChange`, the `Text` element repaints only when the string actually changes — no per-frame redraw cost.
6. **Teardown.** There is no explicit cleanup; the wrapper unmounts when the `GameObject` is destroyed.

## How to use

1. Open `toggle_pause.sui` in the SUI Designer window (`Window -> Sbox UI Designer`) and hit Compile.
2. Drop `TogglePauseController.cs` into `Code/Samples/TogglePause/` (or anywhere under `Code/`).
3. Attach `TogglePauseController` to a GameObject in any scene, hit Play.

No `User.scss` is required — the generated SCSS plus the bar's inline style values are enough for the sample to look correct out of the box.

## Variables

| Name | Type | Default | Role |
|---|---|---|---|
| `IsPaused` | `bool` | `false` | Source of truth for the toggle. TwoWay-bound to `PauseToggle.Checked` so clicking the toggle mutates this directly. |
| `StatusText` | `string` | `"Running"` | Label string mirrored from `IsPaused` by the controller. OneWay-bound to `StatusText.Text`. |

Both Variables are `Manual` source, public on the wrapper, no group, no resource type.

## Bindings

| Element | Property | Variable | Mode | Update Trigger |
|---|---|---|---|---|
| `PauseToggle` | `Checked` | `IsPaused` | TwoWay | OnChange |
| `StatusText` | `Text` | `StatusText` | OneWay | OnChange |

## Events

N/A — every element has an empty `Events` map. The TwoWay binding is the only "event" mechanism in the sample; click handling is internal to the engine `Toggle` widget.

## Required `User.scss` rules

N/A — fully driven by the generated SCSS. Bar background, padding, gap, and the status text color all come from inline values on the `.sui` elements, so a Force Regen is non-destructive even without a `User.scss` file.

## Controller architecture

- **`OnStart`** — mounts the wrapper as `SuiInputMode.MouseOnly`, seeds `_lastIsPaused`, writes the initial `StatusText`. No `OnAwake`, no `OnEnabled` — the wrapper is created via `[Property] Hud = new()` and revived during `OnStart` only.
- **`OnUpdate`** — single edge-triggered branch. `if ( Hud.IsPaused == _lastIsPaused ) return;` keeps the hot path empty when nothing changed; otherwise it updates the cache and writes the new label.
- **No timers, no coroutines, no input polling, no RPCs, no `[Sync]`.** The TwoWay binding is the entire interaction surface.
- **Mount mode is intentional.** `MouseOnly` is required because the toggle needs click input but the HUD must not steal keyboard focus from the gameplay layer.
- **`sealed class`** inheriting `Sandbox.Component`, namespace `Sandbox.Samples`.

## File map

```text
Code/Samples/TogglePause/
  TogglePause.cs               (generated wrapper — do not edit)
  TogglePausePanel.razor       (generated markup — do not edit)
  TogglePausePanel.razor.scss  (generated styles — do not edit)
  TogglePauseController.cs     (you ship this — drives the wrapper)
```

No `User.scss` is shipped with this sample. Create one only if you start restyling the bar.

## Element tree at a glance

```text
Root (Canvas, 1920x1080, PointerEvents=None)
  PauseBar (Panel, Anchor TopCenter, 224x32, PointerEvents=All)
    PauseToggle (Toggle, label "Pause", Checked <-> IsPaused)
    StatusText (Text, Text <- StatusText, green #4ade80)
```

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| HUD doesn't appear at all | Controller compiled before the wrapper was generated, so `Hud` is `null`. | Open `toggle_pause.sui` in the Designer and click Compile, then rebuild the project. |
| Toggle clicks but `StatusText` never updates | `OnUpdate` is not running — the `GameObject` is disabled or the component is paused. | Confirm the host `GameObject` is enabled and the scene is in Play. |
| Toggle visually changes but `IsPaused` stays `false` in the inspector | The binding mode is set to `OneWay` instead of `TwoWay`. | Re-open the binding `bind_toggle_paused` in the Designer and switch it back to `TwoWay`. |
| Label flickers between values every frame | `_lastIsPaused` is never read because the edge check was removed. | Restore the `if ( Hud.IsPaused == _lastIsPaused ) return;` guard at the top of `OnUpdate`. |
| HUD eats keyboard input | Mounted with `SuiInputMode.All` instead of `MouseOnly`. | Change the `Hud.Show` call to `SuiInputMode.MouseOnly`. |
| Clicking outside the bar is blocked | The canvas root lost its `PointerEvents = None`. | Set the `Root` canvas back to `PointerEvents.None` and keep `PauseBar` at `PointerEvents.All`. |

## Extending it

- **Add a `StatusColor : Color` Variable** bound to `StatusText.Color`, then flip it red when paused and green when running.
- **Drive real pause** by writing `Scene.TimeScale = Hud.IsPaused ? 0f : 1f;` inside the same `OnUpdate` edge branch.
- **Add a keyboard shortcut** with `if ( Input.Pressed( "pause" ) ) Hud.IsPaused = !Hud.IsPaused;` — the TwoWay binding pushes the new value into the toggle for free.
- **Network the pause state** by mirroring `IsPaused` to a `[Sync] bool NetPaused` on the same component and writing both sides on change.
- **Surface a fade overlay** by adding a `Panel` child of the canvas with `Opacity` bound to a derived variable, so the world dims when paused.
- **Localize the label** by replacing the `"Paused"`/`"Running"` literals with a lookup into your localization table inside `OnUpdate`.
- **Restyle the bar** by editing `PauseBar.BackgroundColor`, `BorderRadius`, and `Padding` in the Designer — no controller change required.

## Related

- [`../counter_button/`](../counter_button/) — companion sample for `OneWay` int binding driven from the controller.
- [`../text_entry_commit/`](../text_entry_commit/) — TwoWay binding on a `TextEntry` with explicit commit semantics.
- [`../settings_full/`](../settings_full/) — full settings panel that combines `Toggle`, `Slider`, and dropdown bindings into one screen.
