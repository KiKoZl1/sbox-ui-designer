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

## What happens when the parent resizes

This is one of the most common points of confusion, especially for users coming from UMG. The short version:

**Single-point anchors (TopLeft, MiddleCenter, BottomRight, etc.) — child stays the same size, repositions to the new anchor point.**

**Stretch anchors (Stretch, StretchHorizontal, StretchVertical) — child resizes with the parent, maintaining its margins.**

### Worked example

Parent panel sized 400×300, with a child Button at:
- Width: 100, Height: 40
- Anchor: MiddleCenter
- X: 0, Y: 0

The Button renders at the parent's center, 100×40.

Now you resize the parent to 800×600.

**What happens:** the Button is now at the new center (which moved), but **still 100×40**. It does NOT grow to fill more space proportionally.

```
Before resize (parent 400×300):           After resize (parent 800×600):
┌──────────────────────┐                  ┌──────────────────────────────────────┐
│                      │                  │                                      │
│      ┌──────┐        │                  │                                      │
│      │ BTN  │ 100×40 │                  │                                      │
│      └──────┘        │                  │              ┌──────┐                │
│                      │                  │              │ BTN  │ still 100×40   │
└──────────────────────┘                  │              └──────┘                │
                                          │                                      │
                                          │                                      │
                                          └──────────────────────────────────────┘
```

This matches UMG default behavior exactly.

### How to make the child grow with the parent

| Want | Anchor | Behavior |
|---|---|---|
| Child fills parent minus margins | **Stretch** with X/Y/W/H = desired margins | Width and height both scale |
| Child stretches horizontally, fixed height | **StretchHorizontal** | Width scales, height stays |
| Child stretches vertically, fixed width | **StretchVertical** | Height scales, width stays |
| Multiple children share space along an axis | **Flex** layout on the parent (HorizontalBox / VerticalBox / Grid) | Children distribute per `justify-content` + per-child `flex-grow` if set |

### Why we don't have a "scale everything proportionally" mode

This is a feature gap on purpose. UMG has the `ScaleBox` widget for this case, and we don't ship an equivalent today. **A `ScaleBox`-equivalent element type or an `AutoScaleChildren` flag on Panel remains a candidate for a future release.**

If you need proportional scaling today, the workaround is:
- Use a **Flex container** (HorizontalBox / VerticalBox / Grid) — children scale within the flex layout
- Use **Stretch anchor with margins** for absolutely-positioned items
- Use the **canvas scale mode** (`ScreenHeight1080` on the root document) for whole-UI scaling based on resolution

### Why this design

Anchor systems describe *where* an element lives relative to its parent. Sizing is a separate axis from positioning. Conflating them — "the anchor also scales me" — is what causes the most common UMG layout bugs: UI elements that grow into other elements on resolution change because their anchor "owned" their size unexpectedly.

By keeping size and anchor orthogonal, the designer explicitly opts into stretching (via Stretch anchors or Flex layout) when they want it. When they don't, the element stays the size they designed it.

## See also

- [Layout modes]({% link concepts/layout-modes.md %}) — Absolute vs Flex
- [Details panel · Transform]({% link user-guide/details-panel.md %}#transform)
