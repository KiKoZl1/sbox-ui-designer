---
layout: default
title: Showcase samples
parent: Reference
nav_order: 11
---

# Showcase samples
{: .no_toc }

The five beginner showcase samples shipped with V1.5 — each one is the smallest possible end-to-end example of a single SUI Designer concept. Open the `.sui`, drop the companion `Component` on a `GameObject`, and you have a working UI in seconds.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

Showcase samples live in [`samples/showcase/`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase) in the source repo. Each sample folder ships a `.sui` document, a companion `<Name>Controller.cs`, and a per-sample `README.md` with the full setup walkthrough.

Work through them in the order presented — they build on each other conceptually, even though each one stands alone code-wise.

---

## empty_canvas

Minimum viable SUI document — proves the wrapper-mounting plumbing works end-to-end before you start adding complexity.

**What you'll see** — a single line of bold white text reading "Hello SUI!" rendered at 48pt in the dead center of the screen. No background, no border, no decoration — just text floating on top of whatever the game is drawing behind it. The panel uses `SuiInputMode.Passive`, so it never steals focus or swallows pointer events.

### Variables

| Name | Type | Role |
|------|------|------|
| _(none)_ | — | This sample intentionally has no Variables. The `"Hello SUI!"` string is baked into the document as a literal Prop. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| _(none)_ | — | — | — |

[Read the full `empty_canvas` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/empty_canvas).

---

## label_clock

`OneWay` binding from gameplay → UI Variable. The simplest useful pattern: one `Text` element bound to a `string` Variable, with a tiny companion `Component` driving the Variable every frame.

**What you'll see** — a small dark pill anchored to the top-right of the screen. The label `Time:` sits above a large monospace value (e.g. `14:07:42`) that ticks every frame. The value text is green-ish (`#4ade80`) over a translucent near-black background with rounded corners. Pure passive readout — nothing reacts to the mouse, nothing blocks input.

### Variables

| Name | Type | Role |
|------|------|------|
| `ClockText` | `string` | The rendered time string. Driven from `OnUpdate` in C#. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| `ClockValue` | `Text` | `ClockText` | OneWay (`UpdateTrigger = OnChange`) |

[Read the full `label_clock` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/label_clock).

---

## health_bar

`OneWay` `ProgressBar` binding + `float` Variable. A minimal player-health HUD — one `ProgressBar` plus two `Text` widgets, bound to two Variables driven by the companion component.

**What you'll see** — a small dark panel anchored to the top-left of the screen with the label `HP`, a green fill bar, and a `current / max` numeric readout. The bar shrinks and the text updates whenever `Health` changes — no manual UI plumbing, just Variable assignments. Call `hud.TakeDamage(25f)` from anywhere in gameplay and the bar drops to 75%.

### Variables

| Name | Type | Role |
|------|------|------|
| `HealthFraction` | `float` | Normalized 0..1 value driving the ProgressBar fill. |
| `HealthLabel` | `string` | Display string like `"100 / 100"` rendered in the value text. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| `HealthBar` | `ProgressPreviewValue` | `HealthFraction` | OneWay |
| `ValueText` | `Text` | `HealthLabel` | OneWay |

[Read the full `health_bar` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/health_bar).

---

## counter_button

`Button.OnClick` (Code mode) + Variable update from event. The smallest possible "click does something, UI reflects it" example — a single button increments a counter stored in C# and displayed via a one-way Variable binding.

**What you'll see** — a small dark card floats in the middle of the screen. It contains a large green `0` and a green `+1` button below it. Every click bumps the number by one and the label updates instantly. The Component's `Count` property is exposed in the Inspector if you want to inspect or seed it from the editor.

### Variables

| Name | Type | Role |
|------|------|------|
| `CountText` | `string` | Stringified counter value. Written by the companion after each click; `CountLabel` binds to it. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| `CountLabel` | `Text` | `CountText` | OneWay |

### Events

| Element | Event | Mode | Handler |
|---------|-------|------|---------|
| `IncrementButton` | `OnClick` | Code | `OnIncrementClick` (resolved on the companion `Component`) |

[Read the full `counter_button` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/counter_button).

---

## toggle_pause

