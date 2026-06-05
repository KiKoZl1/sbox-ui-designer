# Counter Button

The smallest end-to-end loop the SUI Designer V1.5 can express: one `Button` element with an `OnClick` event in **Code mode**, one `string` Variable, and one `OneWay` `Text` binding driven by `OnChange`. This sample isolates the **wrapper-mount + Variable + Binding round-trip** — you click a button, a controller field changes, the bound `Text` element re-renders. No animation, no networking, no Doo bodies, no input wiring. If you are learning V1.5, start here; every other sample in `showcase/` layers concepts on top of this loop.

## What you'll see

A dark grey card (300x220, rounded corners) sits dead centre of the screen against a near-black canvas background. Inside the card a large bold green number reads `0` near the top, and a square green `+1` button sits below it. The button lightens and scales up slightly on hover, darkens and scales down on press — those state styles are baked into the generated SCSS, not the controller.

Each click increments the number. There is no other UI on screen, no HUD chrome, no keyboard hint — the goal is to make the data flow easy to see.

## Behavior

1. The scene loads and `CounterButtonController.OnStart()` runs.
2. The controller assigns `Hud.OnIncrementClick = OnIncrementClick` **before** mounting — this is the critical ordering for Code-mode events.
3. `Hud.Show(GameObject, SuiInputMode.MouseOnly)` mounts the generated `CounterButton` wrapper as a child PanelComponent. Mouse passes through to the button, keyboard stays with gameplay.
4. The controller seeds `Hud.CountText = Count.ToString()` so frame zero shows the live `Count` value (defaults to `"0"`).
5. User clicks the `+1` button. The renderer invokes the `OnIncrementClick` Action, which routes to the controller method, which bumps `Count` and writes `Hud.CountText = Count.ToString()`.
6. The `OneWay / OnChange` binding on `CountLabel.Text` detects the Variable change and pushes the new string into the element on the next layout pass.

There is no `OnUpdate`, no `OnDestroy` cleanup — `Hud.Show` parents the panel under the GameObject, so destroying the GameObject tears the UI down with it.

## How to use

1. Open `counter_button.sui` in the SUI Designer window (`Window -> Sbox UI Designer`) and hit Compile. This emits `CounterButton.razor`, `CounterButton.razor.scss`, and (if missing) the `CounterButton.cs` wrapper into `Code/Samples/CounterButton/`.
2. Drop `CounterButtonController.cs` into `Code/Samples/CounterButton/` (or anywhere under `Code/` — namespace is `Sandbox.Samples`).
3. Attach `CounterButtonController` to any GameObject in any scene, hit Play. The card appears centred; clicking `+1` advances the number.

No `User.scss` rules are required — every visual is driven by the Designer-emitted SCSS.

## Variables

| Name | Type | Default | Role |
|---|---|---|---|
| `CountText` | `string` | `"0"` | Displayed counter value as a string. The controller writes to it after each click; the binding pushes the new value into `CountLabel`. Manual source, public. |

## Bindings

| Element | Property | Variable | Mode | Update Trigger |
|---|---|---|---|---|
| `CountLabel` (Text) | `Text` | `CountText` | `OneWay` | `OnChange` |

## Events

| Element | Event | Mode | Handler |
|---|---|---|---|
| `IncrementButton` (Button) | `OnClick` | `Code` | `OnIncrementClick` |

## Required `User.scss` rules

N/A — fully driven by the generated SCSS. Hover and pressed states are emitted from the Designer's `HoverStyle` / `PressedStyle` on `IncrementButton`.

## Controller architecture

- `[Property] CounterButton Hud { get; set; } = new()` — inline-constructed so the wrapper appears in the Inspector and you can see / swap its fields without manually `new`-ing in code.
- `[Property] int Count { get; set; } = 0` — inspectable seed value. Set to `42` in the Inspector and the card boots showing `42`.
- `OnStart()` is the only lifecycle override and runs three statements in fixed order:
  1. Assign `Hud.OnIncrementClick` delegate.
  2. `Hud.Show(GameObject, SuiInputMode.MouseOnly)` — mounts and runs `SyncFieldsTo` which copies the delegate into the renderer Panel.
  3. Seed `Hud.CountText` so the bound Text reflects the live counter on frame one.
- `OnIncrementClick()` is the single private handler — increment `Count`, write `Hud.CountText = Count.ToString()`. The `OnChange` binding does the rest.
- No `OnUpdate`, no input polling, no timers, no networking, no persistence, no Doo bodies.

