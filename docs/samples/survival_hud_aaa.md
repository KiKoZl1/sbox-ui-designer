---
layout: default
title: survival_hud_aaa
parent: Samples
nav_order: 15
permalink: /samples/survival_hud_aaa/
---

# survival_hud_aaa
{: .no_toc }

A complete five-stat survival HUD — Health, Hunger, Thirst, Body Temperature, Stamina — plus a biome-driven full-screen tint and a red damage-flash overlay. Built entirely as one `.sui` document with twelve Variables, twelve OneWay bindings, and a single companion `Component` that pushes gameplay state into the wrapper each frame.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Pitch

The other showcase samples each highlight one binding feature in isolation (`health_bar` = one ProgressBar + one Variable, `counter_button` = one button + one event, `toggle_pause` = one TwoWay bool, `label_clock` = one polled string). `survival_hud_aaa` is the **integration test**: every Variable type the Designer supports (`float`, `string`, `bool`, `Color`), every common binding target (`ProgressBar.Value`, `Text.Text`, universal `BackgroundColor`, universal `Visibility`), and a real-shape controller that mirrors the layout you'd actually ship in a survival game.

If the five samples above are the unit tests of the SUI Designer runtime, this one is the smoke test for an entire HUD.

---

## What you'll see

In the top-right corner: a dark card with five labelled rows. Each row has a small `[label]` on the left, a coloured `[bar]` on the right, and a `cur/max` readout sitting underneath the label. The rows are colour-coded — red Health, amber Hunger, blue Thirst, violet Temperature, green Stamina — so you can read the player's state at a glance without reading any text.

Beyond the card: the whole screen tints subtly based on `ActiveBiome` (cool blue in `Snow`, warm amber in `Desert`, sickly green in `Swamp`, no tint in `Default`). When `TakeDamage` is called, the entire screen briefly washes red for `DamageFlashDuration` seconds (default 0.3s), then fades back.

Nothing is interactive — this is a passive read-only HUD. The cursor is never captured and gameplay input is untouched.

---

## How to use

1. Open `survival_hud_aaa.sui` in the **SUI Designer** (`Window -> Sbox UI Designer` in the editor), confirm it loads, then click **Compile**. The generator writes `SurvivalHudAaa.cs` + `SurvivalHudAaaPanel.razor` + `SurvivalHudAaaPanel.scss` into `Code/Samples/SurvivalHudAaa/` under the `Sandbox.Samples` namespace.
2. Copy `SurvivalHudAaaController.cs` into your project (anywhere under `Code/`).
3. In any scene, add a new GameObject and attach the **SurvivalHudAaaController** component to it.
4. Hit **Play**. The stats card appears immediately. Tweak `Health`, `Hunger`, `Thirst`, `BodyTemp`, `Stamina`, or `ActiveBiome` from the Inspector and the HUD follows in real time.
5. Drive it from gameplay code:

   ```csharp
   var hud = Scene.GetAllComponents<SurvivalHudAaaController>().First();
   hud.TakeDamage( 25f );                          // red flash + HP drops
   hud.Eat( 30f );                                 // hunger bar climbs
   hud.Drink( 50f );                               // thirst bar climbs
   hud.AdjustTemperature( -0.2f );                 // start to freeze
   hud.ActiveBiome = SurvivalHudAaaController.Biome.Snow; // screen tints blue
   ```

The default `OnUpdate` loop also maps the engine `Score` (Tab) and `Reload` (R) actions to `TakeDamage(10f)` and `Heal(25f)` respectively, so you can verify the damage-flash and the health bar without writing a single line of glue code. Remove those two lines from the controller when you wire it to real gameplay damage sources.

---

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

Each Variable maps 1:1 to a single binding target — no Compose chains, no converter pipelines. The controller does the math (`Health / MaxHealth`) before pushing the float so the document stays declarative.

---

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

