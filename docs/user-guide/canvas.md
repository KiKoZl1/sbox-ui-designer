---
layout: default
title: Canvas
parent: User Guide
nav_order: 6
---

# Canvas
{: .no_toc }

The paint-based designer surface — center tab "Designer". Every element you add is rendered here in real time, with selection chrome, layout bounds, rulers, and the document area outline.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Why paint-based

The canvas does NOT use s&box's runtime Razor renderer. It uses `Editor.Paint` (Qt-backed) to draw the document directly. Reasons (see [PRD 15]({{ site.baseurl }}/) for the long version):

- Zero hot-reload latency between edits (no engine compile)
- Selection chrome (handles, outline) renders **over** content without being overwritten by the runtime
- Predictable layout — the `SuiLayoutSolver` is canonical, used by both canvas and generator
- Custom overlays (rulers, grid, anchor markers, layout bounds) integrate naturally

The downside: the canvas has its own implementation of layout + image rendering, which can drift from the runtime. Several recent fixes (anchor-aware drag, rgba color parsing, image border-radius clipping) closed those gaps.

## Sub-UI recursive paint (V1.5)

`SuiReference` elements paint their embedded `.sui` document **recursively** — the canvas resolves the referenced doc, applies any per-instance Props overrides, and renders the child's full tree inside the reference's rectangle. Resizing the reference rescales children proportionally (UMG-like). A **purple dashed border** marks the SuiReference rect.

This makes "I can see what I'm designing while I compose sub-UIs" work without `Test in Play`. The child's own Canvas root frame is suppressed via the `SkipRootFrame` flag so it doesn't bleed past the reference bounds. See [Composition]({% link concepts/composition.md %}).

## Interactive states (V1.5 M3.5)

The canvas always renders the **Normal** state. Hover / Pressed / Disabled / Focused overrides ARE NOT painted on the canvas — designers see them via **Test in Play**. This is intentional (the canvas has no input state).

## Coordinate systems

- **Logical** — pixels in the document's drawable area (`0..PanelSize`, e.g. `0..1920`)
- **Widget** — pixels of the canvas widget's local rect (Paint API native space)

Conversion functions: `LogicalToWidget(Vector2)` and `WidgetToLogical(Vector2)`. Apply `Paint.Translate` + `Paint.Scale` then call `Paint.ResetTransform()` when switching to widget-pixel chrome (handles, rulers).

## Selection

- **Click** any element → selects it (replaces selection)
- **Shift+click** → adds to selection
- **Click on empty canvas** → clears selection; starts a **marquee** drag-rectangle (releases to multi-select everything that intersects)
- **Shift+click empty + drag** → additive marquee (keeps existing selection)

## Drag & resize

When an element is selected and its parent is **Absolute**:

- **Drag the element body** → moves it (`SuiMoveElementCommand`). Math is anchor-aware: BottomCenter, TopRight, Stretch — drag-down always moves the element down on screen regardless of anchor.
- **Drag a handle** (8 handles: corners + midpoints) → resizes (`SuiResizeElementCommand`). Hold `Ctrl` for aspect-ratio lock on corner handles.
- **Shift while dragging** → constrain to dominant axis (snap to horizontal-only or vertical-only)

When the parent is **Flex**, elements are flow-positioned — the canvas hides drag/resize handles. Edit the element's order in the Hierarchy panel instead.

## Pan & zoom

- **Middle-mouse drag** → pan
- **Alt+left drag** → pan (Unity / UMG convention)
- **Mouse wheel** → zoom (anchored at cursor)
- **`Ctrl+0`** → fit document to viewport

The zoom level and pan offset persist with the document (`Settings.CanvasZoom`, `CanvasPanX`, `CanvasPanY`).

## Overlays

Toggle from the mini-toolbar (or the document Settings popover):

- **Rulers** — pixel rulers along the top + left edges, adapt to zoom
- **Grid** — dot grid at `Settings.GridSize` interval (default 8 px). Auto-hides at low zoom
- **Layout Bounds** — dashed outline around every element. ~12% alpha so they read as a hint not chrome
- **Anchors** — Quad Pin marker on the selected element's anchor point. Stretch variants show a bracket + corner pins
- **Safe Area** — dashed rect inside the panel rect indicating "safe" content area for variable resolutions
- **Responsive Debug** — adds a top-right banner with detected layout issues (negative sizes, off-screen elements, oversized text, etc.)
- **Widget Info** — small pill on each element showing `name  W×H`

## Status bar

Single line at the bottom of the canvas:

```
ElementName  ·  Type  ·  WxH @ X,Y  ·  Anchor: …
```

Multi-selection: `"N elements selected"`.

Empty selection: `"Nothing selected"`.

## Reference

- Source: [`Editor/Widgets/SuiCanvasWidget.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Widgets/SuiCanvasWidget.cs) (mouse + hit-test)
- Source: [`Editor/Canvas/SuiCanvasViewport.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Canvas/SuiCanvasViewport.cs) (paint + overlays + zoom/pan)
- Source: [`Editor/Canvas/SuiCanvasRenderer.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Canvas/SuiCanvasRenderer.cs) (per-element paint)
- Source: [`Editor/Canvas/SuiLayoutSolver.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Canvas/SuiLayoutSolver.cs) (layout math)
