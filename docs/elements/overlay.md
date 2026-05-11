---
layout: default
title: Overlay
parent: Element reference
nav_order: 8
---

# Overlay

A flex container that creates a positioning context (`position: relative`) so absolutely-positioned children anchor to it instead of cascading to a distant ancestor.

## When to use

When you want to stack multiple elements at the same location (badge over an icon, label over an image, indicator over a slot).

Without Overlay: a child with `position: absolute` anchors to the nearest *positioned* ancestor — typically the root or some big container — and ends up off where you wanted.

With Overlay: the child anchors to **the Overlay element's box**, so its X/Y/Anchor make sense locally.

## Properties

Same as a plain flex container, but generated SCSS adds `position: relative` to create a positioning context.

## Generated output

```html
<div class="alert-overlay sui-alert-overlay">
  <div class="alert-bg sui-alert-bg"></div>
  <div class="alert-icon sui-alert-icon"></div>
  <div class="alert-count sui-alert-count">3</div>
</div>
```

```scss
.sui-alert-overlay {
  position: relative;  /* ← key */
  display: flex;
  ...
  .sui-alert-bg {
    position: absolute;
    /* fills the overlay */
  }
  .sui-alert-icon { ... }
  .sui-alert-count {
    position: absolute;
    right: -4px;
    bottom: -4px;
    /* stack badge */
  }
}
```

## Common pattern: badge

A circular badge with a number on top of an icon:

```
Overlay (64×64)
├── Panel (Anchor: Stretch, dark circle bg)
├── ItemIcon (Anchor: MiddleCenter, 40×40)
└── Text (Anchor: BottomRight, 24×24, "3")
```

## Note

Most elements you'd want to overlay can also be achieved via Panel with `Mode: Absolute` children (where each child has its own Anchor). The Overlay element is just a syntactic convenience — it signals intent and ensures `position: relative` is emitted.
