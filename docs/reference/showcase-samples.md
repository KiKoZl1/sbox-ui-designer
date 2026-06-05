---
layout: default
title: Showcase samples
parent: Reference
nav_order: 11
---

# Showcase samples
{: .no_toc }

The 16 showcase samples shipped with V1.5 — 5 beginner ones that isolate a single SUI Designer concept each, 3 intermediate samples that wire multiple features onto realistic surfaces, and 8 advanced samples that combine the full runtime into game-flow surfaces (modals, multi-tab navigation, dramatic single-element drives, chat history, class pickers, dialog trees, drag-and-drop, and stacking toast queues). Open the `.sui`, drop the companion `Component` on a `GameObject`, and you have a working UI in seconds.
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

## death_respawn_modal

Full-screen "you died" overlay with a crimson 96px headline, a cause-of-death blurb, a `kills / time alive / distance` stats card, a live amber respawn countdown, and primary `RESPAWN` + secondary `Spectate` buttons. The Apex/Souls death modal in one `.sui` plus a ~245-line controller. Demonstrates the **single-variable button-text swap** pattern (one bound string flips between `"Waiting (Ns)..."` and `"RESPAWN"` instead of authoring a second button + an enabled flag), multi-variable coordination via `PushAll()` per `OnUpdate`, and a `K` (Kill) test hotkey that re-triggers the modal with the current inspector values so you can iterate on visuals without dying in real gameplay.

**What you'll see** — a near-black full-screen scrim drops in. The crimson `YOU DIED` headline (96pt bold) sits centred on screen, with a muted slate `Killed by Unknown` cause-of-death line below it. A 600 x 200 rounded slate stats card lists three `label: value` pairs — kills (green), time alive (Mm Ss), distance (m). Below the card, an amber 32pt countdown ticks `Respawn in 3...` → `Respawn in 2...` → `Respawn in 1...` → `Ready!`. Stacked at the bottom: a green primary RESPAWN button (240 x 56) above a dark grey Spectate button (200 x 40). While the countdown is running the primary button reads `Waiting (Ns)...` and clicks silently no-op; once it hits zero the label flips to `RESPAWN` and clicking fires the respawn handler. `Hud.Show(GameObject, SuiInputMode.MouseOnly)` grabs mouse input for the buttons without stealing keyboard focus, so chat/console keep working underneath.

### Variables

| Name | Type | Default | Role |
|------|------|---------|------|
| `DeathCauseText` | `string` | `"Killed by Unknown"` | Cause-of-death blurb under the headline. Controller forwards the `DeathCause` `[Property]`, normalized to "Killed by Unknown" when blank. |
| `KillsThisLifeText` | `string` | `"0"` | Formatted kills count for the stats card. Controller writes `KillsThisLife.ToString()`. |
| `TimeAliveText` | `string` | `"0m 0s"` | Pre-formatted survival time. Controller computes from `TimeAliveSeconds` via `FormatTime` (`"Mm Ss"`). |
| `DistanceText` | `string` | `"0m"` | Pre-formatted distance traveled. Controller computes from `DistanceTraveled` via `FormatDistance` (source units / 39.37 → metres). |
| `CountdownDisplayText` | `string` | `"Respawn in 3..."` | Amber line above the buttons. Controller updates each tick: `"Respawn in Ns..."` while waiting, `"Ready!"` when zero. |
| `RespawnButtonText` | `string` | `"Waiting..."` | Label of the primary button. Bound OneWay to `RespawnButton.ButtonText`. Controller flips between `"Waiting (Ns)..."` and `"RESPAWN"` — the single-variable swap that avoids needing a second button or an `IsDisabled` binding. |

### Bindings

