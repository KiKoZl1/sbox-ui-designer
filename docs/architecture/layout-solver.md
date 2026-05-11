---
layout: default
title: Layout solver
parent: Architecture
nav_order: 4
---

# Layout solver
{: .no_toc }

How SUI Designer computes "where each element ends up on the canvas" — and how it converts a target rect back into anchor/pivot values.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Why a solver exists

The canvas paints elements at exact pixel positions. So does the runtime CSS engine. **Both must agree** — otherwise what you see in the editor doesn't match what ships.

`SuiLayoutSolver` is the single source of truth for "where is each element". One math, three consumers:

- **Renderer** — reads rects to draw.
- **Hit-tester** — reads rects to pick on click.
- **Selection chrome** — reads rects to position resize handles.

This shared dictionary means render fidelity and hit-test fidelity can never drift.

## Inputs and outputs

```csharp
public sealed class SuiLayoutSolver
{
    public Vector2 PanelSize { get; set; }
    public Dictionary<string, Rect> Rects { get; }
    public Dictionary<string, SuiElement> ById { get; }

    public void Solve( SuiDocument document );
}
```

Source: [Editor/Canvas/SuiLayoutSolver.cs](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Canvas/SuiLayoutSolver.cs).

The output `Rects` map is in **logical pixels** (the document's coordinate space — typically 1920x1080). The viewport widget scales it to screen pixels for drawing.

## The three modes

The solver dispatches per parent based on type and `Layout.Mode`:

| Parent type | Layout.Mode | Solver used |
|---|---|---|
| Root canvas | — | Always fills `PanelSize` |
| Grid / InventoryGrid | any | `SuiFlexLayout.SolveGrid` (regular tile pass) |
| HorizontalBox / VerticalBox / Hotbar / any with Mode=Flex | Flex | `SuiFlexLayout.Solve` |
| Everything else | Absolute | `ResolveAbsoluteRect` per child |

### Absolute pass — anchor + pivot math

For a single child of an absolute parent:

1. Look up the **anchor reference point** in the parent — top-left, center, bottom-right, etc.
2. The anchor implies a **pivot** on the child — the point on the child that "sticks" to the reference.
3. Offset by `(X, Y)`, signed by which edge the anchor is on (so positive X on a right anchor means "inward").
4. Subtract `pivotX * width` and `pivotY * height` to get the final top-left.

Concretely:

```csharp
var posX = refX + l.X * signX;        // signed offset from anchor
var posY = refY + l.Y * signY;
var topLeftX = posX - pivotX * w;     // pull back by pivot fraction
var topLeftY = posY - pivotY * h;
```

The anchor → (refX, refY, pivot, sign) table is the heart of the math. From `ResolveAbsoluteRect`:

| Anchor | refX | refY | pivotX | pivotY | signX | signY |
|---|---|---|---|---|---|---|
| TopLeft | 0 | 0 | 0 | 0 | +1 | +1 |
| TopCenter | pw/2 | 0 | 0.5 | 0 | +1 | +1 |
| TopRight | pw | 0 | 1 | 0 | -1 | +1 |
| MiddleLeft | 0 | ph/2 | 0 | 0.5 | +1 | +1 |
| MiddleCenter | pw/2 | ph/2 | 0.5 | 0.5 | +1 | +1 |
| MiddleRight | pw | ph/2 | 1 | 0.5 | -1 | +1 |
| BottomLeft | 0 | ph | 0 | 1 | +1 | -1 |
| BottomCenter | pw/2 | ph | 0.5 | 1 | +1 | -1 |
| BottomRight | pw | ph | 1 | 1 | -1 | -1 |

This table is **mirrored exactly** in `SuiScssGenerator.EmitAnchorRules` — that's what guarantees canvas/runtime parity.

### Stretch anchors

Stretch is the odd one: `X/Y/Width/Height` are interpreted as **margins** (insets from each edge), not as a content box.

```csharp
case SuiAnchor.Stretch:
    return new Rect(
        parentRect.Left + l.X,                    // left margin
        parentRect.Top + l.Y,                     // top margin
        pw - l.X - l.Width,                       // right margin via "Width" field
        ph - l.Y - l.Height );                    // bottom margin via "Height" field
```

This is intentional — it mirrors CSS `left: 0; right: 0; top: 0; bottom: 0;` which is how stretch-anchored elements render. The Details panel relabels the fields when Stretch is selected.

`StretchHorizontal` and `StretchVertical` are the partial-axis variants — half-stretch, half-pinned.

### Flex pass

When a parent has `Mode = Flex`, children are positioned by `SuiFlexLayout.Solve`. The algorithm:

1. **Subtract padding** from parent rect → inner content rect.
2. **Resolve intrinsic sizes** for each child (uses Layout.Width/Height or per-type defaults).
3. **Sum main-axis sizes + gaps** to get used main size.
4. **Distribute remaining space** along main axis per `justify-content`.
5. **Position each child** along main axis; size + position cross axis per `align-items`.

Single-pass — no flex-grow/shrink, no wrapping (Grid uses wrapped flex via the separate `SolveGrid` entry point).

What's supported:
- `flex-direction`: Row, Column, RowReverse, ColumnReverse
- `justify-content`: FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround, SpaceEvenly
- `align-items`: FlexStart, Center, FlexEnd, Stretch
- `gap`, `margin`, `padding`

What's not (deferred to V2):
- `flex-grow` / `flex-shrink` / `flex-basis`
- `align-self` (children inherit parent's `align-items`)
- Baseline align (collapses to FlexStart)

### Grid pass

`SolveGrid` is a dedicated tile-based pass for `InventoryGrid` / `Grid`. Driven by:

- `Props.Columns` (and computed Rows)
- `Props.CellWidth` / `CellHeight`
- `Props.GridGap`
- Parent padding + border

Children get rects assigned in row-major order: column 0 to N, then wrap. The wrap point is the parent's content width minus padding minus 2×borderWidth.

This bypasses standard flex because the InventoryGrid contract is "regular tile, predictable count" — flex-wrap would give the same result only if every child has the exact same intrinsic size. Dedicating a pass to it eliminates a class of bugs.

## Auto-Text measurement

Text elements in `Auto` size mode have W/H derived from rendered text size. The solver measures them once at the start of `Solve()`:

```csharp
private void MeasureAutoTexts( SuiDocument doc )
{
    foreach ( var el in doc.Elements )
    {
        if ( el.Type != SuiElementType.Text ) continue;
        if ( el.Props?.TextSizeMode != SuiTextSizeMode.Auto ) continue;
        Editor.Paint.SetFont( font, size, weight );
        _autoTextSizes[el.Id] = Editor.Paint.MeasureText( el.Props.Text );
    }
}
```

The paint context must be active when `Solve()` is called (the solver is called from `OnPaint`, so this is fine). Measured sizes override `Layout.Width/Height` for the subsequent rect computation.

## The inverse — drag-to-move and resize

When the user drags an element on the canvas, the drag handler computes a **target rect** (where the element should end up) and needs to write back the (X, Y, W, H) values the document should store.

This is the inverse of the layout pass: `RectToLayoutValues`.

```csharp
public static (float x, float y, float w, float h) RectToLayoutValues(
    Rect targetRect, SuiAnchor anchor, Rect parentRect )
```

For each anchor variant the inverse math is:

```csharp
var posX = topLeftX + pivotX * w;     // un-shift by pivot
var posY = topLeftY + pivotY * h;
var x = (posX - refX) * signX;        // signed offset from ref
var y = (posY - refY) * signY;
```

For Stretch the inverse computes the 4 margins directly:

```csharp
var leftMargin = targetRect.Left - parentRect.Left;
var topMargin = targetRect.Top - parentRect.Top;
var rightMargin = parentRect.Right - targetRect.Right;
var bottomMargin = parentRect.Bottom - targetRect.Bottom;
```

This is what makes "drag a TopRight-anchored button to the left" produce the right `X` (positive, because TopRight is signX=-1, so a smaller X means further right).

## Render order — ZIndex + hierarchy

Children of a parent paint in this order:

1. **Ascending ZIndex** — high Z draws last (on top).
2. **Hierarchy order as tie-break** — when ZIndex is equal, authoring order wins.

```csharp
indexed.Sort( ( a, b ) =>
{
    var az = a.el.Layout?.ZIndex ?? 0;
    var bz = b.el.Layout?.ZIndex ?? 0;
    if ( az != bz ) return az.CompareTo( bz );
    return a.idx.CompareTo( b.idx );
});
```

`GetRenderOrderedChildren` is the helper used by both the renderer and the hit-tester. Visual ↔ click parity is by construction.

## Defaults for missing sizes

When `Layout.Width == 0` or `Layout.Height == 0`, the solver falls back to per-type defaults:

| Type | Default W | Default H |
|---|---|---|
| Text | 200 | 32 |
| Button | 120 | 36 |
| Image | 100 | 100 |
| ItemIcon | 64 | 64 |
| InventorySlot | 64 | 64 |
| (anything else) | 100 | 32 |

These match what a freshly dropped element looks like before the user resizes it.

## Hit-testing

There's no separate hit-tester — the `Rects` dictionary IS the hit-test source. The canvas widget walks children in **reverse paint order** (top-z first) and returns the first element whose rect contains the cursor.

This is what makes overlapping elements work: ZIndex 5 paints on top, ZIndex 5 also clicks first.

## Performance

The solver is `O(n)` where n is the number of elements. There's no cross-element dependency that would force multiple passes (flex children don't have flex-grow, grids are regular tiles). Documents up to ~500 elements solve in well under a millisecond.

`Solve()` runs on every paint — the canvas doesn't cache. This is intentional and trivial in cost; caching would just create a different class of bugs.

## See also

- [Canvas renderer]({% link architecture/canvas-renderer.md %}) — what consumes the solver output
- [Anchors and pivot]({% link concepts/anchors-and-pivot.md %}) — user-facing concept
- [Layout modes]({% link concepts/layout-modes.md %}) — Flex vs Absolute concept
