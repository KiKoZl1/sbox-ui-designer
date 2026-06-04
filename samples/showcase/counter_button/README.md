# Counter Button

A minimal showcase of the **Events + Variables + Binding** loop in the s&box UI Designer (`.sui`).

A single button increments a counter that's stored in C# and displayed in a `Text` element via a one-way Variable binding — the smallest possible "click does something, UI reflects it" example.

## What you'll see

A small dark card floats in the middle of the screen. It contains a large green `0` and a green `+1` button below it. Every click bumps the number by one and the label updates instantly. There is no other state — no animation, no networking, no inventory plumbing.

## How to use

1. Open the `counter_button.sui` once in the **SUI Designer** window (`Window → Sbox UI Designer`) and hit **Compile**. This writes `CounterButton.razor` + `CounterButton.scss` + `CounterButton.cs` (the wrapper) into `Code/Samples/CounterButton/` of your project.
2. Drop `CounterButtonController.cs` into the same folder (or anywhere under `Code/`).
3. In any scene, add a new GameObject and attach the **CounterButtonController** Component to it.
4. Press **Play**. The card appears centred. Click `+1` and watch the number climb.

The Component's `Count` property is exposed in the Inspector if you want to inspect or seed it from the editor.

## Variables

| Name | Type | Role |
|---|---|---|
| `CountText` | `string` | Stringified counter value. The companion writes to this after each click; the `CountLabel` element binds its `Text` property to it. |

## Bindings

| Element | Property | Variable | Mode |
|---|---|---|---|
| `CountLabel` (Text) | `Text` | `CountText` | OneWay (UI reads the Variable; never writes back) |

## Events

| Element | Event | Mode | Handler |
|---|---|---|---|
| `IncrementButton` (Button) | `OnClick` | Code | `OnIncrementClick` (resolved on the companion `Component`) |

## Extending it

- **Change the colour scheme** by editing `CounterPanel.BackgroundColor`, `CountLabel.Color`, and `IncrementButton.BackgroundColor` in the Designer — no code change needed.
- **Add a reset button** by duplicating `IncrementButton`, swapping `ButtonText` to `Reset`, and wiring its `OnClick` to a new `OnResetClick()` method that sets `Count = 0; Hud.CountText = "0";`.
- **Persist the count across sessions** by saving `Count` to `FileSystem.Data` in `OnDestroy()` and reloading it in `OnStart()` before the first `Hud.Show()`.
- **Make it react to a keypress** by polling `Input.Pressed("jump")` in `OnUpdate()` and calling `OnIncrementClick()` from there — same handler, two triggers.
- **Show a "high score"** by adding a second Variable `BestText : string` and binding a new `Text` element to it; update it whenever `Count > previousBest`.
