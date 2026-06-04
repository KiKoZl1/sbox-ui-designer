---
layout: default
title: Getting Started
nav_order: 2
has_children: true
permalink: /getting-started/
---

# Getting Started

New to SUI Designer? Start here.

1. [Installation]({% link getting-started/install.md %}) — bring the library into your s&box project.
2. [Your first UI]({% link getting-started/your-first-ui.md %}) — design a simple HUD step by step.
3. [Test in Play]({% link getting-started/test-in-play.md %}) — preview your UI on a real player in real Play mode.

Each guide is independent — feel free to skip ahead if you're comfortable with a step.

## After you've done the basics

The three guides above teach you to draw and compile a static UI. V1.5 adds the data layer — read these once you've shipped your first HUD:

- [Variables]({% link concepts/variables.md %}) — declare typed `Health`, `PlayerName`, `Tint` slots on the document
- [Bindings]({% link concepts/bindings.md %}) — drive `ProgressBar.Value` from a `Health` Variable
- [Converters]({% link concepts/converters.md %}) — transform values between Variable and target type (e.g. `float → string`)
- [Events & Actions]({% link concepts/events-and-actions.md %}) — wire `OnClick` to a C# handler or a visual Doo graph
- [Input widgets]({% link elements/text-entry.md %}) — TextEntry / [Slider]({% link elements/slider.md %}) / [Toggle]({% link elements/toggle.md %}) / [DropDown]({% link elements/dropdown.md %}) for settings screens, with the Apply API for Manual commits
- [Interactive states]({% link concepts/interactive-states.md %}) — Hover / Pressed / Disabled visuals with transitions and sounds
