---
layout: default
title: Concepts
nav_order: 5
has_children: true
permalink: /concepts/
---

# Concepts

Deeper explanations of the ideas behind the editor. Read these once and the rest of the docs (workflows, references) snap into place.

## Layout & visuals

- [Layout modes (Absolute vs Flex)]({% link concepts/layout-modes.md %}) — the most important distinction
- [Anchors and Pivot]({% link concepts/anchors-and-pivot.md %}) — how an element snaps to its parent
- [Styling]({% link concepts/styling.md %}) — Background, Border, Opacity, color formats
- [Visibility, Overflow, Pointer Events]({% link concepts/visibility-overflow.md %}) — the rendering & input attributes

## V1.5 — data & lifecycle

- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — the `<Name>.cs` and the `SuiPanel<TView>` pattern
- [Variables]({% link concepts/variables.md %}) — typed UI-local state
- [Bindings]({% link concepts/bindings.md %}) — connect Variables to element properties
- [Converters]({% link concepts/converters.md %}) — transform values in the binding chain
- [Composition / Sub-UIs]({% link concepts/composition.md %}) — embed one `.sui` inside another via `SuiReference`
- [Events & Actions]({% link concepts/events-and-actions.md %}) — Code vs Doo modes, `@ref` exposure

## V1.5 — polish

- [Interactive states]({% link concepts/interactive-states.md %}) — Hover / Pressed / Disabled / Focused with transitions + sounds
- [Input & Update triggers]({% link concepts/input-and-update-triggers.md %}) — when TwoWay bindings commit (`OnChange / OnLostFocus / OnSubmit / OnRelease / Manual`)

## Under the hood

- [Sandbox.UI CSS limitations]({% link concepts/sandbox-ui-css-limitations.md %}) — the parser gotchas (silent-drop selectors, `!important` ignored, etc.)
- [Reactivity & BuildHash]({% link concepts/reactivity-and-buildhash.md %}) — when does a SUI panel re-render, and why mutating `Hud.Health = 50` just works

If you're new to flex/CSS layout, the [Layout modes]({% link concepts/layout-modes.md %}) page is the place to start. If you're already comfortable with the V1.0 designer and want to learn the V1.5 wiring story, start with [Variables]({% link concepts/variables.md %}) → [Bindings]({% link concepts/bindings.md %}) → [Wrapper generation]({% link concepts/wrapper-generation.md %}).