| Element | Property | Variable | Mode | Trigger |
|---------|----------|----------|------|---------|
| `DeathCause` (Text) | `Text` | `DeathCauseText` | OneWay | OnChange |
| `StatKillsValue` (Text) | `Text` | `KillsThisLifeText` | OneWay | OnChange |
| `StatTimeValue` (Text) | `Text` | `TimeAliveText` | OneWay | OnChange |
| `StatDistanceValue` (Text) | `Text` | `DistanceText` | OneWay | OnChange |
| `CountdownText` (Text) | `Text` | `CountdownDisplayText` | OneWay | OnChange |
| `RespawnButton` (Button) | `ButtonText` | `RespawnButtonText` | OneWay | OnChange |

Every binding is OneWay because the modal is read-only — nothing here is ever edited by the player. The stat row *labels* (`StatKillsLabel`, `StatTimeLabel`, `StatDistanceLabel`) are intentionally **unbound** — they're authored as static design text because the labels never change, only the values do.

### Events

| Element | Event | Mode | Handler |
|---------|-------|------|---------|
| `RespawnButton` (Button) | `OnClick` | Code | `OnRespawnClick` (gated on `_canRespawn` — silent no-op while the countdown is still running) |
| `SpectateButton` (Button) | `OnClick` | Code | `OnSpectateClick` (unconditional) |

Both delegates must be assigned **before** `Hud.Show(...)` — `Show` triggers `SyncFieldsTo`, which copies the wrapper's Action delegates into the live Panel. Anything assigned after `Show` lands on the wrapper but not the renderer and the click silently no-ops forever. Same gotcha that `counter_button` and `settings_full` document.

[Read the full `death_respawn_modal` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/death_respawn_modal).

---

## quest_journal

A centred RPG-style quest journal card — three tab buttons across the top (Active / Completed / Failed), a quest list column on the left with three card slots, and a detail column on the right showing the selected quest's title, long description, three objective rows (label + `ProgressBar` each), and a reward line. The flagship showcase for the **runtime restyle via `Hud.View?.*` Panel refs** pattern (six different buttons marked `ExposeAsVariable=true` so the controller can flip background colour / font colour / font weight to communicate selection state without bouncing through CSS classes or authoring a state Variable per button), and for the **single SUI doc + many backing rows** pattern (the document only knows 10 Variables for "the currently selected quest's fields" — the controller is free to swap which quest is selected via `PushSelectedQuest()` without the document having to model a `List<Quest>`).

**What you'll see** — a near-black scrim covers the screen. Centred on top sits a 1100 x 720 rounded slate card. Title `Quest Journal` in bold 28pt white across the top, then three tab buttons (Active / Completed / Failed) — Active is green-on-black, the other two grey-on-slate by default. Below the tabs, the left column shows three quest-card buttons stacked vertically with their titles; the first card is darkened to indicate selection. The right column shows the selected quest's title in 22pt, a wrapped 14pt slate description, then `OBJECTIVES` as a tracked uppercase header followed by three rows of `label (progress bar)` pairs. At the bottom of the right column, `REWARDS` sits above a single line listing gold + items. Clicking any tab restyles the tab strip green/grey and refreshes the quest list; clicking any quest card restyles the cards (selected = darker bg + white text) and rewrites the entire detail column. Test hotkeys: **Tab** (`Score`) bumps objective #1 forward by 10%; **R** (`Reload`) resets every objective on the current tab back to zero.

### Variables

| Name | Type | Default | Role |
|------|------|---------|------|
| `CurrentTabText` | `string` | `"Active"` | Which tab is currently shown — "Active" / "Completed" / "Failed". Controller writes `tab.ToString()` in `SwitchTab`. Available for any UI that wants to mirror the active tab label. |
| `SelectedQuestTitle` | `string` | `"Slay 10 Zombies"` | Title of the currently selected quest. OneWay-bound to `QuestTitleLabel`. Controller writes via `PushSelectedQuest()` after every selection change. |
| `SelectedQuestDescription` | `string` | `"Reports of zombie sightings near the village graveyard. Clear them out."` | Long-form description. OneWay-bound to `QuestDescriptionLabel`. |
| `Objective1Text` | `string` | `"Slay zombies (3/10)"` | Label of objective #1. Empty leaves the row blank when the quest has fewer than 1 objective. |
| `Objective1Progress` | `float` | `0.3` | Progress 0..1 for objective #1. Bound to `Obj1Bar` (ProgressBar Min=0 Max=1). |
| `Objective2Text` | `string` | `"Return to Mayor"` | Label of objective #2. |
| `Objective2Progress` | `float` | `0` | Progress 0..1 for objective #2. |
| `Objective3Text` | `string` | `""` | Label of objective #3. Empty when the quest has fewer than 3 objectives. |
| `Objective3Progress` | `float` | `0` | Progress 0..1 for objective #3. |
| `SelectedQuestRewardText` | `string` | `"300 gold + Iron Sword"` | Gold + items the player earns on completion. OneWay-bound to `RewardLabel`. |

