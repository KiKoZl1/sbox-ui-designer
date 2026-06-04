---
layout: default
title: InventoryGrid
parent: Element reference
nav_order: 12
---

# InventoryGrid

A slot grid for inventory UIs — same mechanics as [Grid]({% link elements/grid.md %}) but semantically intended to hold `InventorySlot` children.

## Properties

Same as Grid: Columns, Rows, Cell Width, Cell Height, Grid Gap, Auto Fill, Grid Strategy.

## Generated output

```html
<div class="inv-grid sui-inv-grid">
  <div class="slot sui-slot-a"></div>
  <div class="slot sui-slot-b"></div>
  ...
</div>
```

Same SCSS as Grid — `display: flex; flex-wrap: wrap; gap: …; width / height calculated from cell × count.`

## Typical structure

```
InventoryGrid (Columns: 6, Rows: 4)
├── InventorySlot (SlotIndex: 0, PreviewIconPath: ...)
├── InventorySlot (SlotIndex: 1)
├── InventorySlot (SlotIndex: 2)
...
```

The SlotIndex is editor-only — your code typically reads slot data via the parent grid's component, not via the slot's `SlotIndex` property.

## Bindable properties

Identical to [Grid bindings]({% link elements/grid.md %}#bindable-properties): `Columns`, `Rows`, `CellWidth`, `CellHeight`, `Gap` — all OneWay. Useful for resizing inventory grids based on player stats (`Columns` ← `BackpackTier * 2 + 4`).

## See also

- [InventorySlot]({% link elements/inventory-slot.md %}) — child of InventoryGrid
- [Hotbar]({% link elements/hotbar.md %}) — single-row variant
- [Grid]({% link elements/grid.md %}) — non-inventory variant
