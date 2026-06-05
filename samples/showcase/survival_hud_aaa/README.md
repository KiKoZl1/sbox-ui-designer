# Survival HUD (AAA)

A complete five-stat survival HUD - Health, Hunger, Thirst, Body Temperature,
Stamina - plus a biome-driven full-screen tint and a red damage-flash overlay.
Built entirely as one `.sui` document with twelve Variables, twelve OneWay
bindings, and a single companion `Component` that pushes gameplay state into
the wrapper each frame.

## Pitch

The other showcase samples each highlight one binding feature in isolation
(`health_bar` = one ProgressBar + one Variable, `counter_button` = one button +
one event, `toggle_pause` = one TwoWay bool, `label_clock` = one polled string).
`survival_hud_aaa` is the **integration test**: every Variable type the
Designer supports (`float`, `string`, `bool`, `Color`), every common binding
target (`ProgressBar.Value`, `Text.Text`, universal `BackgroundColor`,
universal `Visibility`), and a real-shape controller that mirrors the layout
you'd actually ship in a survival game.

If the five samples above are the unit tests of the SUI Designer runtime,
this one is the smoke test for an entire HUD.

## What you'll see

In the top-right corner: a dark card with five labelled rows. Each row has a
small `[label]` on the left, a coloured `[bar]` on the right, and a `cur/max`
readout sitting underneath the label. The rows are colour-coded - red Health,
amber Hunger, blue Thirst, violet Temperature, green Stamina - so you can read
the player's state at a glance without reading any text.

Beyond the card: the whole screen tints subtly based on `ActiveBiome` (cool
blue in `Snow`, warm amber in `Desert`, sickly green in `Swamp`, no tint in
`Default`). When `TakeDamage` is called, the entire screen briefly washes red
for `DamageFlashDuration` seconds (default 0.3s), then fades back.

The HUD itself is read-only — the cursor is never captured and the click
surface is empty. The controller does, however, consume two debug-only
keyboard hotkeys in `OnUpdate` to make the damage-flash + heal pipeline
easy to eyeball without writing gameplay code. See **Debug hotkeys** below;
remove them before shipping.

## How to use

1. Open `survival_hud_aaa.sui` in the **SUI Designer** (`Window -> Sbox UI
   Designer` in the editor), confirm it loads, then click **Compile**. The
   generator writes `SurvivalHudAaa.cs` + `SurvivalHudAaaPanel.razor` +
   `SurvivalHudAaaPanel.scss` into `Code/Samples/SurvivalHudAaa/` under the
   `Sandbox.Samples` namespace.
2. Copy `SurvivalHudAaaController.cs` into your project (anywhere under
   `Code/`).
3. In any scene, add a new GameObject and attach the
   **SurvivalHudAaaController** component to it.
4. Hit **Play**. The stats card appears immediately. Tweak `Health`,
   `Hunger`, `Thirst`, `BodyTemp`, `Stamina`, or `ActiveBiome` from the
   Inspector and the HUD follows in real time.
5. Drive it from gameplay code:

   ```csharp
   var hud = Scene.GetAllComponents<SurvivalHudAaaController>().First();
   hud.TakeDamage( 25f );                          // red flash + HP drops
   hud.Eat( 30f );                                 // hunger bar climbs
   hud.Drink( 50f );                               // thirst bar climbs
   hud.AdjustTemperature( -0.2f );                 // start to freeze
   hud.ActiveBiome = SurvivalHudAaaController.Biome.Snow; // screen tints blue
   ```

## Variables exposed

| Name                  | Type    | Default        | Group   | Role                                                                       |
| --------------------- | ------- | -------------- | ------- | -------------------------------------------------------------------------- |
| `HealthFraction`      | `float` | `1.0`          | Stats   | Normalized 0..1 drive for the red Health bar.                              |
| `HealthLabel`         | `string`| `"100/100"`    | Stats   | `cur/max` text under the Health bar.                                       |
| `HungerFraction`      | `float` | `1.0`          | Stats   | Normalized 0..1 drive for the amber Hunger bar.                            |
| `HungerLabel`         | `string`| `"100/100"`    | Stats   | `cur/max` text under the Hunger bar.                                       |
| `ThirstFraction`      | `float` | `1.0`          | Stats   | Normalized 0..1 drive for the blue Thirst bar.                             |
| `ThirstLabel`         | `string`| `"100/100"`    | Stats   | `cur/max` text under the Thirst bar.                                       |
| `TempFraction`        | `float` | `0.5`          | Stats   | Normalized 0..1 body temperature (0 = freezing, 1 = burning).              |
| `TempLabel`           | `string`| `"Comfortable"`| Stats   | Descriptive temp string (Freezing / Cold / Comfortable / Hot / Burning).   |
| `StaminaFraction`     | `float` | `1.0`          | Stats   | Normalized 0..1 drive for the green Stamina bar.                           |
| `StaminaLabel`        | `string`| `"100/100"`    | Stats   | `cur/max` text under the Stamina bar.                                      |
| `BiomeTint`           | `Color` | `#0d0d0f00`    | World   | Full-screen tint applied to the root Canvas's `BackgroundColor`.           |
| `DamageFlashVisible`  | `bool`  | `false`        | Effects | When true, the full-screen red overlay is `Visible`; otherwise `Hidden`.   |

## Bindings

