---
layout: default
title: Details panel
parent: User Guide
nav_order: 5
---

# Details panel
{: .no_toc }

The right-hand panel. Shows everything about the currently-selected element. Edits commit instantly to the document (with undo support).
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Top bar

```
┌──────────────────────────┐
│ 🔍 Search Details        │
├──────────────────────────┤
```

Filters rows by label (substring match). Useful when looking for a specific property in a long element type.

## Sections

Each section is collapsible (chevron at the left of the header). The visible sections depend on the element type and on its layout mode.

### Common

Always visible.

| Field | Description |
|---|---|
| **Name** | Element identifier — shown in Hierarchy. Becomes the C# field name when this element is a `SuiReference` (parent wrapper field) or has `Expose as Variable` set (`@ref` field on the renderer). Must be unique within the document (M2-K3 auto-suffixes duplicates). |
| **Tooltip Text** | Text shown when the player hovers this element in-game. |
| **Expose as Variable** | (V1.5 M3) Emits `@ref="<ElementName>"` on the Razor tag and declares a typed property on the renderer Panel — reach the live element from `wrapper.View?.<ElementName>` for imperative pokes. See [Events & Actions]({% link concepts/events-and-actions.md %}). |

### Transform

Always visible (except on the root Canvas, which has its own Canvas section in the Common area).

The fields shown depend on **two independent decisions**:

1. **Position fields** (Anchor, Position, Size, Pivot, Z Index) — shown when the **parent's** Mode is Absolute. Hidden when parent is Flex (the parent flows this element, X/Y are ignored).
2. **Flex container fields** (Direction, Justify, Align Items, Wrap, Gap) — shown when **this element's** Mode is Flex. Hidden when Absolute.

An element can need both — e.g. a Hotbar (Flex container) at BottomCenter of a Canvas (Absolute parent) — and gets **both sections** in the Details panel.

Common Transform fields:

| Field | Notes |
|---|---|
| **Mode** | Absolute (manual positioning) or Flex (children flow) |
| **Position** (X, Y) | Offset from anchor reference point, in logical pixels |
| **Size** (W, H) | Width / Height in logical pixels |
| **Anchor** | 3×3 grid picker — TopLeft / TopCenter / … / Stretch variants |
| **Pivot** (X, Y) | 0..1 fraction inside the element. Affects rotation + meaning of Position |
| **Z Index** | Render order within parent (higher draws on top) |
| **Margin** (L, T, R, B) | Outer offsets, used with Stretch anchors |
| **Padding** (L, T, R, B) | Inner offsets that push children inward |

Flex container fields (only when Mode = Flex):

| Field | Notes |
|---|---|
| **Direction** | Row / Column / RowReverse / ColumnReverse |
| **Justify** | FlexStart / Center / FlexEnd / SpaceBetween / SpaceAround / SpaceEvenly |
| **Align Items** | FlexStart / Center / FlexEnd / Stretch / Baseline |
| **Wrap** | NoWrap / Wrap / WrapReverse |
| **Gap** | Pixels between children |

### Appearance

Always visible.

| Field | Notes |
|---|---|
| **Background** | Color picker — solid, rgba, or hex |
| **Border** | Color picker for stroke |
| **Border Width** | px. Emit only with Border set |
| **Border Radius** | px — corner rounding |
| **Opacity** | 0..1. Cascades to children (CSS-like) |
| **Visibility** | Visible / Hidden (opacity 0) / Collapsed (display none) |
| **Pointer Events** | None (clicks pass through) / All (catches input). Default: None |
| **Overflow** | Visible / Hidden / Scroll |

### Image, Text, Progress, Grid, Inventory

Type-specific sections. See the [element reference]({% link index.md %}#element-reference) for what each shows.

### Input widgets (V1.5 M4)

For `TextEntry` / `Slider` / `Toggle` / `DropDown` the Details panel grows a per-type section:

- **TextEntry** — PlaceholderText, MaxLength, ReadOnly, PreviewValue (design-time only).
- **Slider** — Min / Max / Step / TrackColor / FillColor / HandleColor / ShowValue + tooltip colors / PreviewValue.
- **Toggle** — Checked (preview) + LabelText.
- **DropDown** — Options list editor + SelectedIndex (preview).

See the per-type pages: [TextEntry]({% link elements/text-entry.md %}), [Slider]({% link elements/slider.md %}), [Toggle]({% link elements/toggle.md %}), [DropDown]({% link elements/dropdown.md %}).

### Interactive States (V1.5 M3.5)

Buttons / InventorySlots / ItemIcons expose 4 per-state collapsible dropdowns under Appearance — Hover / Pressed / Disabled / Focused. Each lets you override background color, border, text color, scale, etc., and headers tag with `(set)` when any field is authored. A **Clear State** button blanks an override.

Also in this section:

- **Transition** — `TransitionEnabled` + `TransitionDuration` (seconds, 0.15 default).
- **Hover sound / Press sound** — SoundEvent paths fired on state ingress.
- **Cursor** — preset (Default / Pointer / NotAllowed / Wait / Text / Move / Crosshair / Help / None).
- **Button Shape** — `Rectangle / Square / Round / Pill / Custom` — overrides BorderRadius.
- **Background Image / Size** — image fill mode (`Cover / Contain / Stretch / Custom`).
- **Snap to image aspect** — helper that resizes the element to match the image aspect ratio.

See [Interactive states]({% link concepts/interactive-states.md %}) for the full mental model.

### Bindings inline indicator

Any property that can be bound shows a small chain icon next to its label. Click it to open the [Bind dialog]({% link workflows/binding-a-variable.md %}). A bound property shows the Variable name + converter chain summary instead of the literal value; a broken binding (missing Variable / unknown converter) shows a red ⚠ icon.

## Editing

- Each field commits **on change** via a `SuiSetPropertyCommand<T>` — every edit is in the undo stack.
- **Color picker** is a custom popup (HSV box + sliders + hex + RGBA fields + presets).
- **Anchor picker** is a 3×3 grid with a "Stretch…" button on the right for the Stretch variants.
- Numeric fields support drag-to-scrub (click and drag horizontally to change the value).

## Multi-selection

When multiple elements are selected:

- Common fields with **shared values** show as normal.
- Fields with **mixed values** show as `(mixed)` placeholder.
- Editing a field applies to **all selected** elements.

## Reference

- Source: [`Editor/Widgets/SuiDetailsWidget.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Widgets/SuiDetailsWidget.cs)
- Tooltips: [`Editor/Widgets/SuiDetailsTooltips.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Widgets/SuiDetailsTooltips.cs)
- Field widgets: [`Editor/Widgets/SuiDetailsFields.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Widgets/SuiDetailsFields.cs)