`Toggle` `TwoWay` binding to `bool` Variable. The smallest possible "user input flips a flag, UI reacts" example — a `Toggle` widget is `TwoWay`-bound to a `bool` Variable, and a `Text` element next to it mirrors the toggle state with a coloured label.

**What you'll see** — a small dark bar appears at the top centre of the screen. It contains a "Pause" toggle and a status label. Frame zero the label reads `Running` in green. Click the toggle and the label instantly flips to `Paused`. Click again to flip back. No timers, no animation, no networking — just the binding round-trip.

### Variables

| Name | Type | Role |
|------|------|------|
| `IsPaused` | `bool` | The pause flag. `TwoWay`-bound to the toggle's `Checked` property — the user flips it from the UI, the controller reads it. |
| `StatusText` | `string` | Human-readable mirror of `IsPaused`. Written by the controller in `OnUpdate` whenever `IsPaused` changes. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| `PauseToggle` | `Checked` | `IsPaused` | TwoWay (`UpdateTrigger = OnChange`) |
| `StatusText` | `Text` | `StatusText` | OneWay |

[Read the full `toggle_pause` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/toggle_pause).

---

## settings_full

Full settings dialog with the Apply API, all four V1.5 M4 input widgets, and a live dirty-state indicator. If `counter_button` is the smallest thing that proves the binding loop, this is the smallest thing that proves a real settings screen is shippable without dropping out to Razor.

**What you'll see** — a centred 720×680 dark card titled **Settings** sits over a dimming scrim. The Audio tab is active and shows, top-to-bottom: a `Player Name` TextEntry (prefilled `Player`, max 24 chars), a `Master Volume` Slider 0–100 with a live `50%` readout, a `Music Enabled` Toggle, a `Quality` DropDown (`Low / Medium / High / Ultra`), an empty status line, and a `Reset / Cancel / Apply` button row. Type into the name field — the status line flips to yellow `● Unsaved changes` the moment any value diverges from the last saved snapshot. Apply commits; Cancel reverts to the snapshot; Reset wipes everything back to defaults. Buttons scale 1.03× on hover and 0.97× when pressed.

### Variables

| Name | Type | Default | Role |
|------|------|---------|------|
| `PlayerName` | `string` | `"Player"` | Display name. **TwoWay** + `Manual` — only commits to the wrapper when the controller calls `Apply.All()`. |
| `Volume` | `float` | `50` | Master volume 0–100. **TwoWay** + `OnChange` so `VolumeLabel` updates as you drag. |
| `VolumeLabel` | `string` | `"50%"` | Formatted preview bound **OneWay** to the value text. Controller writes `(int)Hud.Volume + "%"` every frame. |
| `MusicEnabled` | `bool` | `true` | Music on/off. **TwoWay** + `OnChange` — flips immediately when the toggle is clicked. |
| `GraphicsPreset` | `int` | `2` | Quality preset 0–3. **TwoWay** + `OnChange` on `DropDown.Value`. |
| `StatusText` | `string` | `""` | "● Unsaved changes" when dirty, empty when clean. **OneWay** — driven entirely by the controller. |

### Bindings

| Element | Property | Variable | Mode | Update Trigger |
|---------|----------|----------|------|----------------|
| `NameEntry` (TextEntry) | `Value` | `PlayerName` | TwoWay | **Manual** |
| `VolumeSlider` (Slider) | `Value` | `Volume` | TwoWay | OnChange |
| `VolumeValue` (Text) | `Text` | `VolumeLabel` | OneWay | OnChange |
| `MusicToggle` (Toggle) | `Checked` | `MusicEnabled` | TwoWay | OnChange |
| `QualityDropdown` (DropDown) | `Value` | `GraphicsPreset` | TwoWay | OnChange |
| `StatusText` (Text) | `Text` | `StatusText` | OneWay | OnChange |

### Events

| Element | Event | Mode | Handler |
|---------|-------|------|---------|
| `ResetButton` (Button) | `OnClick` | Code | `OnResetClick` |
| `CancelButton` (Button) | `OnClick` | Code | `OnCancelClick` |
| `ApplyButton` (Button) | `OnClick` | Code | `OnApplyClick` |