| Element           | Property          | Variable               | Mode    |
| ----------------- | ----------------- | ---------------------- | ------- |
| `Root` (Canvas)   | `BackgroundColor` | `BiomeTint`            | OneWay  |
| `HealthBar`       | `Value`           | `HealthFraction`       | OneWay  |
| `HealthValueText` | `Text`            | `HealthLabel`          | OneWay  |
| `HungerBar`       | `Value`           | `HungerFraction`       | OneWay  |
| `HungerValueText` | `Text`            | `HungerLabel`          | OneWay  |
| `ThirstBar`       | `Value`           | `ThirstFraction`       | OneWay  |
| `ThirstValueText` | `Text`            | `ThirstLabel`          | OneWay  |
| `TempBar`         | `Value`           | `TempFraction`         | OneWay  |
| `TempValueText`   | `Text`            | `TempLabel`            | OneWay  |
| `StaminaBar`      | `Value`           | `StaminaFraction`      | OneWay  |
| `StaminaValueText`| `Text`            | `StaminaLabel`         | OneWay  |
| `DamageFlash`     | `Visibility`      | `DamageFlashVisible`   | OneWay  |

All bindings use the default `OnChange` `UpdateTrigger` and have no
converters - the Variable types match the property targets exactly (`float`
for `ProgressBar.Value`, `string` for `Text.Text`, `Color` for
`BackgroundColor`, `bool` for `Visibility`).

## Events

**No SUI element events.** The HUD never reacts to clicks or hover — all
state flows one-way from the controller's `[Property]` fields into the SUI
Variables via `PushAll()` on `OnUpdate`. The controller does swallow two
keyboard actions in `OnUpdate` for testing (see **Debug hotkeys** below),
but those are debug-only and should be stripped before shipping. See
`counter_button` if you need the "Code-mode event handler must be assigned
before `Show()`" pattern for a real interactive element.

> If you later add an interactive element (e.g. a "Sleep" button to refill
> Stamina), remember the Code-mode delegate wiring rule:
>
> ```csharp
> Hud.OnSleepClick = OnSleepClick;   // assign FIRST
> Hud.Show( SuiInputMode.MouseOnly ); // then mount
> ```
>
> `Show()` triggers `SyncFieldsTo`, which copies the wrapper's delegate into
> the rendered Panel. Assign after `Show()` and the click silently no-ops.

## Debug hotkeys

These are wired by the sample's controller for testing only — **debug-only,
remove before shipping.**

| Key | Action | What it does |
|---|---|---|
| `Tab` | `Score` | Calls `TakeDamage( 10f )` — subtracts 10 HP, resets `_timeSinceDamage`, and pulses the red `DamageFlashVisible` full-screen overlay for `DamageFlashDuration` seconds (default 0.3s). |
| `R` | `Reload` | Calls `Heal( 25f )` — adds 25 HP clamped at `MaxHealth`. |

Both hijack default s&box input actions (`Score` = Tab, `Reload` = R), so
shipping with them wired means every weapon reload silently tops the player
up by 25 HP. Search the controller for `Input.Pressed` to find these and
remove.

## File map

```text
Code/Samples/SurvivalHudAaa/
  SurvivalHudAaa.cs                    (generated wrapper - do not edit)
  SurvivalHudAaaPanel.razor          (generated markup - do not edit)
  SurvivalHudAaaPanel.razor.scss     (generated styles - do not edit)
  SurvivalHudAaaController.cs        (you ship this - drives the wrapper)
```


## Extending it

- **Compose chain for the fraction**: today `HealthFraction` is pre-computed
  in C# (`health / maxHealth`). Once the converter catalog grows a numeric
  `Divide` step you can swap to two separate `Health:float` + `MaxHealth:float`
  Variables and let a Compose chain do the math inside the binding - drop the
  `PushAll` math entirely.
- **Health-based bar colour**: bind `HealthBar.FillColor` to a derived `Color`
  Variable and tween it from green at 100% to amber at 50% to red at 25%.
  Either compute it in the controller (`Color.Lerp`) or add a
  `FloatToColorRamp` converter to a single `HealthFraction` binding.
- **Negative temperature delta visual**: today `TempFraction` is plain 0..1
  left-to-right fill. For a "half-fill-from-center" cold/hot indicator,
  duplicate the bar - one bar with `ProgressDirection: RightToLeft` for cold
  (0..0.5 inverted), one with `LeftToRight` for hot (0.5..1) - and switch
  which one is visible based on `BodyTemp < 0.5`.
- **Stamina drain on sprint**: in your player controller, while the sprint
  input is held, subtract `30f * Time.Delta` from `Stamina` and zero out
  sprint when it hits 0. Pair with `Rest( 25f * Time.Delta )` while idle to
  refill.
- **Persistent biome trigger**: add a `BoxCollider` + `BiomeVolume` script
  that flips `ActiveBiome` on `OnTriggerEntered`. The screen tint follows on
  the next frame because the controller maps enum -> Color in `PushBiomeTint`.
- **Hunger-tick decay**: in `OnUpdate`, subtract `Time.Delta * 0.5f` from
  Hunger and Thirst so they decay naturally over a few minutes. Once Hunger
  hits 0, start chipping at Health each second to model starvation.
- **Different screen anchor**: the card uses `Anchor: TopRight` with offset
  `(-360, 24)`. Change to `BottomLeft` with `(24, -304)` for a
  Resident-Evil-style placement, or `MiddleCenter` if you want a Souls-style
  centred bar that only appears in danger.