All bindings use the default `OnChange` `UpdateTrigger` and have no converters — the Variable types match the property targets exactly (`float` for `ProgressBar.Value`, `string` for `Text.Text`, `Color` for `BackgroundColor`, `bool` for `Visibility`).

---

## Events

**None.** This HUD is read-only — it never reacts to clicks or hover. All updates flow one-way from the controller's `[Property]` fields into the SUI Variables via `PushAll()` on `OnUpdate`. See the `counter_button` sample if you need the "Code-mode event handler must be assigned before `Show()`" pattern.

> If you later add an interactive element (e.g. a "Sleep" button to refill Stamina), remember the Code-mode delegate wiring rule:
>
> ```csharp
> Hud.OnSleepClick = OnSleepClick;   // assign FIRST
> Hud.Show( SuiInputMode.MouseOnly ); // then mount
> ```
>
> `Show()` triggers `SyncFieldsTo`, which copies the wrapper's delegate into the rendered Panel. Assign after `Show()` and the click silently no-ops.

---

## Controller walkthrough

The companion `SurvivalHudAaaController` is a single `Sandbox.Component` that owns the gameplay state and pushes it to the SUI wrapper every tick.

### Inspector-driven `[Property]` fields

The controller exposes one `[Property]` per stat plus its `Max*` counterpart, all grouped under `Stats`, so tweaking values lives in the Inspector by default. `ActiveBiome` lives under `World`, and `DamageFlashDuration` under `Effects`. Designers can validate the whole HUD without touching code.

```csharp
[Property, Range( 0f, 999f ), Group( "Stats" )] public float Health { get; set; } = 100f;
[Property, Range( 1f, 999f ), Group( "Stats" )] public float MaxHealth { get; set; } = 100f;
[Property, Group( "World" )] public Biome ActiveBiome { get; set; } = Biome.Default;
[Property, Range( 0.05f, 1.0f ), Group( "Effects" )] public float DamageFlashDuration { get; set; } = 0.3f;
```

### Passive mount

In `OnStart`, the controller shows the wrapper in `SuiInputMode.Passive` so the HUD never captures the cursor — the player still has full mouse control over gameplay.

```csharp
protected override void OnStart()
{
    Hud.Show( SuiInputMode.Passive );
    PushAll();
}
```

### `PushAll()` — the per-frame sync

`PushAll()` recalculates every fraction, formats every label, and re-evaluates the biome tint in one shot. Called from `OnStart`, from `OnUpdate`, and from every public mutator (`TakeDamage`, `Heal`, `Eat`, `Drink`, `Rest`, `AdjustTemperature`) so the HUD is always coherent — even if you set `Hunger = 0` directly in the Inspector.

```csharp
private void PushAll()
{
    Hud.HealthFraction = MaxHealth > 0f ? Health / MaxHealth : 0f;
    Hud.HealthLabel    = $"{(int)Health}/{(int)MaxHealth}";
    // ...repeat for Hunger / Thirst / Temp / Stamina...
    PushBiomeTint();
}
```

### Biome → Color switch

`PushBiomeTint` maps the controller-local `Biome` enum to a low-alpha `Color`. Alphas land around ~13% so the world stays readable underneath — enough to communicate "I'm in a snow biome" without drowning the geometry.

```csharp
Hud.BiomeTint = ActiveBiome switch
{
    Biome.Snow   => new Color( 0.40f, 0.60f, 0.95f, 0.13f ),
    Biome.Desert => new Color( 0.95f, 0.70f, 0.30f, 0.13f ),
    Biome.Swamp  => new Color( 0.40f, 0.75f, 0.35f, 0.13f ),
    _            => new Color( 0f, 0f, 0f, 0f ),
};
```

### Damage flash timing

`TakeDamage` records the time with `TimeSince` and toggles `Hud.DamageFlashVisible = true`. `OnUpdate` watches for `_timeSinceDamage >= DamageFlashDuration` and flips the flag back to `false`, which the binding then propagates to the root overlay's `Visibility`. The transition fires once on each edge — there's no per-tick push, so the binding pipeline only runs when state actually changes.

