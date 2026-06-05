# Health Bar

A minimal player-health HUD that isolates the V1.5 **OneWay Manual-trigger binding** pattern. One `ProgressBar` for the fill, one `Text` label for the readout — both bound to wrapper Variables that the companion `Component` writes every tick. The sample teaches how to drive a generated wrapper from gameplay data without ever touching the razor or scss directly, using the simplest possible binding mode: source-of-truth lives on the controller, the wrapper just reflects it. It is the canonical "read-only HUD" template — copy it whenever you need a stats panel that displays state but never sends input back.

## What you'll see

A small dark card pinned to the top-left of the screen, 32px in from the corner. The card is 256x40, has a translucent near-black background (`#0d0d0fcc`) with an 8px corner radius, and stacks three rows vertically with a 6px gap: a tiny grey uppercase "HP" label, a 16px-tall green progress bar (`#4ade80`) sitting on a dim track, and a right-aligned value text like "100 / 100".

The bar fills left-to-right and updates smoothly as the controller mutates `Health`. The numeric label shadows the fill so you always have an exact readout next to the visual.

## Behavior

1. `OnStart()` on `HealthBarController` calls `Hud.Show( SuiInputMode.Passive )` to mount the wrapper into the SUI hud root. `Passive` means the panel renders but never captures the cursor — the player can shoot through it.
2. `PushToHud()` runs once immediately after mount to seed the Variables from the current `Health` / `MaxHealth` values.
3. Every frame, `OnUpdate()` clamps `MaxHealth` to `>= 1` and `Health` into `[0, MaxHealth]`, then calls `PushToHud()` which writes both Variables.
4. Because both bindings declare `UpdateTrigger = OnChange`, the generated wrapper auto-applies the new values to the bar and label as soon as the setters fire — no manual `Hud.Apply.All()` required.
5. `OnDestroy()` calls `Hud?.Remove()` so the panel is removed cleanly when the component is destroyed or the scene unloads.

## How to use

1. Open `health_bar.sui` in the SUI Designer window (`Window -> Sbox UI Designer`) and hit Compile. This emits `HealthBar.cs`, `HealthBarPanel.razor` and `HealthBarPanel.razor.scss` under `Code/Samples/HealthBar/`.
2. Drop `HealthBarController.cs` into `Code/Samples/HealthBar/` (or anywhere under `Code/`).
3. Attach `HealthBarController` to a GameObject in any scene, hit Play. The HUD mounts automatically. Tweak `Health` / `MaxHealth` from the inspector, or call `TakeDamage(25f)` / `Heal(10f)` from gameplay code.

## Variables

| Name | Type | Default | Role |
|---|---|---|---|
| `HealthFraction` | `float` | `1.0` | Normalized health 0..1 — drives the `ProgressBar.Value` fill. |
| `HealthLabel` | `string` | `"100 / 100"` | Display string — drives the right-aligned `Text.Text`. |

Both Variables are declared `Public = true` and use `Source.Kind = "Manual"` so the wrapper exposes them as auto-properties that the controller writes directly.

## Bindings

| Element | Property | Variable | Mode | Update Trigger |
|---|---|---|---|---|
| `HealthBar` (`ProgressBar`) | `Value` | `HealthFraction` | OneWay | OnChange |
| `ValueText` (`Text`) | `Text` | `HealthLabel` | OneWay | OnChange |

Both bindings flow **wrapper → element** only. `OnChange` means the generated setters call `Apply` for just those two elements every time the Variable is written, so there is zero wasted work and no need to call `Hud.Apply.All()` from the controller.

## Events

None — this is a read-only display. No element declares any event handler.

## Required `User.scss` rules

N/A — fully driven by the generated SCSS. The card background, gap, padding, radius and bar colors are all authored in the Designer and emitted into `HealthBarPanel.razor.scss`. You only need a `HealthBarPanel.User.scss` file if you want to override theme colors without losing them on Force Regen.

## Controller architecture

- `[Property] HealthBar Hud { get; set; } = new()` — the generated wrapper, instantiated inline so the inspector slot is never null.
- `[Property, Range(0f, 999f)] float Health` and `MaxHealth` — the source of truth. The wrapper reads from these, never the other way around.
- `OnStart()` — `Hud.Show( SuiInputMode.Passive )` then `PushToHud()` so the first frame shows correct values.
- `OnUpdate()` — clamps `MaxHealth >= 1`, clamps `Health` into `[0, MaxHealth]` via `MathX.Clamp`, calls `PushToHud()`.
- `OnDestroy()` — `Hud?.Remove()` to tear the panel down.
- `PushToHud()` (private) — computes `HealthFraction = MaxHealth > 0f ? Health / MaxHealth : 0f` and `HealthLabel = $"{(int)Health} / {(int)MaxHealth}"`, assigns both onto `Hud`. The `OnChange` triggers do the rest.
- Public API: `TakeDamage(float amount)` and `Heal(float amount)` — early-out on non-positive input, clamp, then `PushToHud()`. Call these from weapons, pickups, status effects, etc.

