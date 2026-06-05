---
layout: default
title: loadout_selector
parent: Samples
nav_order: 11
permalink: /samples/loadout_selector/
---

# loadout_selector
{: .no_toc }

An **Apex / Overwatch-style class picker** showcase for the SUI Designer. Four colour-coded class cards on the left, a live-updating detail panel on the right with name, flavour text, and four stat bars (Health / Speed / Damage / Range), and a green Confirm button at the bottom that locks in the selection and prints the chosen loadout to the console.
{: .fs-6 .fw-300 }

The sample is the natural counterpart to `boss_hp_bar`: where that one was all **OneWay bindings + zero clickable surface**, this one demonstrates the other half of the model — **five Code-mode events** (four class picks plus a confirm) funnelling into controller methods that, in turn, push state to six bound `Variables`. It's the cleanest way to see "click handler in → variables out → bound `ProgressBar` and `Text` redraw" round-trip in a single document.

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What you'll see

A 1100 x 640 card centered on a dim backdrop. The card splits into two halves: **on the left**, a 2x2 grid of bold square class buttons — **ASSAULT** in crimson, **MEDIC** in green, **SNIPER** in blue, **ENGINEER** in amber — each with hover/press scale + tint feedback. **On the right**, a dark detail panel shows the selected class name in big bold caps, a paragraph of flavour description below it, a "STATS" header, and then four labelled `ProgressBar` rows (Health, Speed, Damage, Range) whose fills update instantly as you click between class cards. A green **Confirm Loadout** button sits centered along the bottom edge; clicking it hides the whole card and emits `Log.Info` with the chosen class name so you can see the selection flow back into gameplay code.

## How to use

1. **Drop the controller on any GameObject.** It's `Sandbox.Samples.LoadoutSelectorController`. No inspector knobs are required — the four class definitions are hard-coded in the controller as a `Dictionary<string, ClassDef>` so you can copy-paste a fifth one in minutes.
2. **Hit Play.** The card appears immediately on screen via `Hud.Show( GameObject, SuiInputMode.MouseOnly )` — the cursor is unlocked for the duration so you can click cards without giving up movement input permanently.
3. **Click any class card** to preview that loadout. The right-hand detail panel rewrites itself: name, description, and all four stat bars animate to their new values in a single frame thanks to the OneWay bindings.
4. **Click Confirm** to lock in the selection. The card hides (the controller calls `Hud.Hide()`) and a `Log.Info( $"Loadout confirmed: {SelectedClass}" )` line appears in the console — your real game code would replace that line with whatever "spawn the player with this loadout" call your project uses.

## Variables

| Name | Type | Default | Role |
|---|---|---|---|
| `SelectedClassName` | `string` | `"ASSAULT"` | Display name shown in big caps at the top of the detail panel. Controller writes this in `PushSelectedToHud()` whenever a class card is clicked. |
| `SelectedClassDescription` | `string` | `"Versatile front-line operator. Balanced stats with strong sustain fire."` | Multi-line flavour text shown below the class name. OneWay-bound to `SelectedDescriptionText`. |
| `HealthStatFraction` | `float` | `0.7` | Normalized 0..1 health rating for the selected class. Drives `HealthStatBar` fill. |
| `SpeedStatFraction` | `float` | `0.7` | Normalized 0..1 speed rating for the selected class. Drives `SpeedStatBar` fill. |
| `DamageStatFraction` | `float` | `0.7` | Normalized 0..1 damage rating for the selected class. Drives `DamageStatBar` fill. |
| `RangeStatFraction` | `float` | `0.5` | Normalized 0..1 effective range rating for the selected class. Drives `RangeStatBar` fill. |

All six are `Manual` source — there is no data binding to an inspector property or a resource. The controller owns the truth and pushes via the six wrapper fields whenever a click handler fires. If you later swap the hard-coded dictionary for a `ScriptableObject` (see *Extending it*), the variables stay exactly the same — only `PushSelectedToHud()` changes.

## Bindings

| Element | Property | Variable | Mode | Trigger |
|---|---|---|---|---|
| `SelectedNameText` (Text) | `Text` | `SelectedClassName` | OneWay | OnChange |
| `SelectedDescriptionText` (Text) | `Text` | `SelectedClassDescription` | OneWay | OnChange |
| `HealthStatBar` (ProgressBar) | `Value` | `HealthStatFraction` | OneWay | OnChange |
| `SpeedStatBar` (ProgressBar) | `Value` | `SpeedStatFraction` | OneWay | OnChange |
| `DamageStatBar` (ProgressBar) | `Value` | `DamageStatFraction` | OneWay | OnChange |
| `RangeStatBar` (ProgressBar) | `Value` | `RangeStatFraction` | OneWay | OnChange |

Every binding is `OneWay` because the detail panel is **read-only** — the user never drags a stat bar or edits the class name inline. The four stat bars use the same shape (`ProgressBar.Value` ← `float`) so the matrix at `Code/Runtime/SuiBindingModeMatrix.cs` treats them identically; bindings are validated at load time, so a typo in a variable name would get caught by `SuiDocumentValidator` before the wrapper compiles.

## Events

