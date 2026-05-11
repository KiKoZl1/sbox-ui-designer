---
layout: default
title: Keyboard shortcuts
parent: Reference
nav_order: 4
---

# Keyboard shortcuts

Every hotkey wired into the SUI Designer window. Source: `[Shortcut(...)]` attributes in [Editor/SuiDesignerWindow.cs](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/SuiDesignerWindow.cs).

## File

| Shortcut | Action |
|---|---|
| `Ctrl+S` | Save |
| `Ctrl+B` | Compile |

## Edit

| Shortcut | Action |
|---|---|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+Shift+Z` | Redo (alt) |
| `Del` | Delete selected |
| `F2` | Rename selected in Hierarchy |
| `Ctrl+D` | Duplicate selected |
| `Ctrl+X` | Cut |
| `Ctrl+C` | Copy |
| `Ctrl+V` | Paste |

## Canvas

| Shortcut | Action |
|---|---|
| `Ctrl++` | Zoom in |
| `Ctrl+-` | Zoom out |
| `Ctrl+0` | Fit to screen |
| Mouse wheel | Zoom around cursor |
| Middle-drag | Pan canvas |
| Space + drag | Pan canvas (alt) |

## Selection

| Shortcut | Action |
|---|---|
| Click element | Select |
| `Shift+Click` | Add to selection (multi-select) |
| `Ctrl+Click` | Toggle in selection |
| Click empty canvas | Clear selection |
| Drag empty canvas | Marquee select |

## Drag operations on canvas

| Shortcut | Action |
|---|---|
| Drag element | Move |
| Drag with `Shift` held | Constrain to 8-degree angles (V2) |
| Drag handles | Resize |
| Drag with `Alt` held | Resize from center (V2) |

## Hierarchy widget

| Shortcut | Action |
|---|---|
| Drag-and-drop inside tree | Reparent |
| Drag-and-drop above/below | Reorder siblings |
| `F2` | Rename (with the row selected) |
| Double-click | Open rename inline |

## Notes

- All shortcuts are scoped to the SUI Designer window — they don't fire when other editor windows are focused.
- The window must have focus (click inside it once after a tab switch).
- s&box editor has its own global shortcuts (Ctrl+Shift+S = Save Scene, etc.) — those are independent of SUI Designer.

If a shortcut "doesn't work", the most likely cause is focus — click somewhere inside the SUI window first.
