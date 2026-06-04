---
layout: default
title: Canvas renderer
parent: Architecture
nav_order: 3
---

# Canvas renderer
{: .no_toc }

How the editor canvas paints `.sui` documents using the Qt `Editor.Paint` API — independent of the s&box runtime CSS engine.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Two renderers — not one

There are **two** independent rendering paths for a SUI document:

1. **Canvas renderer** (`SuiCanvasRenderer`) — paints the document in the editor via `Editor.Paint` (Qt). Used in the SUI Designer window.
2. **Runtime renderer** — the s&box CSS engine renders the compiled Razor + SCSS as a `ScreenPanel`. Used at game runtime and in Test in Play.

These are entirely separate code paths. They each implement the same visual contract from different inputs:

```
                      SuiDocument
                          │
                ┌─────────┴──────────┐
                ▼                    ▼
       SuiCanvasRenderer    SuiScssGenerator
       (Editor.Paint)       (.scss + .razor)
                                    │
                                    ▼
                              s&box CSS engine
                              (Yoga + render)
```

Keeping them in agreement is a permanent task. The layout math (`SuiLayoutSolver`) is shared — both renderers ask the same solver "where is element X?" — but the *visual emission* differs.

## Why two renderers?

Why not just render in s&box and show that as the canvas?

- **Editing affordances** — selection chrome, snap guides, drag handles need to draw OVER the UI. Easier in editor paint than in CSS.
- **Hit-testing** — clicking on an element needs precise rect data, not the engine's hit reasoning.
- **No game loop** — the canvas paints synchronously on demand, not at frame rate.
- **Performance** — the editor canvas is a Qt widget. Spinning up a real ScreenPanel for every paint is heavy.
- **Independence from engine quirks** — when the engine has a bug (label rgba quirk, etc.), the canvas still shows the *intended* design.

The trade-off is exactly what you'd expect: the canvas can drift from runtime if either renderer changes without the other. Every drift becomes a known issue.

## The painting context

The canvas widget wraps an `Editor.Paint` session. Before calling the renderer it applies:

```
Paint.Translate( panOffset )
Paint.Scale( zoom, zoom )
```

After that, drawing happens in **logical pixel space** (1920×1080 default). The renderer doesn't need to know about pan or zoom — it just draws at the right logical coordinates.

The viewport widget also draws:

- The canvas background (checkerboard or single color).
- The document boundary outline.
- A safe-area overlay (if enabled).
- Selection chrome (handles, marquee) AFTER the renderer runs.

## Render order

