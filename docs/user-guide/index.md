---
layout: default
title: User Guide
nav_order: 3
has_children: true
permalink: /user-guide/
---

# User Guide

Reference for every panel and toolbar in the SUI Designer window.

The editor uses a **fixed 5-region layout** (no DockManager, no drag-to-rearrange) — by design, to keep visual position predictable:

```
┌──────────────────────────────────────────────────────────────┐
│  Top toolbar                                                 │
├──────────┬──────────────────────────────┬───────────────────┤
│ Palette  │                              │                   │
│          │   Center tabs (Designer /    │   Details panel   │
├──────────┤   Preview / Code)            │                   │
│Hierarchy │                              │                   │
│          │                              │                   │
├──────────┴──────────────────────────────┴───────────────────┤
│  Bottom tabs (Variables / Bindings / Events / Compile /     │
│              Logs)                                           │
└──────────────────────────────────────────────────────────────┘
```

The Variables / Bindings / Events tabs are new in V1.5 — they replace the old single Bindings stub and unlock M1 (Variables + Bindings), M3 (Events + Doo), and M4 (input widgets) UX. See [Variables]({% link concepts/variables.md %}), [Bindings]({% link concepts/bindings.md %}), and [Events & Actions]({% link concepts/events-and-actions.md %}) for the underlying concepts.

Pages in this section:

- [Editor tour]({% link user-guide/editor-tour.md %}) — high-level map
- [Top toolbar]({% link user-guide/top-toolbar.md %}) — Save, Compile, Test in Play, Undo, Redo, Grid, Settings
- [Palette]({% link user-guide/palette.md %}) — element catalog
- [Hierarchy]({% link user-guide/hierarchy.md %}) — tree view, reparent, lock/hide
- [Details panel]({% link user-guide/details-panel.md %}) — per-element property editor
- [Canvas]({% link user-guide/canvas.md %}) — the designer surface (paint-based)
- [Alignment tools]({% link user-guide/alignment-tools.md %}) — Align + Distribute for multi-selection
- [Compile Results]({% link user-guide/compile-results.md %}) — generated/skipped/preserved/conflicts feedback

For the underlying concepts, see [Concepts]({% link index.md %}#concepts).
