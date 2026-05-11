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
- [Button]({% link elements/button.md %}) — clickable region with label
- [ProgressBar]({% link elements/progress-bar.md %}) — fill bar

### Inventory primitives (V1)

- [InventoryGrid]({% link elements/inventory-grid.md %}) — slot grid (wrapped flex)
- [InventorySlot]({% link elements/inventory-slot.md %}) — single slot
- [ItemIcon]({% link elements/item-icon.md %}) — standalone item icon
- [Hotbar]({% link elements/hotbar.md %}) — single-row inventory bar

Every element shares the [Common, Transform, Appearance]({% link user-guide/details-panel.md %}#sections) sections. See [Concepts]({% link index.md %}#concepts) for the meaning of Layout modes, Anchors, etc.
