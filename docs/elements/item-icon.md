---
layout: default
title: ItemIcon
parent: Element reference
nav_order: 14
---

# ItemIcon

A standalone item icon. Same rendering as [InventorySlot]({% link elements/inventory-slot.md %}) but without the slot frame — use it when you want an icon without an explicit slot context (e.g. floating reward icons, tooltip previews, drag-ghosts).

ItemIcon is an interactive type — V1.5 M3.5 adds the same hover / pressed / highlighted / disabled / focused state overrides, transitions, sounds, cursor enum, and `ButtonShape` / `BackgroundSize` controls as Button. See [Interactive states]({% link concepts/interactive-states.md %}). The **Highlighted Style** picker in the Designer pairs with the bindable `IsHighlighted` bool — useful for "this reward is selected" or "this drag-ghost is over a valid drop target" visuals.

## Properties

Same Inventory section as InventorySlot:

| Field | Notes |
|---|---|
| **Slot Index** | Optional |
| **Preview Icon Path** | Path to the icon image |
| **Preview Count** | Stack count overlay (canvas-only currently) |

## Generated output

```html
<div class="reward sui-r1"></div>
```

```scss
.sui-r1 {
  width: 96px;
  height: 96px;
  background-color: rgba(0,0,0,0.5);
  border-color: #475569;
  border-width: 1px;
  border-radius: 6px;
  background-image: url("ui/InventoryAssets/item_icons/Icon_Consumable_BerriesStack.png");
  background-size: contain;
  background-position: center;
  background-repeat: no-repeat;
}
```

## When to use ItemIcon vs InventorySlot

| Use case | Choice |
|---|---|
| Slot in an inventory grid | InventorySlot |
| Reward/loot popup with a small grid of items | ItemIcon |
| Tooltip preview of an item | ItemIcon |
| Drag ghost during a drag-and-drop | ItemIcon |

Both render identically — the choice is semantic and affects how data binds (slots get a slot index, icons don't).

## Bindable properties

| Property | Mode | Target type |
|---|---|---|
| `ImagePath` | OneTime / OneWay | string |
| `Tint` | OneTime / OneWay | Color |
| `IsHighlighted` | OneTime / OneWay | bool |
| Style + Universal | OneWay | per matrix |

## Events surfaced

| Event | Razor attribute |
|---|---|
| `OnClick` | onclick |
| `OnRightClick` | onclick + e.Button=='mouseright' |
| `OnHover` | onmouseover |
| `OnUnhover` | onmouseout |
