---
layout: default
title: Concept map
parent: Reference
nav_order: 12
---

# Concept-to-sample lookup
{: .no_toc }

Answers "which sample teaches me X?". Each section groups related concepts; each row links to every sample that demonstrates that concept.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Bindings

| Concept | What it is | Samples |
|---|---|---|
| OneWay | Gameplay state pushes into a UI Variable; UI follows. Default for HUDs. | [label_clock]({% link samples/label_clock.md %}), [counter_button]({% link samples/counter_button.md %}), [health_bar]({% link samples/health_bar.md %}), [boss_hp_bar]({% link samples/boss_hp_bar.md %}), [death_respawn_modal]({% link samples/death_respawn_modal.md %}), [dialog_system]({% link samples/dialog_system.md %}), [loadout_selector]({% link samples/loadout_selector.md %}), [quest_journal]({% link samples/quest_journal.md %}), [survival_hud_aaa]({% link samples/survival_hud_aaa.md %}) |
| TwoWay | User input on a widget round-trips back to a Variable. Required for Toggle/Slider/TextEntry. | [toggle_pause]({% link samples/toggle_pause.md %}), [settings_full]({% link samples/settings_full.md %}), [chat_panel]({% link samples/chat_panel.md %}) |
| Manual (UpdateTrigger) | Widget value sits stale until controller calls Apply.All(). Canonical "draft → submit" pattern. | [chat_panel]({% link samples/chat_panel.md %}), [settings_full]({% link samples/settings_full.md %}) |
| OnChange (UpdateTrigger) | Fires only when the bound value changes. Cheapest trigger; the default. | every sample that uses bindings |
| OnSubmit (UpdateTrigger) | Fires on Enter inside a TextEntry — engine native event. | [chat_panel]({% link samples/chat_panel.md %}) |

See also: [Bindings concept]({% link concepts/bindings.md %}) and the [Binding mode matrix]({% link reference/binding-mode-matrix.md %}).

---

## Events & actions

| Concept | What it is | Samples |
|---|---|---|
| Code mode | OnClick handler delegate the controller assigns BEFORE Hud.Show (SyncFieldsTo). | [counter_button]({% link samples/counter_button.md %}), [chat_panel]({% link samples/chat_panel.md %}), [death_respawn_modal]({% link samples/death_respawn_modal.md %}), [dialog_system]({% link samples/dialog_system.md %}), [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}), [inventory_grid_full]({% link samples/inventory_grid_full.md %}), [loadout_selector]({% link samples/loadout_selector.md %}), [notification_toast_queue]({% link samples/notification_toast_queue.md %}), [quest_journal]({% link samples/quest_journal.md %}), [settings_full]({% link samples/settings_full.md %}) |
| Doo mode | Inline Doo expression authored in the Designer; runs in the rendered Panel. V1.5 feature. | (no showcase sample uses Doo today — see [Events & Actions]({% link concepts/events-and-actions.md %})) |
| Apply.All() | Flushes every Manual-mode binding at once on Save/Send. | [chat_panel]({% link samples/chat_panel.md %}), [settings_full]({% link samples/settings_full.md %}) |

---

## ExposeAsVariable patterns

| Concept | What it is | Samples |
|---|---|---|
| Single container | Flag one Panel as the runtime AddChild target. | [chat_panel]({% link samples/chat_panel.md %}) (MessageList), [boss_hp_bar]({% link samples/boss_hp_bar.md %}) (DamageFlash) |
| Multiple containers | Two or three ExposeAsVariable Panels coordinated by the controller. | [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}) (BackpackGrid + StashGrid + DragGhost), [dialog_system]({% link samples/dialog_system.md %}) (Card + ChoicesContainer), [notification_toast_queue]({% link samples/notification_toast_queue.md %}) (ToastsContainer) |
| Text element exposure | The recently-fixed codegen path where Text's @ref is emitted into the renderer. | [dialog_system]({% link samples/dialog_system.md %}) (DialogText) |

---

## Interactive states (V1.5)

