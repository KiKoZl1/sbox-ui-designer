---
layout: default
title: Home
nav_order: 1
description: "SUI Designer — visual UI editor for s&box"
permalink: /
---

<p align="center">
  <img src="{{ '/assets/hero.png' | relative_url }}" alt="SUI Designer — Visual UI Editor for s&box" style="max-width: 100%; height: auto; border-radius: 8px;" />
</p>

# SUI Designer
{: .fs-9 }

A visual UI editor for [s&box](https://sbox.game/) — design `.sui` documents in a paint-based canvas, generate idiomatic Razor + SCSS, preview live in Play mode.
{: .fs-6 .fw-300 }

[Get started now](#getting-started){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View on GitHub](https://github.com/KiKoZl1/sbox-ui-designer){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## What it does

SUI Designer turns the manual loop of writing a Razor file by hand, editing SCSS, hot-reloading the engine, and hoping the layout works into a **WYSIWYG** workflow:

1. Drop elements onto a canvas (Panel, Image, Text, Button, ProgressBar, Hotbar, InventoryGrid, …)
2. Edit position, size, anchor, style, layout in a Details panel
3. See the result instantly — paint-based canvas + on-demand runtime preview
4. **Test in Play** opens a real Play-mode scene with a TPS player and mounts your UI as a `ScreenPanel`
5. Compile to your project's `Code/` folder, with full ownership control — generated files marked, user-edited `.User.scss` sidecar protected

The `.sui` document is JSON-backed and version-controlled. The generator emits readable, hand-editable Razor + SCSS that compiles in any s&box game.

## Getting started

If you've never opened SUI Designer before, start here:

- [Installation]({% link getting-started/install.md %}) — bring the addon into your s&box project (2 min)
- [Your first UI]({% link getting-started/your-first-ui.md %}) — build a HUD from scratch (10 min)
- [Test in Play]({% link getting-started/test-in-play.md %}) — see your UI on a real player (3 min)

## User guide

End-to-end reference for the editor and every panel element.

- [Editor tour]({% link user-guide/editor-tour.md %})
- [Palette]({% link user-guide/palette.md %})
- [Hierarchy]({% link user-guide/hierarchy.md %})
- [Details panel]({% link user-guide/details-panel.md %})
- [Canvas]({% link user-guide/canvas.md %})
- [Top toolbar]({% link user-guide/top-toolbar.md %})
- [Alignment tools]({% link user-guide/alignment-tools.md %})
- [Compile Results]({% link user-guide/compile-results.md %})

## Element reference

One page per palette type. Properties, defaults, generated output.

- [Canvas]({% link elements/canvas.md %}) (root)
- [Panel]({% link elements/panel.md %})
- [Text]({% link elements/text.md %})
- [Image]({% link elements/image.md %})
- [Button]({% link elements/button.md %})
- [HorizontalBox]({% link elements/horizontal-box.md %})
- [VerticalBox]({% link elements/vertical-box.md %})
- [Overlay]({% link elements/overlay.md %})
- [Grid]({% link elements/grid.md %})
- [ScrollPanel]({% link elements/scroll-panel.md %})
- [ProgressBar]({% link elements/progress-bar.md %})
- [InventoryGrid]({% link elements/inventory-grid.md %})
- [InventorySlot]({% link elements/inventory-slot.md %})
- [ItemIcon]({% link elements/item-icon.md %})
- [Hotbar]({% link elements/hotbar.md %})

## Concepts

- [Layout modes (Absolute vs Flex)]({% link concepts/layout-modes.md %})
- [Anchors and pivot]({% link concepts/anchors-and-pivot.md %})
- [Styling]({% link concepts/styling.md %})
- [Visibility, overflow, pointer-events]({% link concepts/visibility-overflow.md %})

## Workflows

- [Test in Play workflow]({% link workflows/test-in-play.md %})
- [Compile + output management]({% link workflows/compile-and-output.md %})
- [User SCSS customization]({% link workflows/user-scss-customization.md %})
- [Undo/Redo + commands]({% link workflows/undo-redo-commands.md %})

## Architecture (for developers)

If you want to contribute or extend the editor:

- [Overview]({% link architecture/overview.md %})
- [Document model]({% link architecture/document-model.md %})
- [Canvas renderer]({% link architecture/canvas-renderer.md %})
- [Layout solver]({% link architecture/layout-solver.md %})
- [Generator (Razor + SCSS)]({% link architecture/generator.md %})
- [Compile writer]({% link architecture/compile-writer.md %})
- [Preview system]({% link architecture/preview-system.md %})

## Reference

- [.sui JSON schema]({% link reference/sui-json-schema.md %})
- [Allowed CSS properties]({% link reference/allowed-css.md %})
- [Element type matrix]({% link reference/element-types.md %})
- [Keyboard shortcuts]({% link reference/keyboard-shortcuts.md %})
- [Known issues]({% link reference/known-issues.md %})

## Tutorials

- [Build a survival HUD]({% link tutorials/survival-hud.md %})
- [Build an inventory screen]({% link tutorials/inventory-screen.md %})
- [Build a death modal]({% link tutorials/death-modal.md %})

## Support

- [Troubleshooting]({% link support/troubleshooting.md %})
- [FAQ]({% link support/faq.md %})
- [Changelog]({% link support/changelog.md %})

---

## At a glance

| What | Details |
|---|---|
| **Asset extension** | `.sui` (JSON) |
| **Generates** | `<name>.razor` + `<name>.razor.scss` (+ `<name>.User.scss` sidecar) |
| **Target** | s&box `PanelComponent` (Razor UI) |
| **Editor** | 100% custom paint chrome — no `DockManager`, no `Editor.TabWidget` |
| **Preview** | Embedded `SceneRenderingWidget` + on-demand "Test in Play" with TPS player |
| **License** | MIT |

## Versioning

This documentation tracks the **V1.0** release of SUI Designer.

For older or in-progress changes, see the [Changelog]({% link support/changelog.md %}).