The data model (`Quest` and `Objective` POCOs) is C#-only — `List<custom-POCO>` isn't a valid SUI Variable type, so the doc only knows the "currently selected" projection and the controller maps the selected index → those flat fields.

### Bindings

| Element | Property | Variable | Mode | Trigger |
|---------|----------|----------|------|---------|
| `QuestTitleLabel` (Text) | `Text` | `SelectedQuestTitle` | OneWay | OnChange |
| `QuestDescriptionLabel` (Text) | `Text` | `SelectedQuestDescription` | OneWay | OnChange |
| `Obj1Text` (Text) | `Text` | `Objective1Text` | OneWay | OnChange |
| `Obj1Bar` (ProgressBar) | `Value` | `Objective1Progress` | OneWay | OnChange |
| `Obj2Text` (Text) | `Text` | `Objective2Text` | OneWay | OnChange |
| `Obj2Bar` (ProgressBar) | `Value` | `Objective2Progress` | OneWay | OnChange |
| `Obj3Text` (Text) | `Text` | `Objective3Text` | OneWay | OnChange |
| `Obj3Bar` (ProgressBar) | `Value` | `Objective3Progress` | OneWay | OnChange |
| `RewardLabel` (Text) | `Text` | `SelectedQuestRewardText` | OneWay | OnChange |

Nine OneWay bindings cover the entire detail column. The tab and quest-card button labels are **not** bound — they're mutated inline via `Hud.View?.QuestCard1.ChildrenOfType<Label>().FirstOrDefault().Text = ...` in `SetQuestCardText()` (the runtime-mutation path), avoiding 3+ string Variables that would only ever label the static slots.

### Events

| Element | Event | Mode | Handler |
|---------|-------|------|---------|
| `TabActive` (Button) | `OnClick` | Code | `OnTabActiveClick` |
| `TabCompleted` (Button) | `OnClick` | Code | `OnTabCompletedClick` |
| `TabFailed` (Button) | `OnClick` | Code | `OnTabFailedClick` |
| `QuestCard1` (Button) | `OnClick` | Code | `OnQuest1Click` |
| `QuestCard2` (Button) | `OnClick` | Code | `OnQuest2Click` |
| `QuestCard3` (Button) | `OnClick` | Code | `OnQuest3Click` |

Tab handlers call `SwitchTab(QuestTab)` which updates `_currentTab`, rebuilds the quest list labels, re-pushes the selected quest, and restyles both the tabs and the cards. Quest-card handlers call `SelectQuest(int)` which updates `_selectedIndex`, re-pushes the selected quest, and restyles the card column. All six delegates must be on the wrapper **before** `Hud.Show()` for the same `SyncFieldsTo` reason documented in `death_respawn_modal`.

[Read the full `quest_journal` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/quest_journal).

---

## boss_hp_bar

A dramatic, **Dark-Souls style centred boss HP bar** — crimson fill, three phase divider ticks, an amber `Phase X/Y` caption underneath, and a full-bar white damage flash that pulses on every hit. The cleanest demonstration of how **a single dramatic `ProgressBar` + two `Text` bindings + one `ExposeAsVariable` overlay** can carry an entire boss-fight HUD without per-frame `Children[n].Style = ...` poking. Pairs the bound `HpFraction` with an Absolute-mode container that stacks the bar fill, divider ticks, and the white flash overlay via `ZIndex` so future renames don't quietly break the silhouette. Test hotkeys: **Tab** (`Score`) deals 15% damage and flashes the overlay; **R** (`Reload`) heals 10% silently so heal reads visually different from damage.

