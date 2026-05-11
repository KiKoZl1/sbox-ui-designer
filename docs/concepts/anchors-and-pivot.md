---
layout: default
title: Anchors and Pivot
parent: Concepts
nav_order: 2
---

# Anchors and Pivot
{: .no_toc }

How elements snap to their parents — UMG-style anchoring, applied to s&box.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The 12 anchors

```
┌─────────────────┐
│ TL    TC    TR  │
│                 │
│ ML    MC    MR  │
│                 │
│ BL    BC    BR  │
└─────────────────┘
   ↓
  Stretch / StretchHorizontal / StretchVertical
```

9 single-point anchors + 3 stretch variants.

| Anchor | Reference point on parent | X means | Y means |
|---|---|---|---|
| **TopLeft** | (0, 0) | offset from left edge → right | offset from top edge → down |
| **TopCenter** | (0.5w, 0) | offset from horizontal center | offset from top edge → down |
| **TopRight** | (w, 0) | offset from right edge → left | offset from top edge → down |
| **MiddleLeft** | (0, 0.5h) | offset from left → right | offset from vertical center |
| **MiddleCenter** | (0.5w, 0.5h) | offset from horizontal center | offset from vertical center |
| **MiddleRight** | (w, 0.5h) | offset from right → left | offset from vertical center |
| **BottomLeft** | (0, h) | offset from left → right | offset from bottom → up |
| **BottomCenter** | (0.5w, h) | offset from horizontal center | offset from bottom → up |
| **BottomRight** | (w, h) | offset from right → left | offset from bottom → up |
| **Stretch** | full parent rect | X = left margin | Y = top margin (W = right margin, H = bottom margin) |
| **StretchHorizontal** | full width, vertical center | X = left margin, Y = vertical offset | (W = right margin, H = element height) |
| **StretchVertical** | full height, horizontal center | X = horizontal offset, Y = top margin | (W = element width, H = bottom margin) |

## How anchor + X/Y/W/H produce a rect

For non-Stretch anchors:

```
finalX = parent.X + refX + signX × ownX − pivotX × ownW
finalY = parent.Y + refY + signY × ownY − pivotY × ownH
```

Where `refX/Y` is the anchor reference point on the parent, `signX/Y` is +1 or -1 depending on left-anchored vs right-anchored, and `pivotX/Y` is the element's own pivot (0..1).

The math is implemented in `SuiLayoutSolver.ResolveAbsoluteRect` (forward) and `SuiLayoutSolver.RectToLayoutValues` (inverse, used by drag-to-move).

## Stretch — X/Y/W/H are margins

Stretch is special: X/Y/W/H don't position the element. They are **margins** that shrink the element inward from the parent's edges.

```
Stretch with X=8 Y=8 W=8 H=8:
┌─────────────────────────────────┐
│       8                          │
│   ┌───────────────────────┐      │
│ 8 │                       │ 8    │   ← element fills parent minus 8px each side
│   │       element         │      │
│   └───────────────────────┘      │
│       8                          │
└─────────────────────────────────┘
```

Generated SCSS:

```scss
.stretched {
  position: absolute;
  left: 8px;
  top: 8px;
  right: 8px;
  bottom: 8px;
  /* no width/height — left/right and top/bottom together size it */
}
```

`StretchHorizontal` and `StretchVertical` mix margin-style on one axis with normal X/Y/W/H on the other.

## Pivot

Pivot is a 0..1 fraction indicating which point inside the element is "the pivot."

- `(0, 0)` — top-left corner. Default for TopLeft anchor.
- `(0.5, 0.5)` — center. Default when picking MiddleCenter anchor.
- `(1, 1)` — bottom-right corner.

Affects:
1. The meaning of Position (X/Y offsets the pivot to that position relative to the anchor, not the top-left corner).
2. Rotation (V2) — element rotates around its pivot point.

For most cases, pivot is set automatically when you pick an anchor (the anchor's natural pivot). Manual tweaks are rare.

## Reparenting and anchor preservation

When you change an element's anchor in the Details panel, the editor uses `SuiSetAnchorCommand` which:

1. Snapshots the element's current rect (logical coords)
2. Updates `Layout.Anchor` to the new value
3. Re-computes X/Y/W/H so the element occupies **the same on-screen rect** under the new anchor

So changing anchor doesn't make the element jump — it just changes the reference point. Useful for "I want this Top-Left element to anchor to the right edge as the window resizes" workflows.

## When to use each anchor

| Goal | Anchor |
|---|---|
| Hud overlay at top-left (HP bar) | TopLeft |
| Notification banner top-center | TopCenter |
| Minimap at top-right | TopRight |
| Centered modal | MiddleCenter |
| Status text at bottom-left | BottomLeft |
| Hotbar at bottom-center | BottomCenter |
| Inventory bag at bottom-right | BottomRight |
| Full-screen backdrop (dim) | Stretch |
| Top stripe / header bar | StretchHorizontal |
| Side rail / sidebar | StretchVertical |

## See also

- [Layout modes]({% link concepts/layout-modes.md %}) — Absolute vs Flex
- [Details panel · Transform]({% link user-guide/details-panel.md %}#transform)
