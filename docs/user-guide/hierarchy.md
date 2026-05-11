---
layout: default
title: Hierarchy
parent: User Guide
nav_order: 4
---

# Hierarchy

Tree view of the entire `.sui` document. Each row is one element.

## Layout

```
┌──────────────────────┐
│ 🔍 Search Hierarchy  │
├──────────────────────┤
│ ▾ Root        👁 🔒  │
│   ▾ Background       │
│     T Title          │
│   ━ HealthBar        │
│   ━ StaminaBar       │
└──────────────────────┘
```

## Interactions

| Action | Result |
|---|---|
| Click row | Select element (replaces selection) |
| Shift+click | Add to current selection |
| Ctrl+click | Toggle in/out of selection |
| Double-click | Rename inline (or `F2`) |
| Drag-and-drop | Reparent to another node or reorder among siblings |
| Right-click | Context menu (Rename, Duplicate, Delete, Move Up, Move Down, Reparent, Lock, Hide in Designer) |

## Row icons

Right side of each row:

- 👁 **Visibility** — toggles `Flags.HiddenInDesigner`. **Editor-only** — does not affect runtime; just hides the element from canvas paint so you can focus on other elements.
- 🔒 **Lock** — toggles `Flags.Locked`. Locked elements skip hit-testing (clicks pass through to elements behind), and their entire subtree is also unlock-able. Lets you click "through" containers without selecting them.

## Drop semantics

Drag a node onto another:

- **Drop on the middle of a row** → becomes a **child** of that node
- **Drop on the top edge** → becomes a **previous sibling** (above)
- **Drop on the bottom edge** → becomes a **next sibling** (below)

A small indicator shows which behavior will trigger.

{: .note }
Reparenting is wrapped in a `SuiReparentElementCommand` so undo restores both the parent and the previous sibling order.

## Search

Filters tree nodes by name (substring). Matching nodes plus all their ancestors stay visible (so the tree structure remains readable). Non-matches collapse.

## Right-click context menu

| Item | Action |
|---|---|
| Rename | `F2` — inline rename |
| Duplicate | `Ctrl+D` — clones with new IDs, suffix `_copy` |
| Delete | `Del` — removes element + entire subtree |
| Move Up | Reorder among siblings |
| Move Down | Reorder among siblings |
| Reparent → ▶ | Submenu of valid parents (containers in the document) |
| Lock / Unlock | Toggle `Flags.Locked` |
| Hide in Designer / Show in Designer | Toggle `Flags.HiddenInDesigner` |

## Selection sync

Selecting in the Hierarchy:

- Updates Canvas selection chrome (handles + outline)
- Updates Details panel to show the selected element's properties
- Updates status bar at the bottom of the canvas

Selecting in the Canvas:

- Highlights the matching row in Hierarchy (auto-scrolls if off-screen)

## Reference

- Source: [`Editor/Widgets/SuiHierarchyWidget.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Widgets/SuiHierarchyWidget.cs)
- Commands: `SuiReparentElementCommand`, `SuiReorderElementCommand`, `SuiRenameElementCommand`