The handler name matches the `.sui` Handler string `OnIncrementClick` **by convention only**. The Designer generator emits an `Action` property on the wrapper called `OnIncrementClick`; the controller wires its method to that property explicitly in `OnStart`. There is no reflection or name-based magic — rename one and you must rename the other.

## File map

```text
Code/Samples/CounterButton/
  CounterButton.cs               (generated wrapper - do not edit)
  CounterButton.razor            (generated markup - do not edit)
  CounterButton.razor.scss       (generated styles - do not edit)
  CounterButtonController.cs     (you ship this - drives the wrapper)
```

No `User.scss` — every visual rule lives in the generated SCSS and is regenerated on Compile.

## Element tree at a glance

```text
- Root (Canvas, 1920x1080, PointerEvents=None)
  - CounterPanel (Panel, 300x220, centred, Flex Column, bg #161618)
    - CountLabel (Text "0", 48px bold green, bound to CountText)
    - IncrementButton (Button "+1", green, OnClick -> OnIncrementClick)
```

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Compile error: `The type or namespace 'CounterButton' could not be found`. | Controller compiled before the wrapper was generated — you dropped `CounterButtonController.cs` without hitting Compile in the Designer. | Open `counter_button.sui`, hit Compile, then rebuild. The wrapper `CounterButton.cs` must exist under `Code/Samples/CounterButton/` before the controller resolves. |
| Card shows `0` and never advances on click. | Delegate was assigned to `Hud.OnIncrementClick` **after** `Hud.Show(...)`. `SyncFieldsTo` ran during Show with a null delegate and the renderer never got wired. | Assign `Hud.OnIncrementClick = OnIncrementClick;` on the line **before** `Hud.Show(...)`. This is documented in the Events & Actions Code-mode docs. |
| Card not visible at all in the scene. | No mounted PanelComponent — controller forgot to call `Hud.Show(...)`, or the GameObject the controller is attached to is disabled. | Confirm `OnStart` calls `Hud.Show(GameObject, SuiInputMode.MouseOnly)`; check the GameObject is enabled and the Component itself is enabled. |
| Number updates internally but `CountLabel` stays at `0`. | The binding's `UpdateTrigger=OnChange` is firing but you wrote to `Count` instead of `Hud.CountText`. Bindings track the Variable on the wrapper, not arbitrary controller fields. | Always write through the Variable: `Hud.CountText = Count.ToString();`. The controller's `Count` field is just bookkeeping. |
| Hover / pressed styles missing. | `User.scss` overrides are clobbering the generated `:hover` / `:active` rules. | Either delete the conflicting `User.scss` block or scope your override more tightly so the Designer-emitted state rules still win. |

## Extending it

1. **Recolour without touching code.** Open `counter_button.sui`, change `CountLabel.Color` and `IncrementButton.BackgroundColor`, Compile, hit Play — no controller change.
2. **Add a Reset button.** Duplicate `IncrementButton` in the Designer, rename to `ResetButton`, add an `OnResetClick` Code event, write a `OnResetClick()` handler in the controller that sets `Count = 0` and pushes `Hud.CountText`.
3. **Persist Count across sessions.** In `OnDestroy`, write `FileSystem.Data.WriteJson("counter.json", Count)`. In `OnStart`, read it before assigning the delegate so the seeded value survives restarts.
4. **Trigger via hotkey.** Override `OnUpdate`, call `Input.Pressed("jump")` and route to `OnIncrementClick()` — same handler now drives mouse and keyboard.
5. **Add a high-score readout.** Add a `BestText` Variable + second `Text` element. Track `Math.Max(Count, Best)` in the controller, write through both Variables.
6. **Animate the click.** Add a `Scale` Variable bound to `CountLabel.TransformScale`, ramp it from 1.2 to 1.0 across a few frames after each click in `OnUpdate`. This is the natural bridge to the animation samples.
7. **Network it.** Promote `Count` to a `[Sync]` field on a networked component, push `Hud.CountText` in a `[Rpc.Broadcast]` so every client sees the same number.

## Related

- [`../variable_binding_oneway/`](../variable_binding_oneway/) — same `OneWay / OnChange` Text binding pattern with no Events, isolating just the data-flow half.
- [`../button_doo_event/`](../button_doo_event/) — same Button + Variable shape but the `OnClick` runs in **Doo mode** (inline body) instead of Code mode, no controller required.
- [`../hud_health_bar/`](../hud_health_bar/) — next step up: multiple bound Variables, a controller `OnUpdate` driving a bound numeric value, still no networking.
