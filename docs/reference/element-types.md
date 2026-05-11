---
layout: default
title: Element types
parent: Reference
nav_order: 3
---

# Element types
{: .no_toc }

Every element type, what it does, and what fields it cares about.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The 16 types

| Type | Category | Razor output | Has children |
|---|---|---|---|
| **Canvas** | Root | `<MyHud>` (root selector) | yes (root only) |
| **Panel** | Container | `<div>` | yes |
| **Overlay** | Container (stacking) | `<div>` | yes |
| **Text** | Leaf | `<label>` | no |
| **Image** | Leaf | `<div>` w/ bg-image | no |
| **Button** | Interactive | `<button>` | no (label is intrinsic) |
| **HorizontalBox** | Flex container | `<div class="hbox">` | yes |
| **VerticalBox** | Flex container | `<div class="vbox">` | yes |
| **Grid** | Container | `<div>` (wrapped flex) | yes |
| **ScrollPanel** | Container | `<div>` w/ overflow:scroll | yes |
| **ProgressBar** | Leaf-ish | `<div><div class="fill" /></div>` | no |
| **InventoryGrid** | Container | `<div>` (wrapped flex) | yes (slots) |
| **InventorySlot** | Interactive container | `<div>` | yes (icon, count) |
| **ItemIcon** | Leaf | `<div>` w/ bg-image | no |
| **Tooltip** | Hidden runtime | not emitted in canvas | yes |
| **Hotbar** | Flex container | `<div>` | yes (slots) |

## Field matrix

✓ = relevant for this type. Blank = ignored by generator and inspector.

### Layout (always relevant)

Every type uses `Layout.Mode`, X, Y, Width, Height, Anchor, Pivot, ZIndex, Margin, Padding.

Flex containers also use FlexDirection, JustifyContent, AlignItems, FlexWrap, Gap.

### Style (always relevant)

Every type uses `Style.ClassName`, CustomClasses, BackgroundColor, BorderColor, BorderWidth, BorderRadius, Opacity, Visibility, PointerEvents, Overflow.

### Props per type

| | Text | FontSize | Color | TextAlign | TextSizeMode | ImagePath | Tint | FitMode | Columns | CellW/H | GridGap | ButtonText | ProgressMin/Max | FillColor | ProgressDirection | SlotIndex | PreviewIconPath | PreviewCount |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Canvas** | | | | | | | | | | | | | | | | | | |
| **Panel** | | | | | | | | | | | | | | | | | | |
| **Overlay** | | | | | | | | | | | | | | | | | | |
| **Text** | ✓ | ✓ | ✓ | ✓ | ✓ | | | | | | | | | | | | | |
| **Image** | | | | | | ✓ | ✓ | ✓ | | | | | | | | | | |
| **Button** | | ✓ | ✓ | ✓ | | | | | | | | ✓ | | | | | | |
| **HorizontalBox** | | | | | | | | | | | | | | | | | | |
| **VerticalBox** | | | | | | | | | | | | | | | | | | |
| **Grid** | | | | | | | | | ✓ | ✓ | ✓ | | | | | | | |
| **ScrollPanel** | | | | | | | | | | | | | | | | | | |
| **ProgressBar** | | | | | | | | | | | | | ✓ | ✓ | ✓ | | | |
| **InventoryGrid** | | | | | | | | | ✓ | ✓ | ✓ | | | | | | | |
| **InventorySlot** | | | | | | | | | | | | | | | | ✓ | ✓ | ✓ |
| **ItemIcon** | | | | | | | ✓ | ✓ | | | | | | | | | ✓ | ✓ |
| **Tooltip** | | | | | | | | | | | | | | | | | | |
| **Hotbar** | | | | | | | | | ✓ | ✓ | ✓ | | | | | | | |

## Per-type one-liners

- **Canvas** — root container. Always fills the panel. Exactly one per document. Cannot be deleted.
- **Panel** — generic container. The blank slate. Use when none of the specialized types fit.
- **Overlay** — z-stacking container. Children are absolute-positioned and overlap by ZIndex. Use for HUDs where 5 elements share the same area.
- **Text** — single-line or wrapped text. Auto-sizes to content by default.
- **Image** — render a texture. Path resolved relative to project root.
- **Button** — interactive box with a label. Auto-sets `pointer-events: all`. Hover/active states via `.User.scss`.
- **HorizontalBox** — flex container with `direction: row`. Children flow left-to-right.
- **VerticalBox** — flex container with `direction: column`. Children flow top-to-bottom.
- **Grid** — wrapped flex container with regular tiles. Use for any "N columns × M rows" layout.
- **ScrollPanel** — overflow scrollable container. Catches scroll wheel + drag.
- **ProgressBar** — horizontal/vertical bar with a filled portion. PreviewValue shown in editor.
- **InventoryGrid** — Grid configured for inventory layouts. Sets up CellW/H/Gap with sensible defaults.
- **InventorySlot** — single inventory slot. Holds an ItemIcon. Catches clicks.
- **ItemIcon** — image + count badge. Used inside InventorySlot or standalone.
- **Tooltip** — runtime-only popup. Hidden in canvas (no preview).
- **Hotbar** — single-row flex container of fixed slots. Like Grid but row-only and no wrap.

## Per-type detail pages

Each type has its own page with examples, common patterns, and gotchas:

- [Canvas]({% link elements/canvas.md %})
- [Panel]({% link elements/panel.md %})
- [Overlay]({% link elements/overlay.md %})
- [Text]({% link elements/text.md %})
- [Image]({% link elements/image.md %})
- [Button]({% link elements/button.md %})
- [HorizontalBox]({% link elements/horizontal-box.md %})
- [VerticalBox]({% link elements/vertical-box.md %})
- [Grid]({% link elements/grid.md %})
- [ScrollPanel]({% link elements/scroll-panel.md %})
- [ProgressBar]({% link elements/progress-bar.md %})
- [InventoryGrid]({% link elements/inventory-grid.md %})
- [InventorySlot]({% link elements/inventory-slot.md %})
- [ItemIcon]({% link elements/item-icon.md %})
- [Hotbar]({% link elements/hotbar.md %})

## Type defaults

When the user drops a new element from the Palette, `SuiElement.ApplyTypeDefaults()` sets sensible starting values:

- **Pointer events**: All for Button/InventorySlot/ScrollPanel; None for everything else.
- **Layout.Mode**: Flex for boxes/grids/hotbar; Absolute for everything else.
- **FlexDirection**: Column for VerticalBox; Row for HorizontalBox/Hotbar/Grid/InventoryGrid; default otherwise.
- **FlexWrap**: Wrap for Grid/InventoryGrid; NoWrap for Hotbar.
- **Hotbar.Rows**: forced to 1.
- **Text/Button**: placeholder text "Text" / "Button" if empty.

These defaults don't lock anything in — every value is editable in the Details panel.

## See also

- [SUI JSON schema]({% link reference/sui-json-schema.md %}) — the on-disk format
- [Document model]({% link architecture/document-model.md %}) — internal representation
