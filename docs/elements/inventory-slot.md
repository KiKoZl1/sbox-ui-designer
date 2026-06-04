---
layout: default
title: InventorySlot
parent: Element reference
nav_order: 13
---

# InventorySlot

A single inventory slot. Renders a frame with optional preview icon + stack count. Like Button, InventorySlot is an interactive type — V1.5 M3.5 adds the same hover / pressed / disabled / focused state overrides, transitions, sounds, and cursor enum (see [Interactive states]({% link concepts/interactive-states.md %}) and [Button]({% link elements/button.md %}#appearance--interactive-states-v15-m35)).

## Properties (Inventory section)

| Field | Default | Notes |
|---|---|---|
| **Slot Index** | 0 | Index in the parent grid. Editor-only; runtime usually reads slot data via the grid component, not via this field |
| **Preview Icon Path** | `""` | Editor preview image (e.g. `ui/InventoryAssets/item_icons/Icon_Material_IronIngot.png`). Used as `background-image` in the generated SCSS too |
| **Preview Count** | 0 | Stack count overlay (e.g. "20"). **Canvas paints it; runtime does not yet** — see [ISSUE-005](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md#issue-005) |

## Generated output

```html
<div class="slot sui-slot-a"></div>
```

```scss
.sui-slot-a {
  width: 96px;
  height: 96px;
  background-color: rgba(0,0,0,0.35);
  border-color: #475569;
  border-width: 1px;
  border-radius: 4px;
  background-image: url("ui/InventoryAssets/item_icons/Icon_Weapon_CombatKnife.png");
  background-size: contain;
  background-position: center;
  background-repeat: no-repeat;
}
```

## Stack count badge

In the canvas, `PaintItemIcon` overlays the `PreviewCount` (when > 1) at the bottom-right of the slot with white bold text + shadow.

In runtime, V1 does NOT emit this badge yet ([ISSUE-005](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md#issue-005)). To get parity at runtime, manually add a `<label>` child after compile:

```razor
<div class="slot sui-slot-a">
  <label class="count">@StackCount</label>
</div>
```

And in `.User.scss`:

```scss
.slot .count {
  position: absolute;
  right: 4px;
  bottom: 4px;
  font-weight: bold;
  color: white;
  text-shadow: 1px 1px 2px black;
}
```

## Tips

- Use different **Border Color** for rarities (`#facc15` yellow = rare, `#a855f7` purple = epic, `#3b82f6` blue = legendary).
- Set **Border Width: 2** for rare/epic to emphasize.
- Background `rgba(0,0,0,0.4)` works well over inventory bg images.

## See also

- [InventoryGrid]({% link elements/inventory-grid.md %})
- [ItemIcon]({% link elements/item-icon.md %})
