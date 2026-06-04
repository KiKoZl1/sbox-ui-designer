---
layout: default
title: User Guide
nav_order: 3
has_children: true
permalink: /user-guide/
---

# User Guide

Reference for every panel and toolbar in the SUI Designer window.

<div class="section-grid" markdown="0">
  <a class="section-card" href="{% link user-guide/editor-tour.md %}">
    <span class="card-tag">USER GUIDE</span>
    <h3>Editor tour</h3>
    <p>A 2-minute map of the editor window. For deep dives, follow the links to per-panel pages.</p>
  </a>
  <a class="section-card" href="{% link user-guide/top-toolbar.md %}">
    <span class="card-tag">USER GUIDE</span>
    <h3>Top toolbar</h3>
    <p>The horizontal strip below the menu bar — Save, Compile, Test in Play, Undo, Redo, Grid, Settings. Every action also via menu or shortcut.</p>
  </a>
  <a class="section-card" href="{% link user-guide/palette.md %}">
    <span class="card-tag">USER GUIDE</span>
    <h3>Palette</h3>
    <p>The left-top panel — your element catalog. Drag-and-drop or click to spawn elements onto the canvas.</p>
  </a>
  <a class="section-card" href="{% link user-guide/hierarchy.md %}">
    <span class="card-tag">USER GUIDE</span>
    <h3>Hierarchy</h3>
    <p>Tree view of the entire <code>.sui</code> document. Reparent, lock, hide. Each row is one element.</p>
  </a>
  <a class="section-card" href="{% link user-guide/details-panel.md %}">
    <span class="card-tag">USER GUIDE</span>
    <h3>Details panel</h3>
    <p>The right-hand panel. Shows everything about the currently-selected element. Edits commit instantly with undo support.</p>
  </a>
  <a class="section-card" href="{% link user-guide/canvas.md %}">
    <span class="card-tag">USER GUIDE</span>
    <h3>Canvas</h3>
    <p>The paint-based designer surface — center tab "Designer". Selection chrome, layout bounds, rulers, and the document area outline.</p>
  </a>
  <a class="section-card" href="{% link user-guide/alignment-tools.md %}">
    <span class="card-tag">USER GUIDE</span>
    <h3>Alignment tools</h3>
    <p>Multi-select 2+ (or 3+ for distribute) elements with the same parent, then trigger an alignment or distribution op.</p>
  </a>
  <a class="section-card" href="{% link user-guide/compile-results.md %}">
    <span class="card-tag">USER GUIDE</span>
    <h3>Compile Results</h3>
    <p>Third tab in the bottom panel. Classifies every file <code>SuiCompileWriter</code> touched on the last <code>Ctrl+B</code>.</p>
  </a>
</div>

## Editor layout

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

For the underlying concepts, see [Concepts]({% link concepts/index.md %}).
