---
layout: default
title: Top toolbar
parent: User Guide
nav_order: 2
---

# Top toolbar
{: .no_toc }

The horizontal strip below the menu bar. Every action here is also accessible via the menu or keyboard shortcut.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Layout

Left-to-right grouping (separators between groups):

```
[Save] [Compile]  |  [▶ Test in Play]  |  [Undo] [Redo]  |  [Grid]              [Settings]
```

## Save

`Ctrl+S`. Writes the in-memory `SuiDocument` back to the `.sui` file on disk via the standard s&box `Asset.SaveToDisk` path.

The save also runs `SuiDocumentValidator` — validation errors don't block the save, but appear in the engine console as warnings (`[Sui] validation: …`).

{: .warning }
**Save is blocked while the load is in a corrupted state.** If a `.sui` file was hand-edited or has invalid enum values, the editor's `AssetOpen` detects the size mismatch and refuses to save back — log says `[Sui] save BLOCKED — original file failed to parse`. Fix the JSON, then reopen.

## Compile

`Ctrl+B`. Runs `SuiGenerationPipeline` and writes the result to disk through `SuiCompileWriter`.

The first compile opens a folder picker (defaults to `Code/UI/`). Subsequent compiles use the saved folder, accessible via **File → Change Output Folder**.

Outputs per document:

- `<ClassName>.razor` — Razor template with `@inherits PanelComponent`
- `<ClassName>.razor.scss` — SCSS stylesheet (with `@import "<ClassName>.User.scss"` at the bottom)
- `<ClassName>.User.scss` — user-owned customization file. Created once, never overwritten.

The **Compile Results** tab at the bottom shows what happened to each file (see [Compile Results]({% link user-guide/compile-results.md %})).

## ▶ Test in Play

Compiles to a sandbox path, then opens the bundled `preview_stage.scene` and triggers `EditorScene.Play()`. See [Test in Play]({% link getting-started/test-in-play.md %}) for the full flow.

{: .note }
**No keyboard shortcut** — Test in Play is deliberately a deliberate, explicit action because it switches the active scene context.

## Undo / Redo

`Ctrl+Z` / `Ctrl+Y`. SUI Designer uses a per-document `SuiCommandStack` — every edit (move, resize, property change, add, delete, reparent) goes through a `ISuiCommand` so undo is reliable across all panels.

Multi-selection drag commits one composite command (each member's move is its own command, but they're emitted together so undo unwinds the whole group).

{: .tip }
The undo stack is per-document. Closing and reopening a `.sui` clears it.

## Grid

Toggles the dot-grid overlay on the canvas. The step matches the **Snap** value in the mini-toolbar (8 px default). The overlay auto-hides when the zoomed dot spacing falls below ~4 widget pixels (so it doesn't become a solid haze at low zoom).

## Settings (right-aligned)

Opens the document Settings popover:

- **Output** — namespace, class name, root folder
- **Preview** — debounce ms, auto-preview toggle
- **Canvas** — zoom, pan persisted with the document
- **Designer overlays** — Rulers, Grid, Safe Area, Bounds, Anchors, Responsive Debug

Most of these are duplicated in the mini-toolbar (faster access during design). The Settings popover is for less-frequent adjustments and for things that survive document close/reopen.

## Reference

- Source: [`Editor/SuiDesignerWindow.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/SuiDesignerWindow.cs) (`BuildToolBar`, `BuildMenuBar`)
- Shortcut declarations: see same file, ~line 1129+
