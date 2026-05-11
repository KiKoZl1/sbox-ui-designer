---
layout: default
title: VerticalBox
parent: Element reference
nav_order: 7
---

# VerticalBox

A flex container — children stack vertically in a column.

## Properties

Same as [HorizontalBox]({% link elements/horizontal-box.md %}) but with Direction pre-set to **Column**.

| Field | Default | Notes |
|---|---|---|
| **Direction** | Column | |
| **Justify** | FlexStart | Top of main-axis = top of container |
| **Align Items** | Stretch | Children stretch horizontally (cross axis) |
| **Wrap** | NoWrap | |
| **Gap** | 0 | px between rows |
| **Padding** | 0 | |

## Generated output

```html
<div class="modal sui-modal">
  <label class="title sui-title">YOU DIED</label>
  <label class="subtitle sui-subtitle">…</label>
  <div class="primary-btn sui-respawn-btn">…</div>
</div>
```

```scss
.sui-modal {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  gap: 20px;
  padding: 40px;
}
```

## Tips

- Use Justify = Center to vertically center children inside the container.
- Use Align Items = Center to horizontally center children when their widths differ.
- Use Justify = SpaceBetween for header/body/footer layouts where header sticks to top and footer to bottom.

## See also

- [HorizontalBox]({% link elements/horizontal-box.md %})
- [Concepts · Layout modes]({% link concepts/layout-modes.md %})
