---
layout: default
title: Palette
parent: User Guide
nav_order: 3
---

# Palette

The left-top panel — your element catalog.

## Layout

```
┌──────────────┐
│ 🔍 Search    │
├──────────────┤
│ ▾ COMMON     │
│   ▭ Panel    │
│   T Text     │
│   🖼 Image    │
│   ▢ Button   │
│ ▾ LAYOUT     │
│   ⫶ Horiz…   │
│   ⫯ Verti…   │
│   ▦ Grid     │
│   ◫ Overlay  │
│ ▾ GAME UI v1 │
│   ━ Progres… │
│   ⊟ Scroll…  │
│   ▦ Invent…  │
│   ▢ Invent…  │
│   ◯ ItemIcon │
│   ━ Hotbar   │
└──────────────┘
```

## Search

Top of the panel. Substring match against element type names. Categories with zero matches collapse automatically.

## Categories

### COMMON

| Type | Default size | Generates as |
|---|---|---|
| **Panel** | 100×32 | `<div class="…">` |
| **Text** | 200 (auto height) | `<label class="…">` |
| **Image** | 100×100 | `<div class="…">` with `background-image` |
| **Button** | 120×32 | `<div class="…"><label class="label">…</label></div>` |

### LAYOUT

| Type | Layout mode default | Notes |
|---|---|---|
| **HorizontalBox** | Flex, direction Row | Children pack horizontally |
| **VerticalBox** | Flex, direction Column | Children pack vertically |
| **Grid** | Flex, wrap | Wrapped flex grid using `Columns / Rows / CellWidth / CellHeight / GridGap` |
| **Overlay** | Flex container with `position: relative` | Children get absolute positioning within the overlay |

### INPUT WIDGETS (V1.5 M4)

Four interactive types that **read** user input — the first SUI types that produce values back into your Variables (via [TwoWay bindings]({% link concepts/bindings.md %})).

| Type | Engine backing | Notes |
|---|---|---|
| **TextEntry** | `Sandbox.UI.TextEntry` | Single-line text input. See [TextEntry]({% link elements/text-entry.md %}) |
| **Slider** | Fully custom markup (per D-022) | Horizontal slider — track / fill / thumb / tooltip. See [Slider]({% link elements/slider.md %}) |
| **Toggle** | `Sandbox.UI.Checkbox` | Boolean checkbox. See [Toggle]({% link elements/toggle.md %}) |
| **DropDown** | `Sandbox.UI.DropDown` | Selection dropdown. See [DropDown]({% link elements/dropdown.md %}) |

### COMPOSITION

| Type | Purpose |
|---|---|
| **SuiReference** | Embed another `.sui` document by GUID. See [SuiReference]({% link elements/sui-reference.md %}) and [Composition]({% link concepts/composition.md %}) |

### USER WIDGETS (dynamic)

Every `.sui` registered in the Asset Registry appears as its own palette item under this category — same row shape as a built-in type, dragging or clicking creates a `SuiReference` element with `SourceGuid` already bound to the picked document. Auto-refreshes when documents are added / removed / renamed.

The host document is filtered from its own list to prevent instant reference cycles.

### GAME UI (V1)

| Type | Purpose |
|---|---|
| **ProgressBar** | Fill bar with `ProgressMin`, `ProgressMax`, `ProgressPreviewValue`, `ProgressFillColor`, `ProgressDirection` |
| **ScrollPanel** | Container with overflow: scroll. Same as Flex container otherwise |
| **InventoryGrid** | Wrapped-flex slot grid for inventory UIs. Use with `InventorySlot` children |
| **InventorySlot** | Single slot with `SlotIndex`, optional `PreviewIconPath` + `PreviewCount`. M3.5 interactive states apply |
| **ItemIcon** | Standalone icon with stack count. Same render as `InventorySlot` but without slot frame. M3.5 interactive states apply |
| **Hotbar** | Like InventoryGrid but `flex-wrap: nowrap` (single row) |

## Adding elements

- **Double-click** in the Palette adds at the document root, or under the currently-selected container.
- **Drag** from the Palette onto the canvas or the hierarchy tree to place precisely (drop indicator shows where it'll land — child / before / after).

Newly-added elements:

- Get an auto-generated ID like `el_a3f9b21c` (stable, never changes).
- Get a default `Style.ClassName` derived from the type (e.g. `"text"`, `"button"`).
- Inherit the canvas's default layout for their type — Panel/Image/Text/Button = Absolute, the layout containers = Flex.

## Tooltips

Hover any palette item for a tooltip describing the type. Tooltips are centralized in [`SuiPaletteTooltips`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Widgets/SuiPaletteTooltips.cs) for easy editing.

## Type defaults

Newly-dropped elements get sensible defaults via `SuiElement.ApplyTypeDefaults()`:

- **Pointer events** — `All` for `Button` / `InventorySlot` / `ScrollPanel` / `TextEntry` / `Slider` / `Toggle` / `DropDown`; `None` for everything else.
- **Layout.Mode** — `Flex` for `HorizontalBox` / `VerticalBox` / `Grid` / `InventoryGrid` / `Hotbar`; `Absolute` otherwise.
- **FlexDirection / FlexWrap** — per type (Column for VerticalBox; NoWrap for Hotbar; Wrap for Grid; etc).
- **TransitionEnabled** — `true` by default on `Button` / `InventorySlot` / `ItemIcon` (M3.5).

See [Element type matrix]({% link reference/element-types.md %}) for the complete table.

## Reference

- Source: [`Editor/Widgets/SuiPaletteWidget.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Widgets/SuiPaletteWidget.cs)
- See also: [Element reference]({% link index.md %}#element-reference) for per-type docs.
