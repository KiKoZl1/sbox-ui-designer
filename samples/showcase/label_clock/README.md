# Label Clock

A minimal HUD clock that demonstrates the simplest useful pattern in SUI Designer V1.5: a single `Text` element bound `OneWay` to a `string` Variable, with a tiny companion `Component` driving the Variable every frame.

If you've never built a SUI showcase, start here.

## What you'll see

A small dark pill anchored to the top-right of the screen. The label `Time:` sits above a large monospace value (e.g. `14:07:42`) that ticks every frame. The value text is green-ish (`#4ade80`) over a translucent near-black background with rounded corners. Nothing reacts to the mouse, nothing blocks input — it's a pure passive readout.

## How to use

1. Open the sample's `.sui` once in the **SUI Designer** window so the generator runs and emits `LabelClock.razor` and `LabelClock.razor.scss` into `Code/Samples/LabelClock/` (configured in the document's `Output` section).
2. Create or pick a `GameObject` in your scene and add the **`LabelClockController`** component to it (it lives in the `Sandbox.Samples` namespace).
3. Hit **Play**. The HUD appears immediately and starts updating.

There is no extra wiring. The controller spawns the wrapper itself via `[Property] public LabelClock Hud { get; set; } = new();`, calls `Hud.Show(SuiInputMode.Passive)` in `OnStart`, and writes to `Hud.ClockText` in `OnUpdate`.

## Variables exposed

| Name        | Type     | Source | Role                                                    |
|-------------|----------|--------|---------------------------------------------------------|
| `ClockText` | `string` | Manual | The rendered time string. Driven from `OnUpdate` in C#. |

## Bindings

| Element       | Property | Variable    | Mode    | Update trigger |
|---------------|----------|-------------|---------|----------------|
| `ClockValue`  | `Text`   | `ClockText` | `OneWay`| `OnChange`     |

The static `Time:` label has no binding — it's a literal string baked into the `.sui` because it never changes.

## Events

None. The clock is a pure read-out; nothing is clickable. The controller intentionally uses `SuiInputMode.Passive` so the HUD never steals focus or swallows pointer events from gameplay.

## Extending it

- **Change the format** by editing the controller's `[Property] string Format` in the inspector — try `HH:mm` for a slim look, `dddd HH:mm` to include the weekday, or `hh:mm:ss tt` for AM/PM.
- **Show server-time** by ticking `UseUtc` in the inspector. Combine with a fixed `Format` for a multiplayer scoreboard clock that's identical for every player.
- **Recolour** the value text by editing `ClockValue.Props.Color` in the Designer — drop in a red shade and a higher `FontSize` and you've got a respawn-style countdown placeholder.
- **Add a date row** by inserting a second `Text` element under `ClockPanel`, adding a new Variable `DateText : string`, binding `Text -> DateText` (OneWay), and writing `Hud.DateText = now.ToString("dddd, MMM d")` in `PushTime`.
- **Make it react to gameplay** by replacing the `DateTime.Now` source with anything Variable-shaped — match remaining time, round number, current zone name — same pattern: write the new value into `Hud.<VariableName>` and the binding propagates it.
