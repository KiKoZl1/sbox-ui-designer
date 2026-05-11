---
layout: default
title: HorizontalBox
parent: Element reference
nav_order: 6
---

# HorizontalBox

A flex container — children pack horizontally in a row.

## Properties

Standard Transform with Mode pre-set to **Flex**, Direction **Row**.

| Field | Default | Notes |
|---|---|---|
| **Direction** | Row | Don't change unless you want to swap to column |
| **Justify** | FlexStart | FlexStart / Center / FlexEnd / SpaceBetween / SpaceAround / SpaceEvenly |
| **Align Items** | Stretch | Stretch / FlexStart / Center / FlexEnd / Baseline |
| **Wrap** | NoWrap | NoWrap / Wrap / WrapReverse |
| **Gap** | 0 | px between children |
| **Padding** | 0 | Inner spacing pushing children inward |

## Generated output

```html
<div class="my-row sui-my-row">
  <div class="child-1 sui-child-1"></div>
  <div class="child-2 sui-child-2"></div>
</div>
```

```scss
.sui-my-row {
  display: flex;
  flex-direction: row;
  align-items: stretch;
  gap: 12px;
  padding: 8px;
}
```

## Tips

- **Center children horizontally**: Justify = Center.
- **Pack to ends**: Justify = SpaceBetween.
- **Children inherit width via Stretch (cross axis)**: if you set Align Items = Stretch (default) on a Row container, children **without** an explicit Height will stretch to fill the container's height. Children **with** explicit Height keep it.
- **For grid-like layouts**: combine with Wrap = Wrap and set children to a fixed Width — they flow into multiple rows. For game-flavored inventory grids, use [InventoryGrid]({% link elements/inventory-grid.md %}) instead.

## See also

- [VerticalBox]({% link elements/vertical-box.md %}) — column variant
- [Concepts · Layout modes]({% link concepts/layout-modes.md %})
