---
layout: default
title: Sample index
parent: Reference
nav_order: 10
---

# Sample index
{: .no_toc }

The SUI Designer ships a curated set of **beginner showcase samples** under `samples/showcase/`. Each sample is the smallest possible end-to-end example of one specific feature — open the `.sui`, drop the companion `Component` on a `GameObject`, and you have a working UI in seconds.
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

## Beginner showcase samples (V1.5)

The five baseline samples cover the entire "Variable → Binding → Event" loop one concept at a time. Work through them in order if you're new to SUI Designer.

| Sample | What it demonstrates | Difficulty |
|---|---|---|
| [`empty_canvas`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/empty_canvas) | Minimum viable SUI document — `Show()` / `Hide()` lifecycle | Beginner |
| [`label_clock`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/label_clock) | `OneWay` binding from gameplay → UI Variable | Beginner |
| [`health_bar`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/health_bar) | `OneWay` `ProgressBar` binding + `float` Variable | Beginner |
| [`counter_button`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/counter_button) | `Button.OnClick` (Code mode) + Variable update from event | Beginner |
| [`toggle_pause`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/toggle_pause) | `Toggle` `TwoWay` binding to `bool` Variable | Beginner |

Each row links to the sample's README on GitHub — start there for the full setup walkthrough.

---

## Intermediate / Wow showcase samples (V1.5)

Three "integration test" samples that wire multiple features together on realistic surfaces. Tackle them after the five beginner samples — each one demonstrates a pattern you'd actually ship in a real game.

| Sample | Demonstrates | Difficulty |
|---|---|---|
| [`settings_full`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/settings_full) | Full settings dialog: Apply API, all 4 input widgets, dirty-state | Intermediate |
| [`inventory_grid_full`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/inventory_grid_full) | InventoryGrid + slot click events + tooltip via Visibility binding | Intermediate |
| [`survival_hud_aaa`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/survival_hud_aaa) | 5 ProgressBars + damage flash + biome tint + Compose chains | Intermediate |

Each row links to the sample's README on GitHub — start there for the full setup walkthrough.

---

## Advanced showcase samples (V1.5)

Five advanced samples that combine multiple V1.5 features into full game-flow surfaces — modal sequencing, multi-tab navigation, runtime-driven lists, dramatic single-element drives, and class-pick → confirm flows. Tackle them after the intermediate trio when you want to see how every piece of the runtime composes into something a real shipping game would put on screen.

| Sample | Demonstrates | Difficulty |
|---|---|---|
| [`death_respawn_modal`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/death_respawn_modal) | Apply API + multi-var coordination + countdown timer + Code-mode events + test hotkey | Advanced |
| [`quest_journal`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/quest_journal) | Multi-tab nav via Variable+Visibility + nested detail + ProgressBars per objective | Advanced |
| [`boss_hp_bar`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/boss_hp_bar) | Single dramatic ProgressBar + phase markers + Tab/R test hotkeys + scaled HP math | Advanced |
| [`chat_panel`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/chat_panel) | TextEntry Manual commit + Apply.All + dynamic Hud.View.MessageList.AddChild | Advanced |
| [`loadout_selector`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/loadout_selector) | Class card grid + selected-detail with 4 stat bars + Confirm event pattern | Advanced |

Each row links to the sample's README on GitHub — start there for the full setup walkthrough.

---

## See also

- [Showcase samples]({% link reference/showcase-samples.md %}) — the inline catalog with Variables / Bindings tables.
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how a `.sui` becomes the C# `new()`able wrapper the samples mount.
- [Bindings]({% link concepts/bindings.md %}) — the model behind `OneWay` / `TwoWay` / `Compose`.
- [Events & Actions]({% link concepts/events-and-actions.md %}) — what `counter_button` is exercising.
