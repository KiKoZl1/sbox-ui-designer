# Health Bar

A minimal player-health HUD built with the SUI Designer. One `ProgressBar` plus
two `Text` widgets, bound to two Variables driven by the companion component.

## What you'll see

A small dark panel anchored to the top-left of the screen with the label `HP`,
a green fill bar, and a `current / max` numeric readout. The bar shrinks and
the text updates whenever `Health` changes — no manual UI plumbing, just
Variable assignments.

## How to use

1. Open `health_bar.sui` in the SUI Designer (`Tools → SUI Designer` in the
   editor), confirm it loads, and hit **Compile**. The generator emits
   `HealthBar.cs` + `HealthBarPanel.razor` + `HealthBarPanel.scss` under
   `Code/Samples/HealthBar/` in the `Sandbox.Samples` namespace.
2. Copy `HealthBarController.cs` into your project (anywhere under `Code/`).
3. In a scene, create a GameObject and add the **HealthBarController**
   component. Hit **Play** — the bar appears immediately.
4. Drive it from anywhere in your gameplay code:

   ```csharp
   var hud = Scene.GetAllComponents<HealthBarController>().First();
   hud.TakeDamage( 25f );   // -25 HP, bar drops to 75%
   hud.Heal( 10f );         // +10 HP, bar climbs back up
   hud.Health = 0f;         // bar empties
   ```

## Variables exposed

| Name              | Type     | Role                                                            |
| ----------------- | -------- | --------------------------------------------------------------- |
| `HealthFraction`  | `float`  | Normalized 0..1 value driving the ProgressBar fill.             |
| `HealthLabel`     | `string` | Display string like `"100 / 100"` rendered in the value text.   |

## Bindings

| Element     | Property               | Variable         | Mode    |
| ----------- | ---------------------- | ---------------- | ------- |
| HealthBar   | `ProgressPreviewValue` | `HealthFraction` | OneWay  |
| ValueText   | `Text`                 | `HealthLabel`    | OneWay  |

## Events

None — this is a read-only display. See the `inventory_screen` or
`death_screen` samples for click/hover event wiring.

## Extending it

- **Change the bar color**: edit the `HealthBar` element's `ProgressFillColor`
  in the Designer (try `#ef4444` for red, `#fbbf24` for amber). For a
  health-based color shift, add a converter chain to the binding that maps the
  fraction to a Color.
- **Anchor it elsewhere**: the panel uses `Anchor: TopLeft` with an offset of
  `(32, 32)`. Change the Anchor to `BottomCenter` and zero the offset for a
  fighting-game placement.
- **Add a max-HP buff hook**: expose `MaxHealth` as another SUI Variable, bind
  the right-hand side of the label to it with a `FloatToString` converter, and
  push `Hud.MaxHealth = X` from gameplay code.
- **Animate the fill**: add a second ProgressBar behind this one bound to a
  `LerpedHealthFraction` Variable updated with a slower lerp in `OnUpdate` to
  get the classic "damage trail" effect (red underlay revealed when HP drops
  fast, then catches up).
- **Hide when full**: bind the panel's `Style.Visibility` to a derived
  Variable, or call `Hud.Hide()` / `Hud.Show()` from gameplay when health is
  at 100%.