| Element | Event | Mode | Handler |
|---|---|---|---|
| `ClassCard_1` (Button, ASSAULT) | `OnClick` | Code | `OnAssaultClick` |
| `ClassCard_2` (Button, MEDIC) | `OnClick` | Code | `OnMedicClick` |
| `ClassCard_3` (Button, SNIPER) | `OnClick` | Code | `OnSniperClick` |
| `ClassCard_4` (Button, ENGINEER) | `OnClick` | Code | `OnEngineerClick` |
| `ConfirmButton` (Button) | `OnClick` | Code | `OnConfirmClick` |

> **Note on Code-mode wiring.** The generator emits each OnClick handler as `[Property, Group("Events")] public Action OnAssaultClick { get; set; }` (and the four siblings) on the `LoadoutSelector` wrapper class — **not** as methods name-resolved on the controller. The controller must explicitly assign every delegate *before* `Hud.Show()`:
>
> ```csharp
> Hud.OnAssaultClick  = OnAssaultClick;   // assign ALL FIVE first
> Hud.OnMedicClick    = OnMedicClick;
> Hud.OnSniperClick   = OnSniperClick;
> Hud.OnEngineerClick = OnEngineerClick;
> Hud.OnConfirmClick  = OnConfirmClick;
> Hud.Show( GameObject, SuiInputMode.MouseOnly ); // then mount
> ```
>
> `Show()` triggers `SyncFieldsTo`, which copies the wrapper's delegates into the renderer Panel. Assigning after `Show()` leaves the renderer with `null` for that one button — it still hovers and presses visually (HoverStyle / PressedStyle live on the Button itself) but the click silently no-ops, which is the single most confusing failure mode of the whole sample. See the full pattern in [Events & Actions → Code mode]({% link concepts/events-and-actions.md %}#code-mode).

## Extending it

A few directions worth exploring once you've got it on screen:

1. **Add more classes.** Duplicate any `ClassCard_N` Button in the Designer, give it a new colour scheme, point its `OnClick` at a new `OnXxxClick` handler, add a matching entry to the controller's `_classes` dictionary, and re-tile the 2x2 grid into a 3x2 or 4x2 — the detail panel and bindings don't change at all.
2. **Swap to a data-driven `[Resource]`.** Define a `ClassDefinition` resource (`Name`, `Description`, `Health`, `Speed`, `Damage`, `Range`, `IconPath`, `TintColor`) and a `LoadoutLibrary : GameResource` that holds a `List<ClassDefinition>`. The controller iterates the library and assigns the same five delegates to whichever buttons it spawns — designers can then add classes from the inspector without touching code.
3. **Add a weapon-preview Image.** Drop an `Image` element inside `DetailPanel`, add a `SelectedWeaponIcon : string` variable bound to its `ImagePath`, and set the path from each click handler. Pair it with `FitMode = Contain` so different weapon icon aspect ratios render cleanly without cropping.
4. **Per-stat tooltips on hover.** Wrap each stat row in a Panel with a `TooltipText` ("Health: total HP pool before regeneration kicks in") so hovering Speed for half a second shows the explanation. The `TooltipText` field is already on every element — just fill it per row in the Designer.
5. **Integrate with a real player loadout system.** Replace the `Log.Info` line in `OnConfirmClick` with a `[Broadcast]` RPC that fires `Player.ApplyLoadout( selectedClassName )` on the owning client, then spawn the matching weapon prefab and apply the stat overrides to the player's `CharacterController` (movement speed) and `HealthComponent` (max HP).
6. **Replace text glyphs with actual class portraits.** Set `ButtonText = ""` on each ClassCard and assign a `BackgroundImage` to a per-class portrait PNG instead. Keep the coloured `BackgroundColor` as a thin border ring by switching to a 4px `BorderColor` + transparent fill, so the portrait reads through but the class colour still identifies the card at a glance.

## File map

```
samples/showcase/loadout_selector/
├── loadout_selector.sui            — V3 document (20 elements, 6 variables)
├── LoadoutSelectorController.cs    — Sandbox.Component, ~120 lines
└── README.md                       — this file
```

On first compile inside an s&box project, the Designer's generator drops:

```
Code/Samples/LoadoutSelector/
├── LoadoutSelector.razor           — generated renderer
├── LoadoutSelector.razor.scss      — generated styles
└── LoadoutSelector.cs              — generated wrapper (variables, Action delegates, View ref struct)
```

You only ever hand-edit `LoadoutSelectorController.cs`. The three generated files are regenerated from `loadout_selector.sui` on every doc save and should be treated as build output (don't commit edits to them — the Designer will overwrite).

---

## See also

- [Read the full `loadout_selector` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/loadout_selector)
- [Showcase samples]({% link reference/showcase-samples.md %}) — the full landing page for all 16 V1.5 samples
- [Sample index]({% link reference/sample-index.md %}) — quick-reference index across the catalog
- [Bindings]({% link concepts/bindings.md %}) — how OneWay variables push to `ProgressBar.Value` and `Text`
- [Events & Actions]({% link concepts/events-and-actions.md %}) — Code-mode `OnClick` delegate wiring (the failure mode this sample warns about)
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — what the generator emits for variables and events