[Read the full `settings_full` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/settings_full).

---

## inventory_grid_full

Flagship showcase of the **InventoryGrid + InventorySlot** pair plus the **Expose-as-Variable + per-child wire-up** pattern. A 6×4 backpack lands in the middle of the screen with four seeded items; hover surfaces a tooltip, left-click logs a select, right-click drops the item, double-click triggers a use — all driven from a single companion Component.

**What you'll see** — a 640×540 dark card centred on screen behind a translucent black scrim. The header reads **Inventory** with a green subtitle **`4 / 24 slots`** below it. A 6×4 grid of 84px slots fills the middle; the first four slots show a sword, a health potion, a loaf of bread, and a gold coin (rest are empty cells). A thin tooltip strip at the bottom of the card stays hidden until you hover an item. Hover the sword → the tooltip slides in reading `Iron Sword  x1`. Click the potion → console logs `Select slot #1: Health Potion (x5)`. Right-click it → the icon disappears, subtitle ticks down to `3 / 24 slots`. Double-click bread → console logs `Use slot #2: Bread`.

### Variables

| Name | Type | Default | Role |
|------|------|---------|------|
| `SlotCountText` | `string` | `"0 / 24 slots"` | Header subtitle. Controller writes `"<filled> / <capacity> slots"` after every mutation. |
| `ItemTooltip` | `string` | `""` | Tooltip text body. Controller writes `"<name>  x<count>"` on slot hover, clears on unhover. |
| `TooltipVisible` | `bool` | `false` | Flips the tooltip card's `Visibility` (the universal `bool` property). |

> The inventory's `List<ItemEntry>` lives entirely in C# on the controller — V1.5 only supports primitive Variable types (`string` / `int` / `bool` / `float` / `Color`). SUI owns the *view state* (counts, flags, visible strings); the controller owns the *domain state* and pushes view-facing derivatives into Variables.

### Bindings

| Element | Property | Variable | Mode | Notes |
|---------|----------|----------|------|-------|
| `Subtitle` (Text) | `Text` | `SlotCountText` | OneWay | Header subtitle reads `"X / 24 slots"`. |
| `Tooltip` (Panel) | `Visibility` | `TooltipVisible` | OneWay | Universal property; flips between `Visible` and `Hidden`. |
| `TooltipText` (Text) | `Text` | `ItemTooltip` | OneWay | Body of the tooltip card. |

### Events

| Element | Event | Mode | Handler |
|---------|-------|------|---------|
| `BackpackGrid` (InventoryGrid) | `OnClick` | Code | `OnGridClick` (assigned before `Show()`) |
| `BackpackGrid` (InventoryGrid) | `OnRightClick` | Code | `OnGridRightClick` (assigned before `Show()`) |

> Per-slot routing (hover / click / double-click on each of the 24 cells) is wired in C# via the **Expose as Variable** pattern — the controller reaches `Hud.View?.BackpackGrid` after first render, walks `grid.Children`, and assigns listeners per slot with the index captured by closure. The grid-level events declared in the `.sui` are fallbacks for clicks in padding / gaps. See the README's *Why the per-slot wire-up lives in C#* section for the full rationale.

[Read the full `inventory_grid_full` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/inventory_grid_full).

---

## survival_hud_aaa

A complete five-stat survival HUD — Health, Hunger, Thirst, Body Temperature, Stamina — plus a biome-driven full-screen tint and a red damage-flash overlay. Built entirely as one `.sui` document with twelve Variables, twelve OneWay bindings, and a single companion `Component` that pushes gameplay state into the wrapper each frame. If the five beginner samples are the unit tests of the runtime, this one is the smoke test for an entire HUD.

**What you'll see** — top-right corner: a dark card with five labelled rows. Each row has a small `[label]` on the left, a coloured `[bar]` on the right, and a `cur/max` readout under the label. Rows are colour-coded (red Health, amber Hunger, blue Thirst, violet Temperature, green Stamina) so you can read player state at a glance. Beyond the card: the whole screen tints subtly based on `ActiveBiome` — cool blue in `Snow`, warm amber in `Desert`, sickly green in `Swamp`, no tint in `Default`. When `TakeDamage` is called, the entire screen briefly washes red for `DamageFlashDuration` seconds, then fades back. Fully passive — cursor is never captured and gameplay input is untouched.