```csharp
public void TakeDamage( float amount )
{
    if ( amount <= 0f ) return;
    Health = MathX.Clamp( Health - amount, 0f, MaxHealth );
    _timeSinceDamage = 0f;
    if ( !_flashVisible )
    {
        _flashVisible = true;
        Hud.DamageFlashVisible = true;
    }
    PushAll();
}
```

### Public gameplay API

The controller exposes a clean public surface so gameplay code never reaches into the wrapper directly:

| Method                          | Effect                                                                 |
| ------------------------------- | ---------------------------------------------------------------------- |
| `TakeDamage( float amount )`    | Subtracts from Health and pulses the red overlay.                      |
| `Heal( float amount )`          | Adds to Health, clamped at `MaxHealth`.                                |
| `Eat( float amount )`           | Adds to Hunger, clamped at `MaxHunger`.                                |
| `Drink( float amount )`         | Adds to Thirst, clamped at `MaxThirst`.                                |
| `Rest( float amount )`          | Adds to Stamina, clamped at `MaxStamina`.                              |
| `AdjustTemperature( float dt )` | Shifts `BodyTemp` by `dt` (positive = warmer), clamped to `[0, 1]`.    |

### Cleanup

`OnDestroy` calls `Hud?.Remove()` so the rendered Panel is detached when the GameObject is destroyed — important for scene swaps and Play-mode reloads.

```csharp
protected override void OnDestroy()
{
    Hud?.Remove();
}
```

---

## Extending it

- **Compose chain for the fraction**: today `HealthFraction` is pre-computed in C# (`health / maxHealth`). Once the converter catalog grows a numeric `Divide` step you can swap to two separate `Health:float` + `MaxHealth:float` Variables and let a Compose chain do the math inside the binding — drop the `PushAll` math entirely.
- **Health-based bar colour**: bind `HealthBar.FillColor` to a derived `Color` Variable and tween it from green at 100% to amber at 50% to red at 25%. Either compute it in the controller (`Color.Lerp`) or add a `FloatToColorRamp` converter to a single `HealthFraction` binding.
- **Negative temperature delta visual**: today `TempFraction` is plain 0..1 left-to-right fill. For a "half-fill-from-center" cold/hot indicator, duplicate the bar — one bar with `ProgressDirection: RightToLeft` for cold (0..0.5 inverted), one with `LeftToRight` for hot (0.5..1) — and switch which one is visible based on `BodyTemp < 0.5`.
- **Stamina drain on sprint**: in your player controller, while the sprint input is held, subtract `30f * Time.Delta` from `Stamina` and zero out sprint when it hits 0. Pair with `Rest( 25f * Time.Delta )` while idle to refill.
- **Persistent biome trigger**: add a `BoxCollider` + `BiomeVolume` script that flips `ActiveBiome` on `OnTriggerEntered`. The screen tint follows on the next frame because the controller maps enum -> Color in `PushBiomeTint`.
- **Hunger-tick decay**: in `OnUpdate`, subtract `Time.Delta * 0.5f` from Hunger and Thirst so they decay naturally over a few minutes. Once Hunger hits 0, start chipping at Health each second to model starvation.
- **Different screen anchor**: the card uses `Anchor: TopRight` with offset `(-360, 24)`. Change to `BottomLeft` with `(24, -304)` for a Resident-Evil-style placement, or `MiddleCenter` if you want a Souls-style centred bar that only appears in danger.

---

## File map

