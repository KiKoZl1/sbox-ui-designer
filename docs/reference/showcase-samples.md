---
layout: default
title: Showcase samples
parent: Reference
nav_order: 11
---

# Showcase samples
{: .no_toc }

Sixteen samples ship in V1.5 to cover every shipped feature — from the smallest mount lifecycle up to a flagship survival HUD that touches every Variable type and every binding target in a single document. They are grouped into five categories (Starter, Input widgets, Interactive states, Runtime-rendered, Full-feature) so you can either follow the difficulty ramp or jump straight to the recipe you need. Each sample has its own dedicated page under `/samples/<name>/`, reachable from the docs sidebar, with full Variables / Bindings / Events tables.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Browse by category

### Starter (3)

| Sample | What it teaches | Difficulty |
|---|---|---|
| [empty_canvas]({% link samples/empty_canvas.md %}) | Minimum mount lifecycle | Beginner |
| [label_clock]({% link samples/label_clock.md %}) | First OneWay binding | Beginner |
| [counter_button]({% link samples/counter_button.md %}) | First Code-mode OnClick + Variable | Beginner |

### Input widgets (2)

| Sample | What it teaches | Difficulty |
|---|---|---|
| [toggle_pause]({% link samples/toggle_pause.md %}) | Smallest TwoWay binding (Toggle round-trip) | Intermediate |
| [settings_full]({% link samples/settings_full.md %}) | Every input widget + Apply.All() | Intermediate |

### Interactive states (3)

| Sample | What it teaches | Difficulty |
|---|---|---|
| [health_bar]({% link samples/health_bar.md %}) | OneWay ProgressBar + Variable | Intermediate |
| [boss_hp_bar]({% link samples/boss_hp_bar.md %}) | Phase markers + ZIndex overlay + ExposeAsVariable writes | Intermediate |
| [death_respawn_modal]({% link samples/death_respawn_modal.md %}) | Countdown-gated click + 6 OneWay text bindings | Intermediate |

### Runtime-rendered (7)

| Sample | What it teaches | Difficulty |
|---|---|---|
| [chat_panel]({% link samples/chat_panel.md %}) | Manual TextEntry + Apply.All + dynamic AddChild | Advanced |
| [dialog_system]({% link samples/dialog_system.md %}) | Branching NPC tree with typewriter + deferred mutation | Advanced |
| [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}) | Two 4x4 grids with cursor-ghost + hit-test | Advanced |
| [inventory_grid_full]({% link samples/inventory_grid_full.md %}) | Runtime grid wired via ExposeAsVariable | Flagship |
| [loadout_selector]({% link samples/loadout_selector.md %}) | Card grid + detail panel + 5 buttons + 6 Variables | Advanced |
| [notification_toast_queue]({% link samples/notification_toast_queue.md %}) | Stacking auto-dismiss toasts + CSS transitions | Advanced |
| [quest_journal]({% link samples/quest_journal.md %}) | Multi-tab nav via IsHighlighted + HighlightedStyle | Advanced |

### Full-feature (1)

| Sample | What it teaches | Difficulty |
|---|---|---|
| [survival_hud_aaa]({% link samples/survival_hud_aaa.md %}) | Every Variable type + every binding target in one HUD | Flagship |

## Browse by concept

Looking for a sample that demonstrates a specific concept (OneWay binding, Apply.All, IsHighlighted, CSS transitions, etc.)? Check the [Concept map]({% link reference/concept-map.md %}) for a lookup table grouped by concept family.

## Pattern recipes

| I want to... | Look at... |
|---|---|
| Drive a piece of text from gameplay every frame | [label_clock]({% link samples/label_clock.md %}) |
| Build a full settings screen with Apply / Cancel / Reset | [settings_full]({% link samples/settings_full.md %}) |
| Spawn UI elements dynamically at runtime | [chat_panel]({% link samples/chat_panel.md %}) or [notification_toast_queue]({% link samples/notification_toast_queue.md %}) |
| Build a drag-and-drop with cursor-following ghost | [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}) |
| Show a stacking notification queue | [notification_toast_queue]({% link samples/notification_toast_queue.md %}) |
| Build a multi-tab UI with selected-state highlight | [quest_journal]({% link samples/quest_journal.md %}) |
| Show a full-screen modal with countdown | [death_respawn_modal]({% link samples/death_respawn_modal.md %}) |
| Drive a dramatic single bar with phase markers | [boss_hp_bar]({% link samples/boss_hp_bar.md %}) |
| Build a class/loadout picker with detail pane | [loadout_selector]({% link samples/loadout_selector.md %}) |
| Wire a 6x4 inventory grid | [inventory_grid_full]({% link samples/inventory_grid_full.md %}) |
| Implement branching NPC dialog with typewriter | [dialog_system]({% link samples/dialog_system.md %}) |
| Ship a survival HUD touching every Variable type | [survival_hud_aaa]({% link samples/survival_hud_aaa.md %}) |
| Make a checkbox flip a bool | [toggle_pause]({% link samples/toggle_pause.md %}) |
| Bind a ProgressBar to a normalized health value | [health_bar]({% link samples/health_bar.md %}) |
| Increment a number on click | [counter_button]({% link samples/counter_button.md %}) |
| Prove the wrapper-mount plumbing works | [empty_canvas]({% link samples/empty_canvas.md %}) |

## Source on GitHub

Every sample folder ships a `.sui` document, a `<Name>Controller.cs`, and a per-folder `README.md` with Variables / Bindings / Events tables. Source lives at [samples/showcase/](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase) in the repo.

## See also

- [Sample index]({% link reference/sample-index.md %}) — short catalog
- [Sample tour]({% link getting-started/sample-tour.md %}) — guided learning path
- [Concept map]({% link reference/concept-map.md %}) — concept → sample lookup
