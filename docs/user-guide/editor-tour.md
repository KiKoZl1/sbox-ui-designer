---
layout: default
title: Editor tour
parent: User Guide
nav_order: 1
---

# Editor tour
{: .no_toc }

A 2-minute map of the editor window. For deep dives, follow the links to per-panel pages.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Menu bar

Top of the window:

- **File** — Save, Compile, Change Output Folder, Open Generated Folder, Close
- **Edit** — Undo, Redo, Cut, Copy, Paste, Duplicate, Delete, Rename, Align ▶ (submenu with 8 ops)
- **View** — Zoom In, Zoom Out, Fit to Screen
- **Tools** — Validate Document, Regenerate Preview, Clean Preview Cache, Clean All SUI Caches, Install Sample Documents
- **Help** — Open PRD

## Top toolbar

Right below the menu bar. From left to right:

| Button | Action | Shortcut |
|---|---|---|
| **Save** | Persist the `.sui` JSON | `Ctrl+S` |
| **Compile** | Generate `.razor` + `.razor.scss` to the output folder | `Ctrl+B` |
| **▶ Test in Play** | Compile, load preview stage scene, enter Play mode with your UI mounted | — |
| **Undo** | Revert last edit | `Ctrl+Z` |
| **Redo** | Reapply | `Ctrl+Y` |
| **Grid** | Toggle the canvas grid overlay | — |
| **Settings** (right-aligned) | Project Output settings, preview behavior, generator options | — |

See [Top toolbar]({% link user-guide/top-toolbar.md %}) for details on each.

## Center tabs

Three views of the same document:

- **Designer** — paint-based canvas where you visually compose the UI
- **Preview** — embedded `SceneRenderingWidget` that renders the live runtime Razor/SCSS (simulated lifecycle)
- **Code** — read-only viewer of the generated `.razor` and `.razor.scss`, with **Refresh** to regenerate

Below the tabs sits the **mini-toolbar** (only visible in Designer mode):

- **Screen** — dropdown to switch the preview resolution (1920×1080, 1280×720, custom, …)
- **Zoom** — canvas zoom level (10% — 800%)
- **Snap** — snap-to-grid step (off / 1px / 2px / 4px / 8px / 16px / 32px)
- **Rulers** toggle — pixel rulers along the top & left edges
- **Bug** toggle — Responsive Debug overlay (live validator + safe area + bounds)
- **Anchors** toggle — show Quad Pin marker on the selected element's anchor
- **Bounds** toggle — dashed outline around every element's layout bounds
- **Align ▾** — 6 alignment ops for ≥2 selected elements with the same parent
- **Distribute ▾** — Horizontal / Vertical distribution for ≥3 elements
- **Fit** — fit the canvas to the viewport (`Ctrl+0`)

## Left sidebar

Vertical stack of two panels:

### Palette (top)

Categorized list of element types. Double-click adds at root (or under the selected container). Drag drops into the hierarchy or canvas. Categories:

- **COMMON** — Panel, Text, Image, Button
- **LAYOUT** — HorizontalBox, VerticalBox, Grid, Overlay
- **INPUT WIDGETS** (V1.5 M4) — TextEntry, Slider, Toggle, DropDown
- **COMPOSITION** — SuiReference
- **USER WIDGETS** (dynamic) — every `.sui` in the project as its own palette item
- **GAME UI (V1)** — ProgressBar, ScrollPanel, InventoryGrid, InventorySlot, ItemIcon, Hotbar

Search bar at the top filters by name (substring match). See [Palette]({% link user-guide/palette.md %}).

### Hierarchy (bottom)

Tree view of the document. Right-click any node for context menu (Rename, Duplicate, Delete, Lock, Hide in Designer, Move Up / Down / Reparent…). Drag-and-drop reparents elements.

Icons on each row (right side):

- 👁 visible toggle (HiddenInDesigner flag — only affects canvas paint, not runtime)
- 🔒 lock toggle (prevents canvas clicks from selecting it; click-through to siblings/parents)

See [Hierarchy]({% link user-guide/hierarchy.md %}).

## Right sidebar

The **Details panel** — collapsible sections for the currently-selected element:

- **Common** — Name, Tooltip Text, Expose as Variable (V1.5 M3)
- **Transform** — Mode (Absolute/Flex), Position, Size, Anchor (3×3 picker), Pivot, Z Index, Margin, Padding, and (when Flex) Direction, Justify, Align Items, Wrap, Gap
- **Appearance** — Background, Border, Border Width, Border Radius, Opacity, Visibility, Pointer Events, Overflow. For interactive types (Button / InventorySlot / ItemIcon) also Hover/Pressed/Disabled/Focused dropdowns + Transition + Cursor + Button Shape + Background Size (V1.5 M3.5).
- **Image / Text / Progress / Grid / Inventory / TextEntry / Slider / Toggle / DropDown** — type-specific sections.

Bound properties show an inline chain icon + Variable name. See [Details panel]({% link user-guide/details-panel.md %}).

Each section is collapsible. The search bar at the top filters rows by label. See [Details panel]({% link user-guide/details-panel.md %}).

## Bottom panel

A 5-tab strip (V1.5):

- **Variables** — declare typed UI-local state. Add/Edit/Delete + IsPublic flag for parent-settable params. See [Variables]({% link concepts/variables.md %}).
- **Bindings** — list every binding in the document with target element/property → source Variable → converter chain → mode + trigger. Click to edit. See [Bindings]({% link concepts/bindings.md %}).
- **Events** — every wired event in the document with Code/Doo mode, handler name, target element. See [Events & Actions]({% link concepts/events-and-actions.md %}).
- **Compile Results** — last compile run's classification (Generated / Skipped / Preserved / User-Owned / Conflicts / Obsolete / Orphans deleted). Also surfaces broken bindings + name collisions.
- **Logs** — points to the engine console for `[Sui]` / `[SuiPreviewMount]` / `[SuiPreviewLauncher]` messages.

(The "Animations" tab is gone in V1.5 — the V2 animation timeline will sit elsewhere.)

## Status bar

Single-line strip at the very bottom of the Canvas region:

```
ElementName  ·  Type  ·  WxH  @ X,Y  ·  Anchor: …
```

Updates live as you select / multi-select / move elements. Shows `Nothing selected` when nothing is focused.

## Keyboard shortcuts

See the complete list in [Reference · Keyboard shortcuts]({% link reference/keyboard-shortcuts.md %}). Top hits:

- `Ctrl+S` Save · `Ctrl+B` Compile · `Ctrl+Z` Undo · `Ctrl+Y` Redo
- `Ctrl+C` Copy · `Ctrl+V` Paste · `Ctrl+D` Duplicate · `Del` Delete · `F2` Rename
- `Ctrl++` Zoom in · `Ctrl+-` Zoom out · `Ctrl+0` Fit to screen

## Next

- [Top toolbar]({% link user-guide/top-toolbar.md %})
- [Palette]({% link user-guide/palette.md %})
- [Canvas]({% link user-guide/canvas.md %})