The renderer walks the document tree depth-first. Per parent, it asks the solver for children in **paint order** (ascending ZIndex, hierarchy as tie-break) — same order as runtime CSS would composite. See [Layout solver]({% link architecture/layout-solver.md %}#render-order--zindex--hierarchy).

For each element it:

1. Reads the element's rect from `solver.Rects[el.Id]`.
2. Applies opacity / visibility — `Hidden` and `Collapsed` skip drawing entirely (Collapsed also doesn't lay out, but the solver already handled that).
3. Emits the type-specific visual.
4. Recurses into children.

## Per-type rendering rules

From the renderer comment block, here's the contract for each element type:

| Type | Visual |
|---|---|
| **Canvas** (root) | Faint outline only — the document boundary |
| **Panel** | Background-color fill + border (with optional radius) |
| **Text** | Text drawn with FontFamily / Size / Weight / Color / Align / Overflow |
| **Image** | Background fill + image via `Paint.SetBrush(Pixmap)` + tint |
| **Button** | Composed: Panel (bg + border) + centered Text label |
| **ProgressBar** | Outer Panel + inner filled bar at `PreviewValue / (Max - Min)` |
| **HorizontalBox** | No own visual (layout container) |
| **VerticalBox** | No own visual |
| **Grid** | No own visual — children handled by `SolveGrid` |
| **Overlay** | No own visual (z-stacking container) |
| **ScrollPanel** | Panel + clip-rect (V2 — for now renders as Panel) |
| **InventorySlot** | Panel with subdued background |
| **InventoryGrid** | No own visual |
| **ItemIcon** | Image rendering of `PreviewIconPath` |
| **Tooltip** | **Hidden in canvas** (runtime-only) |
| **Hotbar** | No own visual |
| **SuiReference** | Resolves `SourceGuid` via registry + recursively paints the embedded child's element tree inside the reference rect, with a purple dashed border. `SkipRootFrame` suppresses the child's outer Canvas frame so its outline does not bleed past the reference bounds. |
| **TextEntry** | Panel background + border + placeholder text rendered with subdued color; cursor indicator hidden in canvas. |
| **Slider** | Track rectangle + filled portion sized by `SliderValue / (SliderMax - SliderMin)` + thumb circle; tooltip hidden. Mirrors the runtime markup pixel-for-pixel. |
| **Toggle** | Checkbox visual: outline + check-mark glyph when `ToggleChecked` is true; optional `ToggleLabelText` drawn to the right. |
| **DropDown** | Closed-state pill with the option at `DropDownSelectedIndex` + chevron glyph. Open-state popup not drawn (canvas is static). |

"No own visual" means the container itself draws nothing; only its children paint.

## Color parsing

`ParseColor( string )` accepts:

- `#RRGGBB` — 6-digit hex
- `#RRGGBBAA` — 8-digit hex with alpha
- `#RGB` — 3-digit shorthand
- `rgb(r, g, b)` — decimal 0..255
- `rgba(r, g, b, a)` — decimal + alpha 0..1

Returns `null` for malformed input → the renderer skips emitting that fill rule (matches generator behavior — null = "no rule").

For a long time the canvas only accepted `#hex`. Adding rgba parsing closed a known bug where translucent backgrounds rendered fully transparent on the canvas while shipping fine at runtime. See [Known issues]({% link reference/known-issues.md %}) for the historical context.

## Image rendering

Images use `Paint.Draw(Rect, Pixmap, alpha, radius)` — the editor API that supports rounded corners and alpha multiplication natively.

```csharp
var pixmap = LoadPixmap( imagePath );
Paint.Draw( rect, pixmap, alpha, borderRadius );
```

The earlier implementation used `Paint.SetBrush(Pixmap)` + `Paint.DrawRect` which **tiled** the image instead of fitting. Replaced after research into the engine source confirmed `Paint.Draw(Rect, Pixmap, ...)` as the canonical path. See [memory: s&box Editor.Paint API for images](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/docs/known-issues.md).

For images that are much smaller on the canvas than their native size (heavy down-scale), the renderer CPU pre-resizes the pixmap to avoid aliasing. Triggered when `effective display size < native size × threshold`.

## Text rendering

Two paths:

1. **`SuiTextSizeMode.Auto`** — text size determined by `Paint.MeasureText` (already done in the solver). The renderer just draws the text at the measured size.
2. **`SuiTextSizeMode.Fixed`** / **`AutoHeightWrap`** — text fits inside the explicit W/H. The renderer applies `text-align` + `vertical-align` to position the text inside the box.

Font selection:

```csharp
var fontName = el.Props.FontFamily ?? Theme.DefaultFont;
var fontSize = el.Props.FontSize > 0 ? el.Props.FontSize : 14f;
var weight = MapFontWeight( el.Props.FontWeight );
Editor.Paint.SetFont( fontName, fontSize, weight );
```

`Theme.DefaultFont` matches what s&box uses as the default UI font, so an unspecified font renders the same on both sides.

## Per-element unique class

When the renderer paints a `Panel` and the SCSS generator emits styles for the same panel, they target the same logical element via `sui-<id>`. This is the unique class added to every generated element to avoid sibling cascade collisions.

From `SuiRazorGenerator`:

```html
<div class="sui-elem-42 my-user-class">
```

The canvas renderer doesn't need this class — it picks values from the element directly. The class matters for the generated SCSS so that `.slot-icon` rules don't bleed across siblings (every slot has the same `ClassName="slot"` but different `sui-<id>`).

## What the canvas deliberately doesn't show

- **`box-shadow`** in `.User.scss` — sidecar isn't parsed by the canvas; it's runtime-only.
- **Hover / pressed states** — canvas is a static preview, no interaction.
- **`:intro` / `:outro` transitions** — not animated.
- **Tooltip pop-ups** — runtime-only.
- **Children clipped by ScrollPanel** — V2 will add clipping.

The status bar surfaces a hint when the canvas might be lying ("`.User.scss` not previewed — Compile + Play to test").

## See also

- [Layout solver]({% link architecture/layout-solver.md %}) — what feeds the renderer
- [Generator pipeline]({% link architecture/generator.md %}) — the other consumer of layout
- [Test in Play]({% link workflows/test-in-play.md %}) — see what the *runtime* would render
