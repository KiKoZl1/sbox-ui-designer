---
layout: default
title: Getting Started
nav_order: 2
has_children: true
permalink: /getting-started/
---

# Getting Started

New to SUI Designer? Start here. Each guide is independent — feel free to skip ahead if you're comfortable with a step.

<div class="section-grid" markdown="0">
  <a class="section-card" href="{% link getting-started/install.md %}">
    <span class="card-tag">GETTING STARTED</span>
    <h3>Installation</h3>
    <p>Install SUI Designer into an s&amp;box project as a library (addon). ~2 minutes.</p>
  </a>
  <a class="section-card" href="{% link getting-started/your-first-ui.md %}">
    <span class="card-tag">GETTING STARTED</span>
    <h3>Your first UI</h3>
    <p>Build a centered HUD with a health bar and a title text — from a fresh <code>.sui</code> to a compiled <code>PanelComponent</code>. ~10 minutes.</p>
  </a>
  <a class="section-card" href="{% link getting-started/test-in-play.md %}">
    <span class="card-tag">GETTING STARTED</span>
    <h3>Test in Play</h3>
    <p>One-click workflow that loads a stage scene with a TPS player and mounts your UI as a <code>ScreenPanel</code> overlay. ~3 minutes.</p>
  </a>
</div>

## After you've done the basics

The three guides above teach you to draw and compile a static UI. V1.5 adds the data layer — read these once you've shipped your first HUD:

- [Variables]({% link concepts/variables.md %}) — declare typed `Health`, `PlayerName`, `Tint` slots on the document
- [Bindings]({% link concepts/bindings.md %}) — drive `ProgressBar.Value` from a `Health` Variable
- [Converters]({% link concepts/converters.md %}) — transform values between Variable and target type (e.g. `float → string`)
- [Events & Actions]({% link concepts/events-and-actions.md %}) — wire `OnClick` to a C# handler or a visual Doo graph
- [Input widgets]({% link elements/text-entry.md %}) — TextEntry / [Slider]({% link elements/slider.md %}) / [Toggle]({% link elements/toggle.md %}) / [DropDown]({% link elements/dropdown.md %}) for settings screens, with the Apply API for Manual commits
- [Interactive states]({% link concepts/interactive-states.md %}) — Hover / Pressed / Disabled visuals with transitions and sounds