**What you'll see** — a 1200 x 100 panel centred against the top of the screen, dark semi-transparent backing with a faint crimson outline. The boss name `ANCIENT DRAGON` sits in bold tracked-out crimson caps at the top, then a 1152 x 24 horizontal HP bar fills with crimson and empties left-to-right as the boss takes damage. Three black divider ticks at 25 / 50 / 75 split the bar into four phases. An amber `Phase X/4` caption sits below the bar, ticking up as the boss drops through phases. Every time you press Tab, the bar shrinks, the white overlay pulses from a 0.7 opacity peak and decays back to invisible over roughly 0.2 seconds, and the phase label updates when you cross a divider. `Hud.Show(SuiInputMode.Passive)` keeps the bar render-only so it never grabs your crosshair.

### Variables

| Name | Type | Default | Role |
|------|------|---------|------|
| `BossName` | `string` | `"ANCIENT DRAGON"` | Display name. Controller uppercases the inspector value once in `OnStart`. |
| `HpFraction` | `float` | `1.0` | Normalized 0..1 health. Controller computes `CurrentHp / MaxHp` every tick. |
| `PhaseLabel` | `string` | `"Phase 1/4"` | Pre-formatted phase string. Controller computes from `ceil(HpFraction * Phases)`. |

### Bindings

| Element | Property | Variable | Mode | Trigger |
|---------|----------|----------|------|---------|
| `BossNameText` (Text) | `Text` | `BossName` | OneWay | OnChange |
| `HpBar` (ProgressBar) | `Value` | `HpFraction` | OneWay | OnChange |
| `PhaseLabel` (Text) | `Text` | `PhaseLabel` | OneWay | OnChange |

The white `DamageFlash` overlay is **not** bound — it's `ExposeAsVariable=true` instead so the controller can write `Hud.View?.DamageFlash.Style.Opacity` directly each tick in the decay curve. Three reasons it bypasses the binding model: there's no `Style.Opacity` bindable in the matrix (opacity is a Panel style attribute, not a Variable-driven property); the decay curve runs in `OnUpdate` and writing a OneWay variable every frame churns the binding pipeline for a single attribute; and `Hud.View?.DamageFlash` survives frame zero gracefully via null-coalescing access (the View is null until Razor mounts the renderer on the first render pass).

### Events

**None.** This sample has no Code-mode events. Damage and heal are triggered by polling `Input.Pressed("Score")` / `Input.Pressed("Reload")` from `OnUpdate`, not from the UI itself — that keeps the HUD pure (no clickable surface) and means the bar never has to compete with weapon fire for input focus.

[Read the full `boss_hp_bar` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/boss_hp_bar).

---

## chat_panel

A compact in-game **chat panel** — one dark card in the bottom-left corner with a scrollable message history, a typed input, and a green Send button. The smallest runtime-driven sample shipping in V1.5: **two `ExposeAsVariable` elements** (one `Panel`, one `TextEntry`), **one Manual TextEntry binding**, **one Code-mode click event**, and a runtime list of `Label` children rebuilt every send via `Hud.View?.MessageList.DeleteChildren(true)` + `AddChild<Label>()`. Demonstrates the canonical "type then submit" flow — TextEntry in `Manual` mode so the draft sits on the widget until the controller calls `Hud.Apply.All()` on Send, instead of churning the wrapper field on every keystroke. Comes with a `/me` (amber emote) and `/system` (green system) command parser, an `R` (`Reload`) Enter-fallback hotkey so the sample works on any project without an `actions.cfg` edit, and a `MaxMessages = 64` cap to keep the runtime panel from growing unbounded.

