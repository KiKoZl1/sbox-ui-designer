---
layout: default
title: Layout modes
parent: Concepts
nav_order: 1
---

# Layout modes (Absolute vs Flex)
{: .no_toc }

Every element has a `Layout.Mode` — `Absolute` or `Flex`. This single field is the most important decision when designing a UI.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Two independent decisions

When you select an element, two things determine how it's positioned and how its children are positioned:

1. **The element's OWN Mode** — controls how **its children** are arranged.
2. **The element's PARENT's Mode** — controls how **the element itself** is positioned.

The Details panel reflects this:

- Position fields (Anchor, X/Y, W/H, Pivot, Z Index) **only show** when the parent is Absolute (otherwise the parent flows this element and X/Y are ignored).
- Flex container fields (Direction, Justify, Align Items, Wrap, Gap) **only show** when this element's Mode is Flex (otherwise the element doesn't lay out children with flex).

So a Hotbar (Flex container, lays out slots in a row) inside a Canvas (Absolute root, positions the Hotbar manually) gets BOTH sections.

## Absolute

Children are positioned manually via Anchor + Position + Size + Pivot.

```
┌────────────────────────────┐
│ Panel (Absolute)           │
│                            │
│  ┌─────┐                   │
│  │Icon │  (X:40 Y:40)      │
│  └─────┘                   │
│                  ┌─────┐   │
│                  │Text │   │
│                  └─────┘   │
│                  (X:-40 Y:-40 anchor BR)
└────────────────────────────┘
```

Each child knows its own position relative to its anchor on the parent. The parent doesn't move children unless you do.

Generated SCSS for children:

```scss
.child {
  position: absolute;
  left: 40px;   /* or right: 40px for *Right anchors */
  top: 40px;    /* or bottom: 40px for Bottom* */
  width: 96px;
  height: 96px;
}
```

**Use Absolute when:**

- You want pixel-perfect placement
- Elements don't need to flow around each other
- HUD-style overlays with fixed positions
- Manual stacking and overlap

## Flex

Children flow according to flex layout rules. The parent decides where each child goes — children's X/Y are ignored.

```
┌────────────────────────────┐
│ Card (Flex, Column)        │
│                            │
│   [Header]                 │
│      ↕ gap                 │
│   [Items Row]              │
│      ↕ gap                 │
│   [Footer]                 │
│                            │
└────────────────────────────┘
```

Generated SCSS for the container:

```scss
.card {
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
  align-items: stretch;
  gap: 16px;
  padding: 20px;
}
```

Generated SCSS for each child (just sizes + content, no position):

```scss
.header {
  width: 100%;  /* via align-items: stretch on parent */
  height: 40px;
}
```

**Use Flex when:**

- Children should pack horizontally or vertically with even gaps
- Items wrap into multiple rows (Wrap)
- You want auto-distribution (SpaceBetween / SpaceAround / SpaceEvenly)
- Layout should respond to dynamic content count
- Grid-like inventories, stat rows, button rows

## Container types

The palette has 6 layout-container types — all of them have Mode = Flex by default, with sensible defaults for Direction/Wrap:

| Type | Direction | Wrap | Typical use |
|---|---|---|---|
| HorizontalBox | Row | NoWrap | Side-by-side icons, button row |
| VerticalBox | Column | NoWrap | Stat list, menu items, modal contents |
| Grid | Row | Wrap | Inventory grid, image gallery |
| InventoryGrid | Row | Wrap | Same as Grid + semantic intent |
| Hotbar | Row | NoWrap | Single-row inventory bar |
| Overlay | Row | NoWrap | Stack with position: relative for absolute children |

You can change Mode/Direction/Wrap any time via Details.

## Switching modes mid-design

Changing an element's Mode from Absolute → Flex (or vice versa) is safe. Existing X/Y/W/H values are preserved in the JSON but may or may not be used depending on the new mode. The canvas re-solves layout immediately.

## When in doubt

- **Designing a HUD with fixed positions for each element?** → Absolute root, Absolute children.
- **Listing items vertically or horizontally with even spacing?** → Flex container, Absolute children inside (their X/Y are ignored).
- **Designing a flexible card/modal with header + content + footer?** → Flex column, children flow.
- **Inventory grid?** → InventoryGrid type (built-in wrapped flex).

## Common patterns

### Mixed Absolute + Flex

A Canvas (Absolute root) with a Hotbar (Flex Row) anchored to BottomCenter:

```
Canvas (Absolute)
└── Hotbar (Flex Row, Anchor: BottomCenter, Y: -40)
    ├── InventorySlot (intrinsic 72×72)
    ├── InventorySlot (intrinsic 72×72)
    └── …
```

The Hotbar uses its X/Y on the Canvas (Absolute parent) but flows its slots horizontally (Flex container).

### Flex inside Flex

Modal with a header that's itself a flex row:

```
Modal (Flex Column)
├── Header (Flex Row, gap: 12)
│   ├── Icon
│   └── Title text
├── Body
└── Footer (Flex Row, justify: SpaceBetween)
    ├── Cancel button
    └── Confirm button
```

Both inner rows are flex containers, but they're flex CHILDREN of the modal column — their own position is determined by the modal, but they internally lay out their own children in rows.

## See also

- [Anchors and Pivot]({% link concepts/anchors-and-pivot.md %}) — how Anchor + X/Y interact in Absolute mode
- [Details panel]({% link user-guide/details-panel.md %}#transform) — which fields show when
