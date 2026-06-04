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

A visual UI editor for [s&box](https://sbox.game/) — design `.sui` documents in a paint-based canvas, generate idiomatic Razor + SCSS, drive everything from typed Variables + bindings, preview live in Play.
{: .fs-6 .fw-300 }

[Get started](#getting-started){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View on GitHub](https://github.com/KiKoZl1/sbox-ui-designer){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## What it does

SUI Designer replaces the loop of *hand-writing Razor → editing SCSS → hot-reloading → guessing the layout* with a WYSIWYG workflow that produces idiomatic, hand-editable s&box code:

1. Drop elements from the Palette (`Panel`, `Image`, `Text`, `Button`, `ProgressBar`, `Hotbar`, `InventoryGrid`, **`TextEntry`**, **`Slider`**, **`Toggle`**, **`DropDown`**, **`SuiReference`** for sub-UIs, ...).
2. Edit position, size, anchor, style, layout in a Details panel.
3. Declare **typed Variables** on the document — `Health: int`, `PlayerName: string`, `Tint: Color`, ... — then bind element properties to them through optional converter chains.
4. Wire **events** (`OnClick`, `OnValueChanged`, ...) to either a C# handler or a visual [Doo](https://docs.facepunch.com/s/sbox-dev/doc/doo) graph stored inside the document.
5. See it live — paint-based canvas at design time, **Test in Play** to mount the UI on a real player in a real scene.
6. Compile to your project's `Code/` folder. Every `.sui` generates three files: `<Name>Panel.razor` + `.razor.scss` + `<Name>.cs` (a wrapper extending `SuiPanel<TView>` with `Add` / `Show` / `Hide` / `Remove` and per-Variable `[Property]` mirrors).

The `.sui` document is JSON-backed, schema-versioned, and version-control-friendly. The generator emits readable, hand-editable Razor + SCSS that compiles in any s&box game.

## What's new in V1.5

V1.5 turns the static layout-builder into a full data + scripting workflow:

- **Typed Variables + Bindings** — declare `Health: float`, `PlayerName: string`, bind `ProgressBar.Value` to `Health` through a 66-entry built-in converter library (or your own `[SuiConverter]`).
- **Sub-UI composition (`SuiReference`)** — embed one `.sui` inside another with proportional canvas rescaling; ForEach iterates a `List<T>` Variable into a child template.
- **Events with two modes** — wire `OnClick` to a C# handler in a `<Name>.partial.cs` sidecar, or to a [Doo](https://docs.facepunch.com/s/sbox-dev/doc/doo) graph serialized inside the document (UMG-style: graph travels with the widget).
- **Input widgets** — TextEntry / Slider / Toggle / DropDown with explicit `UpdateTrigger` (OnChange / OnLostFocus / OnSubmit / OnRelease / Manual) and a `wrapper.Apply.<Field>()` namespace for Manual commits.
- **Interactive states** — Hover / Pressed / Disabled / Focused style overrides with `transition` + hover/press sound assets.

Upgrading from V1.0? Read the [V1.0 → V1.5 upgrade guide]({% link UPGRADE_V1_0_TO_V1_5.md %}).

## Getting started

Never opened SUI Designer before? Start here:

- [Installation]({% link getting-started/install.md %}) — bring the addon into your s&box project (2 min)
- [Your first UI]({% link getting-started/your-first-ui.md %}) — build a HUD from scratch and use the wrapper (10 min)
- [Test in Play]({% link getting-started/test-in-play.md %}) — see your UI on a real player (3 min)

Upgrading a V1.0 project? See the [V1.0 → V1.5 upgrade guide]({% link UPGRADE_V1_0_TO_V1_5.md %}).

## User guide

End-to-end reference for the editor and every panel.

- [Editor tour]({% link user-guide/editor-tour.md %})
- [Palette]({% link user-guide/palette.md %})
- [Hierarchy]({% link user-guide/hierarchy.md %})
- [Details panel]({% link user-guide/details-panel.md %})
- [Canvas]({% link user-guide/canvas.md %})
- [Top toolbar]({% link user-guide/top-toolbar.md %})
- [Alignment tools]({% link user-guide/alignment-tools.md %})
- [Compile Results]({% link user-guide/compile-results.md %})

## Concepts

The mental model behind the editor — read these once and the rest snaps into place.

- [Layout modes (Absolute vs Flex)]({% link concepts/layout-modes.md %})
- [Anchors and pivot]({% link concepts/anchors-and-pivot.md %})
- [Styling]({% link concepts/styling.md %})
- [Visibility, overflow, pointer-events]({% link concepts/visibility-overflow.md %})
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — `<Name>.cs` and the `SuiPanel<TView>` pattern
- **[Variables]({% link concepts/variables.md %})** — typed UI-local state
- **[Bindings]({% link concepts/bindings.md %})** — connect Variables to element properties
- **[Converters]({% link concepts/converters.md %})** — transform values in the binding chain
- **[Composition / Sub-UIs]({% link concepts/composition.md %})** — embed one `.sui` inside another via `SuiReference`
- **[Events & Actions]({% link concepts/events-and-actions.md %})** — Code vs Doo modes, `@ref` exposure
- **[Interactive states]({% link concepts/interactive-states.md %})** — Hover / Pressed / Disabled / Focused with transitions + sounds
- **[Input & Update triggers]({% link concepts/input-and-update-triggers.md %})** — when TwoWay bindings actually commit

## Element reference

One page per palette type.

### Layout root
- [Canvas]({% link elements/canvas.md %})

### Containers
- [Panel]({% link elements/panel.md %})
- [HorizontalBox]({% link elements/horizontal-box.md %})
- [VerticalBox]({% link elements/vertical-box.md %})
- [Overlay]({% link elements/overlay.md %})
- [Grid]({% link elements/grid.md %})
- [ScrollPanel]({% link elements/scroll-panel.md %})

### Visuals
- [Image]({% link elements/image.md %})
- [Text]({% link elements/text.md %})
- [Button]({% link elements/button.md %}) — with M3.5 interactive states
- [ProgressBar]({% link elements/progress-bar.md %})

### Input widgets (new in V1.5 M4)
- [TextEntry]({% link elements/text-entry.md %})
- [Slider]({% link elements/slider.md %})
- [Toggle]({% link elements/toggle.md %})
- [DropDown]({% link elements/dropdown.md %})

### Composition
- [SuiReference]({% link elements/sui-reference.md %}) — embed another `.sui`

### Inventory primitives
- [InventoryGrid]({% link elements/inventory-grid.md %})
- [InventorySlot]({% link elements/inventory-slot.md %})
- [ItemIcon]({% link elements/item-icon.md %})
- [Hotbar]({% link elements/hotbar.md %})

## Workflows

How to actually get things done in the editor.

- [Test in Play workflow]({% link workflows/test-in-play.md %})
- [Compile + output management]({% link workflows/compile-and-output.md %})
- [User SCSS customization]({% link workflows/user-scss-customization.md %})
- [Undo/Redo + commands]({% link workflows/undo-redo-commands.md %})
- **[Binding a Variable]({% link workflows/binding-a-variable.md %})** — the bind popup, step by step
- **[Working with converters]({% link workflows/working-with-converters.md %})** — Compose, Format, custom
- **[Manual commit with `Apply`]({% link workflows/manual-commit-with-apply.md %})** — UpdateTrigger.Manual flow
- **[Embedding sub-UIs]({% link workflows/embedding-sub-uis.md %})** — `SuiReference` + ForEach
- [Events & Element refs]({% link workflows/events-and-refs.md %}) — wire `OnClick`, expose `@ref`
- **[Upgrading from V1.0]({% link workflows/upgrading-from-v1-0.md %})**

## Architecture (for contributors)

If you want to extend the editor:

- [Overview]({% link architecture/overview.md %})
- [Document model]({% link architecture/document-model.md %})
- [Canvas renderer]({% link architecture/canvas-renderer.md %})
- [Layout solver]({% link architecture/layout-solver.md %})
- [Generator (Razor + SCSS + wrapper)]({% link architecture/generator.md %})
- [Compile writer]({% link architecture/compile-writer.md %})
- [Preview system]({% link architecture/preview-system.md %})

## Reference

- [.sui JSON schema]({% link reference/sui-json-schema.md %}) — current schema (V3)
- [Allowed CSS properties]({% link reference/allowed-css.md %})
- [Element type matrix]({% link reference/element-types.md %})
- [Keyboard shortcuts]({% link reference/keyboard-shortcuts.md %})
- [Known issues]({% link reference/known-issues.md %})
- **[Converters catalog]({% link reference/converters-catalog.md %})** — all 66 builtins
- **[Wrapper API]({% link reference/wrapper-api.md %})** — `SuiPanel<TView>` surface
- **[Update-trigger matrix]({% link reference/update-triggers.md %})** — which triggers each widget allows
- **[Binding-mode matrix]({% link reference/binding-mode-matrix.md %})** — OneTime / OneWay / TwoWay per property

## Tutorials

Worked, end-to-end examples.

- [Build a survival HUD]({% link tutorials/survival-hud.md %})
- [Build an inventory screen]({% link tutorials/inventory-screen.md %})
- [Build a death modal]({% link tutorials/death-modal.md %})
- **[Settings screen with input widgets]({% link tutorials/settings-screen.md %})** — TextEntry + Slider + Toggle + DropDown + Apply API
- **[Health HUD with converters]({% link tutorials/health-hud-with-converters.md %})** — bind `Health` → ProgressBar + `"75/100 HP"` label

## Support

- [Troubleshooting]({% link support/troubleshooting.md %})
- [FAQ]({% link support/faq.md %})
- [Changelog]({% link support/changelog.md %})

---

## At a glance

| What | Details |
|---|---|
| **Asset extension** | `.sui` (JSON, schema V3) |
| **Generates per `.sui`** | `<Name>Panel.razor` + `<Name>.razor.scss` + `<Name>.cs` wrapper (+ `<Name>.User.scss` sidecar) |
| **Wrapper base class** | `SuiPanel<TView>` (`Code/Runtime/SuiPanel.cs`) |
| **Element types** | 21 (15 V1.0 + SuiReference + 4 input widgets + Tooltip reserved) |
| **Builtin converters** | 66 across Math / Range / Conversion / Logic / String / Color / Collection |
| **Editor** | 100% custom paint chrome — no `DockManager`, no `Editor.TabWidget` |
| **Preview** | Embedded `SceneRenderingWidget` + on-demand **Test in Play** with TPS player |
| **License** | MIT |

## Versioning

This documentation tracks the **V1.5** release of SUI Designer. The internal
deviations log (kept in the source repo, not published to the docs site) is the
authoritative changelog between the original locked PRDs and what actually
shipped.

For older history, see the [Changelog]({% link support/changelog.md %}).
