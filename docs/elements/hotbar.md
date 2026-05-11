---
layout: default
title: Hotbar
parent: Element reference
nav_order: 15
---

# Hotbar

A single-row inventory bar — semantically a slot container that doesn't wrap to a second row.

## Properties

Same as [Grid]({% link elements/grid.md %}) but the generator emits `flex-wrap: nowrap` instead of `wrap`. Typical setup:

- Rows: 1
- Columns: 8 or 9
- Cell Width / Height: 64 or 72
- Grid Gap: 4 or 8

## Generated output

```html
<div class="hotbar sui-hotbar">
  <div class="hb-slot sui-hb-1"></div>
  <div class="hb-slot sui-hb-2"></div>
  ...
</div>
```

```scss
.sui-hotbar {
  display: flex;
  flex-direction: row;
  flex-wrap: nowrap;
  gap: 8px;
  width: 800px;
  height: 88px;
}
```

## Typical positioning

Bottom-center of the screen:

- Anchor: BottomCenter
- Y: -40 (push 40px up from the bottom)
- X: 0 (horizontally centered)
- Width: cells × CellWidth + (cells - 1) × Gap

For a 9-slot hotbar with 72px cells and 8px gap: 9×72 + 8×8 = 712 px.

## Tips

- Use [InventorySlot]({% link elements/inventory-slot.md %}) children with rarity-coloured borders to indicate selected slot.
- For a "selected slot" visual (yellow border on the active hotbar slot), use `.User.scss`:

```scss
.hotbar .hb-slot.active {
  border-color: #facc15;
  border-width: 3px;
}
```

And toggle the `.active` class from your `PanelComponent` based on the player's current hotkey.
