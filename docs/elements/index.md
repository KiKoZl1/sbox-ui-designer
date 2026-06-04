---
layout: default
title: Element reference
nav_order: 4
has_children: true
permalink: /elements/
---

# Element reference

Every element type in the palette has its own page covering:

- **What it is** — concept + when to use
- **Properties** — type-specific fields (beyond Common / Transform / Appearance)
- **Generated output** — what Razor / SCSS comes out
- **Tips & gotchas**

## Catalog

### Layout root

- [Canvas]({% link elements/canvas.md %}) — root only, one per document

### Containers

- [Panel]({% link elements/panel.md %}) — generic `<div>`
- [HorizontalBox]({% link elements/horizontal-box.md %}) — flex row
- [VerticalBox]({% link elements/vertical-box.md %}) — flex column
- [Overlay]({% link elements/overlay.md %}) — flex with `position: relative` + absolute children
- [Grid]({% link elements/grid.md %}) — wrapped flex grid
- [ScrollPanel]({% link elements/scroll-panel.md %}) — scrollable container

### Visuals

- [Image]({% link elements/image.md %}) — bitmap with fit modes
- [Text]({% link elements/text.md %}) — label with font + alignment
- [Button]({% link elements/button.md %}) — clickable region with M3.5 interactive states
- [ProgressBar]({% link elements/progress-bar.md %}) — fill bar

### Input widgets (V1.5 M4)

The first SUI element types that **read** user input — TwoWay bindings + per-binding [UpdateTrigger]({% link concepts/input-and-update-triggers.md %}).

- [TextEntry]({% link elements/text-entry.md %}) — text input
- [Slider]({% link elements/slider.md %}) — horizontal slider (fully custom markup per D-022)
- [Toggle]({% link elements/toggle.md %}) — boolean checkbox
- [DropDown]({% link elements/dropdown.md %}) — selection dropdown

### Composition

- [SuiReference]({% link elements/sui-reference.md %}) — embed another `.sui` document

### Inventory primitives

- [InventoryGrid]({% link elements/inventory-grid.md %}) — slot grid (wrapped flex)
- [InventorySlot]({% link elements/inventory-slot.md %}) — single slot (M3.5 interactive states)
- [ItemIcon]({% link elements/item-icon.md %}) — standalone item icon (M3.5 interactive states)
- [Hotbar]({% link elements/hotbar.md %}) — single-row inventory bar

Every element shares the [Common, Transform, Appearance]({% link user-guide/details-panel.md %}#sections) sections. See [Concepts]({% link concepts/index.md %}) for the meaning of Layout modes, Anchors, Variables, Bindings, Events, Interactive States, etc.