No timers, no hotkeys, no input actions, no RPCs, no `[Sync]`, no networking. This is a deliberately single-player display; for multiplayer, drive `Health` from a networked stat component upstream.

## File map

```text
Code/Samples/HealthBar/
  HealthBar.cs                  (generated wrapper — do not edit)
  HealthBarPanel.razor          (generated markup — do not edit)
  HealthBarPanel.razor.scss     (generated styles — do not edit)
  HealthBarController.cs        (you ship this — drives the wrapper)
```

No `HealthBarPanel.User.scss` is shipped — add one only if you need to override the generated styles in a way that survives Force Regen.

## Element tree at a glance

```text
Root (Canvas, 1920x1080, PointerEvents None)
  HealthPanel (VerticalBox, 32,32, 256x40, Column, Gap 6, Padding 16)
    Label (Text "HP", FontSize 12, Bold, #9ca3af)
    HealthBar (ProgressBar, 248x16, Value bound, FillColor #4ade80)
    ValueText (Text bound, FontSize 14, Right-aligned)
```

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Compile errors mentioning `HealthBar` does not exist | Wrapper not generated yet — controller compiled before Designer Compile ran | Open `health_bar.sui` in the Designer and hit Compile. The wrapper is emitted under `Code/Samples/HealthBar/`. |
| Bar never moves even though `Health` changes | Forgot to call `PushToHud()` after writing to `Health` directly, OR `Hud` slot is null | Use `TakeDamage` / `Heal` (they push for you), or call `PushToHud()` yourself. Make sure `Hud` is assigned in the inspector or instantiated inline. |
| Bar is full but label says "0 / 0" | `MaxHealth` was set to 0, so the fraction defaulted to 0 and the label rounded to zero | The controller clamps `MaxHealth >= 1` in `OnUpdate` — make sure `OnUpdate` is running (component enabled, scene playing). |
| Panel never appears on screen | `Hud.Show()` was never called, or the component is disabled | `OnStart` calls `Show`. Confirm the component is enabled, and that the GameObject is not disabled in the hierarchy. |
| HUD lingers after scene reload | Forgot to wire `OnDestroy` to `Hud.Remove()` | The shipped controller does this — if you copy the pattern, keep the `OnDestroy` override. |
| Panel steals mouse clicks | Wrong input mode passed to `Show` | Use `SuiInputMode.Passive`. `Active` is for menus and inventories, not read-only HUD. |

## Extending it

- **Recolor the fill** — change `ProgressFillColor` on the `HealthBar` element in the Designer (e.g. red when low, green when high). For dynamic recolor, bind `FillColor` to a new `Color` Variable and compute it in `PushToHud`.
- **Re-anchor the panel** — drag `HealthPanel` from TopLeft to BottomCenter, or any of the nine anchors. The pivot and offsets update in place; no controller change required.
- **Expose `MaxHealth` to the UI** — add a `MaxHealth` Variable of type `float`, bind it to a third `Text` element with a `FloatToString` converter, and write it from `PushToHud()`.
- **Add a lerped "damage trail" second bar** — duplicate the `ProgressBar`, tint it red, bind it to a new `HealthFractionLagged` Variable, and lerp it toward `HealthFraction` in `OnUpdate`.
- **Hide-when-full** — add a `Visibility` binding on `HealthPanel` driven by a `bool` Variable, or just call `Hud.Hide()` when `Health >= MaxHealth` and `Hud.Show( SuiInputMode.Passive )` when it drops.
- **Drive from a networked stat component** — replace the local `Health` field with a read of `Components.Get<PlayerStats>().Health`. The HUD stays purely presentational.
- **Stack into a multi-stat panel** — copy the sample and add Stamina + Thirst + Temperature bars in the same `VerticalBox`, each with their own Variable + OneWay binding.

## Related

- [`counter_button`](../counter_button/) — the simplest TwoWay pattern (button click → Variable increment), the natural next step after Health Bar.
- [`stat_block`](../stat_block/) — multi-variable read-only display with grouped Variables, builds directly on this sample.
- [`damage_indicator`](../damage_indicator/) — short-lived overlay that pairs well with `TakeDamage` calls from this controller.
