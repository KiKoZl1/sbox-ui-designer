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

## The 21 types (V1.5)

| Type | Category | Razor output | Has children |
|---|---|---|---|
| **Canvas** | Root | `<root>` selector | yes (root only) |
| **Panel** | Container | `<div>` | yes |
| **Overlay** | Container (stacking) | `<div>` | yes |
| **Text** | Leaf | `<label>` | no |
| **Image** | Leaf | `<div>` w/ bg-image | no |
| **Button** | Interactive | `<div>` w/ label | no (label is intrinsic) |
| **HorizontalBox** | Flex container | `<div class="hbox">` | yes |
| **VerticalBox** | Flex container | `<div class="vbox">` | yes |
| **Grid** | Container | `<div>` (wrapped flex) | yes |
| **ScrollPanel** | Container | `<div>` w/ overflow:scroll | yes |
| **ProgressBar** | Leaf-ish | `<div><div class="fill" /></div>` | no |
| **InventoryGrid** | Container | `<div>` (wrapped flex) | yes (slots) |
| **InventorySlot** | Interactive container | `<div>` (M3.5 states) | yes (icon, count) |
| **ItemIcon** | Leaf-interactive | `<div>` w/ bg-image (M3.5 states) | no |
| **Tooltip** | Hidden runtime | not emitted in canvas | yes |
| **Hotbar** | Flex container | `<div>` | yes (slots) |
| **SuiReference** | Composition (V1.5) | `<ChildPanel ... />` tag | n/a (resolved at compile) |
| **TextEntry** | Input widget (V1.5 M4) | `Sandbox.UI.TextEntry` | no |
| **Slider** | Input widget (V1.5 M4) | custom track/fill/thumb/tooltip divs (per D-022) | no |
| **Toggle** | Input widget (V1.5 M4) | `Sandbox.UI.Checkbox` | no |
| **DropDown** | Input widget (V1.5 M4) | `Sandbox.UI.DropDown` | no |

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
- **Button** — interactive box with a label. Auto-sets `pointer-events: all`. M3.5 interactive states (Hover / Pressed / Disabled / Focused) + Transition + Sound + Cursor + ButtonShape + BackgroundSize.
- **HorizontalBox** — flex container with `direction: row`. Children flow left-to-right.
- **VerticalBox** — flex container with `direction: column`. Children flow top-to-bottom.
- **Grid** — wrapped flex container with regular tiles. Use for any "N columns × M rows" layout.
- **ScrollPanel** — overflow scrollable container. Catches scroll wheel + drag.
- **ProgressBar** — horizontal/vertical bar with a filled portion. PreviewValue shown in editor. All fields bindable.
- **InventoryGrid** — Grid configured for inventory layouts. Sets up CellW/H/Gap with sensible defaults.
- **InventorySlot** — single inventory slot. Holds an ItemIcon. Catches clicks. M3.5 interactive states.
- **ItemIcon** — image + count badge. Used inside InventorySlot or standalone. M3.5 interactive states.
- **Tooltip** — runtime-only popup. Hidden in canvas (deferred — V1.6).
- **Hotbar** — single-row flex container of fixed slots. Like Grid but row-only and no wrap.
- **SuiReference** (V1.5) — embeds another `.sui` doc by `SourceGuid`. Recursive paint on the canvas. ForEach for dynamic lists. See [Composition]({% link concepts/composition.md %}).
- **TextEntry** (V1.5 M4) — single-line text input backed by `Sandbox.UI.TextEntry`. `Value` TwoWay-bindable.
- **Slider** (V1.5 M4) — fully custom track / fill / thumb / tooltip markup (per D-022). `Value` TwoWay-bindable.
- **Toggle** (V1.5 M4) — boolean checkbox backed by `Sandbox.UI.Checkbox`. `Checked` TwoWay-bindable.
- **DropDown** (V1.5 M4) — selection dropdown backed by `Sandbox.UI.DropDown`. `Value` (int via `Option.Value` index) TwoWay-bindable per D-024.

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
- [SuiReference]({% link elements/sui-reference.md %}) — V1.5
- [TextEntry]({% link elements/text-entry.md %}) — V1.5 M4
- [Slider]({% link elements/slider.md %}) — V1.5 M4
- [Toggle]({% link elements/toggle.md %}) — V1.5 M4
- [DropDown]({% link elements/dropdown.md %}) — V1.5 M4

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