**What you'll see** — a 480 x 320 dark card with rounded corners and a thin grey border anchored to the bottom-left of the screen, 24px from the edge and 20px up. Top-left of the card reads `Chat` in bold grey; top-right reads `0 messages` (then `1 message`, `2 messages`, …) as you interact. Below the header is a 240px-tall message list with `Overflow=Hidden` that clips the oldest messages once the list outgrows the card. Beneath that is a 28px-tall darker bar with the text input on the left and a green Send button on the right (1.03× hover, 0.97× pressed, green→darker-green tint). A green seed message — `[00:00] system: Welcome - type a message and hit Send (or press R).` — fills the panel on mount so it never looks blank. Every send appends a `[MM:SS] author: text` row below.

### Variables

| Name | Type | Default | Role |
|------|------|---------|------|
| `ChatInputText` | `string` | `""` | The current draft in the input field. **TwoWay** + **Manual** — the wrapper field only catches up to the live widget when the controller calls `Hud.Apply.All()` (which `OnSendClick` does on every send). |
| `MessageCountText` | `string` | `"0 messages"` | Header counter ("12 messages"). **OneWay**-bound to `MsgCountText`. Controller writes from `_messages.Count` at the end of `RenderMessages()` with singular/plural handling. |

### Bindings

| Element | Property | Variable | Mode | Update Trigger |
|---------|----------|----------|------|----------------|
| `ChatInput` (TextEntry) | `Value` | `ChatInputText` | TwoWay | **Manual** |
| `MsgCountText` (Text) | `Text` | `MessageCountText` | OneWay | OnChange |

`Manual` on the input is intentional — a chat send is the canonical "draft → submit" pattern. `OnChange` would commit the draft on every keystroke and clash with the Send button's flush. The wrapper exposes a per-element `Apply.ChatInputValue()` plus a catch-all `Apply.All()` for this exact flow.

Two elements are flagged `ExposeAsVariable=true`: `MessageList` (Panel) so the controller can `DeleteChildren(true)` + `AddChild<Label>()` per render plus `TryScrollToBottom()` to keep the newest message in view; and `ChatInput` (TextEntry) so the controller can call `Focus()` after each send to return the keyboard caret without the player clicking again.

### Events

| Element | Event | Mode | Handler |
|---------|-------|------|---------|
| `SendButton` (Button) | `OnClick` | Code | `OnSendClick` (runs `Hud.Apply.All()`, trims the draft, exits if empty, parses `/me` and `/system`, appends a `ChatMessage`, trims to `MaxMessages`, re-renders, clears the input, refocuses `ChatInput`) |

The delegate must be assigned **before** `Hud.Show(GameObject, SuiInputMode.All)` (`All` mode is required so the TextEntry can receive keyboard input) — same `SyncFieldsTo` gotcha as the other Code-mode samples.

[Read the full `chat_panel` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/chat_panel).

---

## loadout_selector

An **Apex / Overwatch-style class picker** — four colour-coded class cards on the left (Assault crimson, Medic green, Sniper blue, Engineer amber), a live-updating detail panel on the right with name, flavour text, and four `ProgressBar` stat rows (Health / Speed / Damage / Range), and a green Confirm button at the bottom that locks in the selection and prints the chosen loadout to the console. The natural counterpart to `boss_hp_bar`: where that one was all OneWay bindings + zero clickable surface, this one demonstrates the other half of the model — **five Code-mode events** (four class picks plus a confirm) funnelling into controller methods that push state into six bound Variables. The cleanest way to see "click handler in → variables out → bound `ProgressBar` and `Text` redraw" round-trip in a single document.

**What you'll see** — a 1100 x 640 card centred on a dim backdrop, splitting into two halves. On the left, a 2x2 grid of bold square class buttons — ASSAULT in crimson, MEDIC in green, SNIPER in blue, ENGINEER in amber — each with hover/press scale + tint feedback. On the right, a dark detail panel shows the selected class name in big bold caps, a paragraph of flavour description below it, a `STATS` header, then four labelled `ProgressBar` rows whose fills update instantly as you click between class cards. A green `Confirm Loadout` button sits centred along the bottom edge; clicking it hides the whole card via `Hud.Hide()` and emits `Log.Info($"Loadout confirmed: {SelectedClass}")` so you can see the selection flow back into gameplay code. `Hud.Show(GameObject, SuiInputMode.MouseOnly)` unlocks the cursor for the duration so you can click cards without permanently giving up movement input.

