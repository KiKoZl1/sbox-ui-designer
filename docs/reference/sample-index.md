---
layout: default
title: Sample index
parent: Reference
nav_order: 10
---

# Sample index
{: .no_toc }

Every `.sui` shipped with the project's `Assets/SuiSamples/` folder, indexed by feature. Pick the closest one to what you want to build, open it in the Designer, and learn by remix.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## How to use this index

The Sbox UI Designer ships ~20 sample `.sui` files in `Assets/SuiSamples/`. They double as integration tests + reference implementations for every feature the V1.5 release covers. Most have a companion C# controller under `Code/SuiSamples/`.

The table below groups them by what they showcase. Each row:

- **Sample** — the `.sui` filename.
- **Companion code** — the `.cs` controller if any (under `Code/SuiSamples/` or `Code/BindTest/`).
- **Features exercised** — the V1.5 surfaces the sample touches.

Open a sample with **File → Open** in the Designer (Asset Browser → double-click works too).

---

## Layout + visuals

Foundational samples — drag, drop, position, paint. No bindings, no events.

| Sample | Companion code | Features exercised |
|---|---|---|
| `simple_panel.sui` | — | Single Panel + Text. Smallest end-to-end smoke test. |
| `hotbar_basic.sui` | — | Grid + InventorySlot row. No data wiring. |
| `inventory_basic.sui` | — | Basic InventoryGrid + InventorySlot grid layout. |
| `loot_pickup.sui` | — | Overlay + Image + Text — short transient pickup HUD. |

---

## Bindings + Variables

Samples that exercise the [Bindings]({% link concepts/bindings.md %}) system + Variables.

| Sample | Companion code | Features exercised |
|---|---|---|
| `hud_bindtest.sui` | `Code/BindTest/HudBindtestController.cs` | OneWay bindings, Compose converter, Health/MaxHealth scenario. |
| `hud_stats_v2.sui` | — | Multi-row stats display + bindings on every row. |
| `hud_survival.sui` | — | Survival HUD — hunger/thirst/stamina bars driven from gameplay. |
| `composed_stat_row.sui` | — | Sub-UI exposed for embedding — bind targets for a parent's ForEach. |

---

## Composition + sub-UIs

Samples that exercise [SuiReference]({% link elements/sui-reference.md %}) + nested wrapper hierarchies.

| Sample | Companion code | Features exercised |
|---|---|---|
| `TestParent.sui` | `Code/SuiSamples/TestParentController.cs` | Parent embedding `TestSlot.sui` — basic single-child composition. |
| `TestSlot.sui` | — | The child of `TestParent` — Variables flagged `IsPublic` for the parent's Props editor. |
| `TestGrand.sui` | — | Depth-3 composition — Grand → Parent → Slot. Exercises recursive `ContentHash`. |
| `TestTwins.sui` | `Code/SuiSamples/TestTwinsController.cs` | Two embeds of the same child — Style.ClassName disambiguation (D-010). |
| `TestForEach.sui` | `Code/SuiSamples/TestForEachController.cs` | ForEach over a `List<T>` Variable — member-name matching. |
| `instance_hud.sui` | — | Hud that embeds nested instances of a single sub-UI definition. |

---

## Events + Interaction

Samples that exercise [Events & Actions]({% link concepts/events-and-actions.md %}) — Code mode, Doo mode, `@ref` exposure, M3.5 interactive states.

| Sample | Companion code | Features exercised |
|---|---|---|
| `InteractiveHud.sui` | `Code/BindTest/InteractiveHudController.cs` | Full M3 event story — Code OnClick, Doo OnHover, `@ref` for direct Panel access, M3.5 hover / pressed / disabled / focused states. |
| `EventTest.sui` | — | Smaller event smoke — Code-mode handlers wired across multiple Buttons. |

---

## Input widgets (V1.5 M4)

Samples that exercise the V1.5 M4 input widgets: TextEntry, Slider, Toggle, DropDown + the Apply API.

| Sample | Companion code | Features exercised |
|---|---|---|
| `InputWidgetsShowcase.sui` | `Code/SuiSamples/InputWidgetsShowcaseController.cs` | Every input widget on one panel — TextEntry, Slider with OnRelease + visual buffer, Toggle, DropDown with options list, Apply / Cancel button events. The reference for the Settings Screen tutorial. |

---

## Death / modal patterns

Samples that exercise modal flows, full-screen overlays, and discrete UI moments.

| Sample | Companion code | Features exercised |
|---|---|---|
| `death_screen.sui` | — | Full-screen overlay with respawn / quit buttons. Anchored Stretch. |

---

## Inventory + game UI

Samples that approximate fuller game HUDs.

| Sample | Companion code | Features exercised |
|---|---|---|
| `inventory_screen.sui` | — | Larger inventory + tabbed categories — composition + scroll behaviour. |
| `quest_log.sui` | — | List-style quest log — text-heavy layout + scrolling. |

---

## How to find what you need

Start from the goal:

- **"I want a HUD that displays Hp from gameplay code."** → `hud_bindtest.sui` + the controller.
- **"I want a settings panel with sliders and dropdowns."** → `InputWidgetsShowcase.sui` + the [Settings screen tutorial]({% link tutorials/settings-screen.md %}).
- **"I want a parent panel that embeds N rows of a small sub-UI."** → `TestForEach.sui` for ForEach mechanics, then `composed_stat_row.sui` for a publishable embed.
- **"I want hover / pressed / disabled button states."** → `InteractiveHud.sui` (M3.5 polish).
- **"I want to wire OnClick to a Doo graph."** → `InteractiveHud.sui` (PauseButton uses Doo).
- **"I want to verify deep composition doesn't break reactivity."** → `TestGrand.sui` (depth-3 + each level mutates).

When in doubt, open the sample in the Designer and look at the **Variables** tab + **Events** tab — they're the contract surfaces.

---

## Caveats

- Sample C# controllers live in different folders (`Code/SuiSamples/` for newer ones, `Code/BindTest/` for the V1.5 spike-era ones). The grouping is historical — not architectural.
- Some samples (`hotbar_basic`, `loot_pickup`) have no companion C# because they showcase layout-only patterns.
- The `Test*.sui` cluster is integration-testing-oriented — they exist primarily to exercise the runtime, not as design references. Prefer the named samples (`Hud*`, `Interactive*`, `InputWidgetsShowcase`) when learning a pattern.

---

## See also

- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how samples become C# you can `new()` and `Show()`
- [Composition]({% link concepts/composition.md %}) — recursive ContentHash explained
- [Settings screen tutorial]({% link tutorials/settings-screen.md %}) — guided walk through `InputWidgetsShowcase`
- [Health HUD with converters tutorial]({% link tutorials/health-hud-with-converters.md %}) — guided binding + Compose walk-through
