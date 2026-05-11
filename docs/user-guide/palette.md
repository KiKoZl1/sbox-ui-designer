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

### GAME UI (V1)

| Type | Purpose |
|---|---|
| **ProgressBar** | Fill bar with `ProgressMin`, `ProgressMax`, `ProgressPreviewValue`, `ProgressFillColor`, `ProgressDirection` |
| **ScrollPanel** | Container with overflow: scroll. Same as Flex container otherwise |
| **InventoryGrid** | Wrapped-flex slot grid for inventory UIs. Use with `InventorySlot` children |
| **InventorySlot** | Single slot with `SlotIndex`, optional `PreviewIconPath` + `PreviewCount` |
| **ItemIcon** | Standalone icon with stack count. Same render as `InventorySlot` but without slot frame |
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

## Reference

- Source: [`Editor/Widgets/SuiPaletteWidget.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Widgets/SuiPaletteWidget.cs)
- See also: [Element reference]({% link index.md %}#element-reference) for per-type docs.