### Variables

| Name | Type | Default | Role |
|------|------|---------|------|
| `HealthFraction` | `float` | `1.0` | Normalized 0..1 drive for the red Health bar. |
| `HealthLabel` | `string` | `"100/100"` | `cur/max` text under the Health bar. |
| `HungerFraction` | `float` | `1.0` | Normalized 0..1 drive for the amber Hunger bar. |
| `HungerLabel` | `string` | `"100/100"` | `cur/max` text under the Hunger bar. |
| `ThirstFraction` | `float` | `1.0` | Normalized 0..1 drive for the blue Thirst bar. |
| `ThirstLabel` | `string` | `"100/100"` | `cur/max` text under the Thirst bar. |
| `TempFraction` | `float` | `0.5` | Normalized 0..1 body temperature (0 = freezing, 1 = burning). |
| `TempLabel` | `string` | `"Comfortable"` | Descriptive temp string (Freezing / Cold / Comfortable / Hot / Burning). |
| `StaminaFraction` | `float` | `1.0` | Normalized 0..1 drive for the green Stamina bar. |
| `StaminaLabel` | `string` | `"100/100"` | `cur/max` text under the Stamina bar. |
| `BiomeTint` | `Color` | `#0d0d0f00` | Full-screen tint applied to the root Canvas's `BackgroundColor`. |
| `DamageFlashVisible` | `bool` | `false` | When true, the full-screen red overlay is `Visible`; otherwise `Hidden`. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| `Root` (Canvas) | `BackgroundColor` | `BiomeTint` | OneWay |
| `HealthBar` | `Value` | `HealthFraction` | OneWay |
| `HealthValueText` | `Text` | `HealthLabel` | OneWay |
| `HungerBar` | `Value` | `HungerFraction` | OneWay |
| `HungerValueText` | `Text` | `HungerLabel` | OneWay |
| `ThirstBar` | `Value` | `ThirstFraction` | OneWay |
| `ThirstValueText` | `Text` | `ThirstLabel` | OneWay |
| `TempBar` | `Value` | `TempFraction` | OneWay |
| `TempValueText` | `Text` | `TempLabel` | OneWay |
| `StaminaBar` | `Value` | `StaminaFraction` | OneWay |
| `StaminaValueText` | `Text` | `StaminaLabel` | OneWay |
| `DamageFlash` | `Visibility` | `DamageFlashVisible` | OneWay |

All bindings use the default `OnChange` trigger and have no converters — Variable types match the property targets exactly. The README walks through extending this with Compose chains (e.g. `Health:float` + `MaxHealth:float` + a `Divide` step) once the converter catalog grows numeric ops.

### Events

**None.** This HUD is read-only — it never reacts to clicks or hover. All updates flow one-way from the controller's `[Property]` fields into the SUI Variables via `PushAll()` on `OnUpdate`. See `counter_button` if you need the "Code-mode event handler must be assigned before `Show()`" pattern.

[Read the full `survival_hud_aaa` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/survival_hud_aaa).

---

## Coming soon

A second wave of samples is planned to cover intermediate and advanced patterns. None of these ship in V1.5.

**Intermediate** (V1.5.1)

- `chat_panel` — scrollable message log + `TextEntry` submit + RPC fan-out.

**Advanced** (V1.5.1)

- `death_respawn_modal` — full-screen overlay, respawn countdown, two action buttons, modal focus capture.
- `quest_journal` — tabbed quest log with active / completed / failed lists, expandable entries, scroll-to-active.

---

## See also

- [Sample index]({% link reference/sample-index.md %}) — the high-level catalog with difficulty + difficulty groupings.
- [Tutorials]({% link tutorials/index.md %}) — guided walkthroughs that pair with the showcase samples.
- [Bindings]({% link concepts/bindings.md %}) — the model behind `OneWay` / `TwoWay`.
- [Events & Actions]({% link concepts/events-and-actions.md %}) — what `counter_button` is exercising.
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how a `.sui` becomes the C# `new()`able wrapper the samples mount.