| Concept | What it is | Samples |
|---|---|---|
| HoverStyle | Authored in the .sui; emits :hover CSS. | [counter_button]({% link samples/counter_button.md %}), [chat_panel]({% link samples/chat_panel.md %}), [loadout_selector]({% link samples/loadout_selector.md %}), [notification_toast_queue]({% link samples/notification_toast_queue.md %}), [quest_journal]({% link samples/quest_journal.md %}), [settings_full]({% link samples/settings_full.md %}) |
| PressedStyle | Same model as Hover, emits :active. | (same samples as Hover) |
| IsHighlighted + HighlightedStyle | Bool-driven sticky-on state; takes priority over Hover via &.highlighted:not(:active). | [quest_journal]({% link samples/quest_journal.md %}) (tab bar) |
| IsDisabled + DisabledStyle | Bool-driven disabled visual + pointer-events:none. | (no showcase sample uses Disabled today) |

See: [Interactive states concept]({% link concepts/interactive-states.md %}).

---

## Layout

| Concept | What it is | Samples |
|---|---|---|
| Anchoring (Anchor + Pivot) | Where the element sits in the parent. | every sample |
| FlexLayout (Row, Column, Wrap) | FlexDirection controls how children flow. | [dialog_system]({% link samples/dialog_system.md %}), [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}), [notification_toast_queue]({% link samples/notification_toast_queue.md %}) |
| GridLayout | Display: Grid with column count + cell size. | [inventory_grid_full]({% link samples/inventory_grid_full.md %}) |
| Mode=Absolute vs Mode=Flex | Mode controls whether THIS element is positioned by parent flex or by explicit X/Y. | [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}) (parent Absolute, children Flex children) |

See: [Layout modes]({% link concepts/layout-modes.md %}).

---

## Visual effects

| Concept | What it is | Samples |
|---|---|---|
| CssTransitions | Author transitions in the .sui or User.scss; controller flips classes. | [loadout_selector]({% link samples/loadout_selector.md %}), [notification_toast_queue]({% link samples/notification_toast_queue.md %}) |
| UserScss | Escape hatch for hand-rolled .User.scss that survives Force Regen. | [dialog_system]({% link samples/dialog_system.md %}), [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}), [notification_toast_queue]({% link samples/notification_toast_queue.md %}) |
| ProgressBar | Bar fill bound to a normalized 0..1 float. | [health_bar]({% link samples/health_bar.md %}), [boss_hp_bar]({% link samples/boss_hp_bar.md %}), [loadout_selector]({% link samples/loadout_selector.md %}), [quest_journal]({% link samples/quest_journal.md %}), [settings_full]({% link samples/settings_full.md %}), [survival_hud_aaa]({% link samples/survival_hud_aaa.md %}) |

---

## Runtime patterns

| Concept | What it is | Samples |
|---|---|---|
| RuntimeAddChild | Populate an ExposeAsVariable Panel from a controller list every render. | [chat_panel]({% link samples/chat_panel.md %}), [dialog_system]({% link samples/dialog_system.md %}), [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}), [notification_toast_queue]({% link samples/notification_toast_queue.md %}) |
| Deferred mutation outside event dispatch | Setting a _pending flag inside an onclick and processing it next frame to avoid Sandbox.UI IOOR. | [dialog_system]({% link samples/dialog_system.md %}), [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}), [notification_toast_queue]({% link samples/notification_toast_queue.md %}) |
| One-shot bootstrap (@ref capture after first paint) | OnUpdate flag-gated logic that runs once when Hud.View?.X first becomes non-null. | [chat_panel]({% link samples/chat_panel.md %}), [dialog_system]({% link samples/dialog_system.md %}), [drag_drop_inventory]({% link samples/drag_drop_inventory.md %}), [notification_toast_queue]({% link samples/notification_toast_queue.md %}) |
| Apply.All() | Flushes every Manual-mode binding at once. | [chat_panel]({% link samples/chat_panel.md %}), [settings_full]({% link samples/settings_full.md %}) |

---

## See also

- [Showcase samples gallery]({% link reference/showcase-samples.md %}) — browse by category
- [Sample index]({% link reference/sample-index.md %}) — short catalog
- [Sample tour]({% link getting-started/sample-tour.md %}) — guided learning path