| File                                                                                         | Owner          | Role                                                                                          |
| -------------------------------------------------------------------------------------------- | -------------- | --------------------------------------------------------------------------------------------- |
| `survival_hud_aaa.sui`                                                                       | SUI Designer   | The source document — Variables, elements, bindings. Edit in the Designer, never by hand.     |
| `SurvivalHudAaa.cs`                                                                          | Generated      | Wrapper class written by Compile. Re-generated on every Compile — do not edit.                |
| `SurvivalHudAaaPanel.razor`                                                                  | Generated      | Razor template rendered by `Sandbox.UI`. Re-generated on every Compile — do not edit.         |
| `SurvivalHudAaaPanel.scss`                                                                   | Generated      | Stylesheet for the Panel. Re-generated on every Compile — do not edit.                        |
| `SurvivalHudAaaController.cs`                                                                | Your code      | Component that owns gameplay state and pushes it to the wrapper every tick. Edit freely.      |

> Re-running Compile overwrites the three generated files in place. Keep your gameplay logic in the controller, never in the generated `.cs` / `.razor` / `.scss`.

---

## Troubleshooting

| Symptom                                                                | Likely cause                                                                                                          | Fix                                                                                                                                                                          |
| ---------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| HUD doesn't appear when I hit Play                                     | Controller was added but `Hud.Show()` never ran — usually because the GameObject is disabled.                         | Enable the GameObject in the Inspector. If the HUD is still blank, confirm `SurvivalHudAaa.cs` was generated by Compile and lives under `Code/Samples/SurvivalHudAaa/`.        |
| Bars don't move when I change `Health` in the Inspector                | `OnUpdate` isn't running because the Component is disabled or you only called the public API once outside Play.       | Toggle the GameObject off / on, or call `Hud.HealthFraction = ...` directly to confirm the wrapper is alive.                                                                  |
| Damage flash stays red forever                                         | `DamageFlashDuration` was set to `0` (clamped to 0.05) or `_timeSinceDamage` never gets a chance to elapse because something keeps calling `TakeDamage` every tick. | Bump `DamageFlashDuration` to a sane value (~0.3s) and check that no `OnUpdate` in your project calls `TakeDamage` unconditionally.                                            |
| Screen never tints even when I change `ActiveBiome`                    | Wrapper was never mounted (no `Hud.Show()`), or the root canvas isn't the element bound to `BiomeTint`.               | Confirm the root element of the `.sui` is named `Root` and that its `BackgroundColor` is bound to `BiomeTint` (OneWay). Re-Compile after fixing.                              |
| Compile complains about `SurvivalHudAaa` not existing                  | The wrapper hasn't been generated yet — the controller compiles before the SUI Designer writes the `.cs` file.        | Open `survival_hud_aaa.sui` in the Designer and hit **Compile**. The wrapper class lands in `Code/Samples/SurvivalHudAaa/SurvivalHudAaa.cs` and the controller compiles.       |
| HUD captures the mouse and gameplay input dies                         | The Component called `Hud.Show( SuiInputMode.MouseOnly )` or `MouseAndKeyboard` instead of `Passive`.                 | Restore `Hud.Show( SuiInputMode.Passive )` in `OnStart`. The HUD is read-only and should never claim input.                                                                    |
| Adding a Sleep button click does nothing                               | Code-mode delegate was assigned after `Show()` — `SyncFieldsTo` ran with the field still null.                        | Move `Hud.OnSleepClick = OnSleepClick;` above `Hud.Show( ... );`. See the warning under [Events](#events).                                                                     |

---

## See also

- [Source on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/survival_hud_aaa) — full README, `.sui`, and `SurvivalHudAaaController.cs` in the source repo.
- [Showcase samples]({% link reference/showcase-samples.md %}) — gallery landing for every shipped sample, grouped by category.
- [Sample index]({% link reference/sample-index.md %}) — flat catalogue sorted by difficulty.
- [health_bar]({% link samples/health_bar.md %}) — the single-stat starter that this HUD scales up from.
- [boss_hp_bar]({% link samples/boss_hp_bar.md %}) — phase-marker variant with `ExposeAsVariable` Style writes.
- [settings_full]({% link samples/settings_full.md %}) — every input widget plus the `Apply.All` save pattern, for when you need an interactive companion screen.