### Variables

| Name | Type | Default | Role |
|------|------|---------|------|
| `SelectedClassName` | `string` | `"ASSAULT"` | Display name shown in big caps at the top of the detail panel. Controller writes this in `PushSelectedToHud()` whenever a class card is clicked. |
| `SelectedClassDescription` | `string` | `"Versatile front-line operator. Balanced stats with strong sustain fire."` | Multi-line flavour text shown below the class name. OneWay-bound to `SelectedDescriptionText`. |
| `HealthStatFraction` | `float` | `0.7` | Normalized 0..1 health rating for the selected class. Drives `HealthStatBar` fill. |
| `SpeedStatFraction` | `float` | `0.7` | Normalized 0..1 speed rating for the selected class. Drives `SpeedStatBar` fill. |
| `DamageStatFraction` | `float` | `0.7` | Normalized 0..1 damage rating for the selected class. Drives `DamageStatBar` fill. |
| `RangeStatFraction` | `float` | `0.5` | Normalized 0..1 effective range rating for the selected class. Drives `RangeStatBar` fill. |

All six are `Manual`-source — there is no data binding to an inspector property or a resource. The controller owns the truth (a hard-coded `Dictionary<string, ClassDef>`) and pushes via the six wrapper fields whenever a click handler fires.

### Bindings

| Element | Property | Variable | Mode | Trigger |
|---------|----------|----------|------|---------|
| `SelectedNameText` (Text) | `Text` | `SelectedClassName` | OneWay | OnChange |
| `SelectedDescriptionText` (Text) | `Text` | `SelectedClassDescription` | OneWay | OnChange |
| `HealthStatBar` (ProgressBar) | `Value` | `HealthStatFraction` | OneWay | OnChange |
| `SpeedStatBar` (ProgressBar) | `Value` | `SpeedStatFraction` | OneWay | OnChange |
| `DamageStatBar` (ProgressBar) | `Value` | `DamageStatFraction` | OneWay | OnChange |
| `RangeStatBar` (ProgressBar) | `Value` | `RangeStatFraction` | OneWay | OnChange |

Every binding is OneWay because the detail panel is read-only — the user never drags a stat bar or edits the class name inline.

### Events

| Element | Event | Mode | Handler |
|---------|-------|------|---------|
| `ClassCard_1` (Button, ASSAULT) | `OnClick` | Code | `OnAssaultClick` |
| `ClassCard_2` (Button, MEDIC) | `OnClick` | Code | `OnMedicClick` |
| `ClassCard_3` (Button, SNIPER) | `OnClick` | Code | `OnSniperClick` |
| `ClassCard_4` (Button, ENGINEER) | `OnClick` | Code | `OnEngineerClick` |
| `ConfirmButton` (Button) | `OnClick` | Code | `OnConfirmClick` |

All five delegates must be assigned **before** `Hud.Show(GameObject, SuiInputMode.MouseOnly)`. Assigning after `Show()` leaves the renderer with `null` for that one button — it still hovers and presses visually (HoverStyle / PressedStyle live on the Button itself) but the click silently no-ops, which is the single most confusing failure mode of the whole sample.

[Read the full `loadout_selector` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/loadout_selector).

---

## See also

- [Sample index]({% link reference/sample-index.md %}) — the high-level catalog with difficulty + difficulty groupings.
- [Tutorials]({% link tutorials/index.md %}) — guided walkthroughs that pair with the showcase samples.
- [Bindings]({% link concepts/bindings.md %}) — the model behind `OneWay` / `TwoWay`.
- [Events & Actions]({% link concepts/events-and-actions.md %}) — what `counter_button` is exercising.
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how a `.sui` becomes the C# `new()`able wrapper the samples mount.
