---
layout: default
title: Sample index
parent: Reference
nav_order: 10
---

# Sample index
{: .no_toc }

The SUI Designer ships **16 showcase samples** under `samples/showcase/`. Each sample is the smallest possible end-to-end example of one specific feature or pattern — open the `.sui`, drop the companion `Component` on a `GameObject`, and you have a working UI in seconds.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## How to use this index

Every showcase sample lives in its own folder under `samples/showcase/<name>/` in the source repo and ships with:

- A `.sui` document — the UI definition you open in the Designer.
- A `<Name>Controller.cs` — the companion `Component` that mounts the wrapper and drives the Variables.
- A `README.md` — what it does, how to use it, the Variables / Bindings / Events tables, and ideas for extending it.

Pick the closest sample to what you want to build, read its README, copy the pattern. **The per-sample READMEs are the primary docs** — this page is the catalog that helps you find the right one.

For a deeper, doc-site-native catalog with the Variables / Bindings tables inlined, see [Showcase samples]({% link reference/showcase-samples.md %}).

---

## Starter samples (V1.5)

The smallest possible end-to-end documents. Work through these first if you are new to SUI Designer.

| Sample | What it demonstrates | Difficulty |
|---|---|---|
| [`empty_canvas`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/empty_canvas) | Minimum viable SUI document — `Show()` / `Hide()` lifecycle | Beginner |
| [`label_clock`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/label_clock) | `OneWay` binding from gameplay → UI Variable | Beginner |
| [`counter_button`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/counter_button) | `Button.OnClick` (Code mode) + Variable update from event | Beginner |

---

## Input widget samples (V1.5)

Samples that exercise the V1.5 input widget set and the canonical commit patterns.

| Sample | What it demonstrates | Difficulty |
|---|---|---|
| [`toggle_pause`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/toggle_pause) | `Toggle` `TwoWay` binding to `bool` Variable | Intermediate |
| [`settings_full`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/settings_full) | Full settings dialog: Apply API, all 4 input widgets, dirty-state | Intermediate |

---

## Interactive-state samples (V1.5)

Reactive HUDs that drive multiple bindings from gameplay events.

| Sample | What it demonstrates | Difficulty |
|---|---|---|
| [`health_bar`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/health_bar) | `OneWay` `ProgressBar` binding + `float` Variable | Intermediate |
| [`boss_hp_bar`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/boss_hp_bar) | Single dramatic ProgressBar + phase markers + ExposeAsVariable Style writes | Intermediate |
| [`death_respawn_modal`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/death_respawn_modal) | Apply API + multi-var coordination + countdown + Code-mode events | Intermediate |

---

## Runtime-rendered samples (V1.5)

Samples that AddChild / mutate the visual tree at runtime via `ExposeAsVariable`.

| Sample | What it demonstrates | Difficulty |
|---|---|---|
| [`chat_panel`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/chat_panel) | TextEntry Manual commit + Apply.All + dynamic `Hud.View.MessageList.AddChild` | Advanced |
| [`dialog_system`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/dialog_system) | Branching NPC tree + typewriter text + deferred mutation outside event dispatch | Advanced |
| [`drag_drop_inventory`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/drag_drop_inventory) | Two 4x4 grids with cursor-following ghost + hit-test on mouse-up | Advanced |
| [`inventory_grid_full`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/inventory_grid_full) | InventoryGrid + slot click events + tooltip via Visibility binding | Flagship |
| [`loadout_selector`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/loadout_selector) | Class card grid + selected-detail with 4 stat bars + Confirm event pattern | Advanced |
| [`notification_toast_queue`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/notification_toast_queue) | Stacking auto-dismissing toasts + CSS transitions + frame-staggered class flips | Advanced |
| [`quest_journal`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/quest_journal) | Multi-tab nav via Variable+Visibility + nested detail + ProgressBars per objective | Advanced |

---

## Full-feature showcase (V1.5)

| Sample | What it demonstrates | Difficulty |
|---|---|---|
| [`survival_hud_aaa`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/survival_hud_aaa) | 5 ProgressBars + damage flash + biome tint + every Variable type | Flagship |

---

## See also

- [Showcase samples]({% link reference/showcase-samples.md %}) — the inline catalog with Variables / Bindings tables.
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how a `.sui` becomes the C# `new()`able wrapper the samples mount.
- [Bindings]({% link concepts/bindings.md %}) — the model behind `OneWay` / `TwoWay` / `Compose`.
- [Events & Actions]({% link concepts/events-and-actions.md %}) — what `counter_button` is exercising.
