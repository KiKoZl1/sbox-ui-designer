---
layout: default
title: Grid
parent: Element reference
nav_order: 9
---

# Grid

A wrapped-flex grid for laying out children in `Columns × Rows`. Unlike CSS Grid (not supported in s&box UI), this uses `display: flex; flex-wrap: wrap;` and cell sizes from the element's properties.

## Properties (Grid section)

| Field | Default | Notes |
|---|---|---|
| **Columns** | 1 | Number of columns |
| **Rows** | 1 | Number of rows (used only for auto-sizing) |
| **Cell Width** | 64 | px |
| **Cell Height** | 64 | px |
| **Grid Gap** | 4 | px between cells |
| **Auto Fill** | off | (V1 reserved — fills available space with as many cells as fit) |
| **Grid Strategy** | WrappedFlex | Currently the only strategy supported |

## Generated output

```html
<div class="rewards-grid sui-rewards-grid">
  <div class="reward sui-r1"></div>
  <div class="reward sui-r2"></div>
  <div class="reward sui-r3"></div>
  <div class="reward sui-r4"></div>
  <div class="reward sui-r5"></div>
  <div class="reward sui-r6"></div>
</div>
```

```scss
.sui-rewards-grid {
  display: flex;
  flex-direction: row;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 12px;
  width: 312px;        /* Columns × CellWidth + (Columns-1) × Gap + 2×BorderWidth + PaddingX */
  height: 204px;       /* Rows × CellHeight + (Rows-1) × Gap + 2×BorderWidth + PaddingY */
}
```

The generator computes the grid's `width` and `height` to fit exactly Columns × CellWidth (plus gaps, border, padding compensation). This ensures children wrap at the right column count.

If the user pins an explicit `Width` or `Height` in Layout, those win — the cell-derived size is only used when the user hasn't set one.

## Bindable properties

| Property | Mode | Target type |
|---|---|---|
| `Columns` | OneTime / OneWay | int |
| `Rows` | OneTime / OneWay | int |
| `CellWidth` | OneTime / OneWay | float |
| `CellHeight` | OneTime / OneWay | float |
| `Gap` | OneTime / OneWay | float |
| Style + Universal | OneWay | per matrix |

## Tips

- **Don't set children to a different width** than `Cell Width`. They'll still wrap, but spacing will look uneven.
- For inventory UIs specifically, use [InventoryGrid]({% link elements/inventory-grid.md %}) — same mechanics but with semantic intent and clearer property names.
- **Border + box-sizing gotcha**: s&box runtime CSS uses border-box. The generator already adds `2 × BorderWidth + PaddingX/Y` to the computed width so a 1px border doesn't push the last column off the row.
