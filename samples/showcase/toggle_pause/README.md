# Toggle Pause

A minimal showcase of **two-way input binding** in the s&box UI Designer (`.sui`).

A `Toggle` widget is `TwoWay`-bound to a `bool` Variable, and a `Text` element next to it mirrors the toggle state with a coloured label. The smallest possible "user input flips a flag, UI reacts" example.

## What you'll see

A small dark bar appears at the top centre of the screen. It contains a "Pause" toggle and a status label. Frame zero the label reads `Running` in green. Click the toggle and the label instantly flips to `Paused`. Click again to flip back. No timers, no animation, no networking — just the binding round-trip.

## How to use

1. Open `toggle_pause.sui` once in the **SUI Designer** window (`Window → Sbox UI Designer`) and hit **Compile**. This writes `TogglePause.razor` + `TogglePause.scss` + `TogglePause.cs` (the wrapper) into `Code/Samples/TogglePause/` of your project.
2. Drop `TogglePauseController.cs` into the same folder (or anywhere under `Code/`).
3. In any scene, add a new GameObject and attach the **TogglePauseController** Component to it.
4. Press **Play**. The bar appears at the top of the screen. Click the toggle and watch the label flip colour and text.

The Component holds no Inspector-tunable state — all live state lives on the wrapper's Variables, which is exactly the pattern you want for binding-driven UIs.

## Variables

| Name | Type | Role |
|---|---|---|
| `IsPaused` | `bool` | The pause flag. `TwoWay`-bound to the toggle's `Checked` property — the user flips it from the UI, the controller reads it. |
| `StatusText` | `string` | Human-readable mirror of `IsPaused`. `OneWay`-bound to the status label. Written by the controller in `OnUpdate` whenever `IsPaused` changes. |

## Bindings

| Element | Property | Variable | Mode |
|---|---|---|---|
| `PauseToggle` (Toggle) | `Checked` | `IsPaused` | TwoWay (UI and code both mutate it; `UpdateTrigger = OnChange`) |
| `StatusText` (Text) | `Text` | `StatusText` | OneWay (UI reads the Variable; never writes back) |

## Events

None — this sample is purely binding-driven. The toggle's `TwoWay` mode is the only "event": every click mutates `Hud.IsPaused`, and the controller's `OnUpdate` notices the edge and rewrites `Hud.StatusText`.

## Extending it

- **Switch the status colour with the state** by adding a second Variable `StatusColor : Color`, binding the Text element's `Color` to it, and assigning `Hud.StatusColor = Hud.IsPaused ? Color.Red : Color.Green;` next to the `StatusText` update.
- **Drive actual game pause** by replacing the `_lastIsPaused` mirror with `Scene.TimeScale = Hud.IsPaused ? 0f : 1f;` — the toggle becomes a real pause button.
- **Add a keyboard shortcut** by polling `Input.Pressed("pause")` in `OnUpdate` and setting `Hud.IsPaused = !Hud.IsPaused;` — the `TwoWay` binding pushes the change back into the toggle UI automatically.
- **Make it networked** by marking `IsPaused` `[Sync]` on the controller (mirror it from `Hud.IsPaused`) so every client sees the same pause state — see the `networking.md` reference in the sbox-pro skill for the patterns.
- **Style the bar** by editing `PauseBar.BackgroundColor`, `BorderRadius`, or `Padding` directly in the Designer — no code change needed.
