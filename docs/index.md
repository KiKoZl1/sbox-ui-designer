---
layout: default
title: Home
nav_order: 1
description: "SUI Designer — visual UI editor for s&box"
permalink: /
---

<style>
.hero {
  background: linear-gradient(135deg, #0f0f11 0%, #161618 100%);
  border: 1px solid #2a2a2e;
  border-radius: 12px;
  padding: 3rem 2rem;
  margin: 1rem 0 2rem 0;
  text-align: center;
}
.hero h1 {
  font-size: 3rem;
  margin: 0 0 1rem 0;
  color: #ffffff;
  border-bottom: none;
}
.hero-tagline {
  font-size: 1.15rem;
  color: #9ca3af;
  max-width: 720px;
  margin: 0 auto 1.75rem auto;
  line-height: 1.55;
}
.hero-cta {
  display: flex;
  gap: 0.75rem;
  justify-content: center;
  flex-wrap: wrap;
}
.hero-cta .btn {
  font-size: 1rem;
  padding: 0.6rem 1.25rem;
}

.feature-grid,
.section-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  margin: 1.5rem 0;
}
.quickstart-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  margin: 1.5rem 0;
}

.feature-card,
.section-card,
.quickstart-card {
  display: block;
  background: #161618;
  border: 1px solid #2a2a2e;
  border-radius: 8px;
  padding: 1.25rem 1.5rem;
  transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
  text-decoration: none;
  color: inherit;
}
.feature-card:hover,
.section-card:hover,
.quickstart-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(74, 222, 128, 0.15);
  border-color: #4ade80;
  text-decoration: none;
}

.card-label {
  display: block;
  text-transform: uppercase;
  font-size: 0.7rem;
  color: #4ade80;
  letter-spacing: 0.05em;
  font-weight: 600;
  margin-bottom: 0.4rem;
}
.card-title {
  font-size: 1.1rem;
  font-weight: 600;
  color: #ffffff;
  margin: 0 0 0.4rem 0;
}
.card-body {
  font-size: 0.92rem;
  color: #9ca3af;
  line-height: 1.5;
  margin: 0;
}

.quickstart-card .card-number {
  font-size: 2rem;
  font-weight: 700;
  color: #4ade80;
  line-height: 1;
  margin-bottom: 0.5rem;
}

.docs-footer {
  margin-top: 3rem;
  padding-top: 1.5rem;
  border-top: 1px solid #2a2a2e;
  font-size: 0.9rem;
  color: #6b7280;
  text-align: center;
}

@media (max-width: 768px) {
  .feature-grid,
  .section-grid,
  .quickstart-grid {
    grid-template-columns: 1fr;
  }
  .hero { padding: 2rem 1.25rem; }
  .hero h1 { font-size: 2.25rem; }
}
</style>

<div class="hero">
  <h1>SUI Designer</h1>
  <p class="hero-tagline">Author functional s&box UIs visually. Variables, bindings, events — wired in the editor, generated as Razor + C# wrappers, hot-reloaded in play.</p>
  <div class="hero-cta">
    <a class="btn btn-primary" href="{% link getting-started/index.md %}">Get started →</a>
    <a class="btn" href="https://github.com/KiKoZl1/sbox-ui-designer">View on GitHub</a>
  </div>
</div>

## What V1.5 brings

<div class="feature-grid">

  <a class="feature-card" href="{% link concepts/variables.md %}">
    <span class="card-label">Reactive state</span>
    <h3 class="card-title">Typed Variables</h3>
    <p class="card-body">Strongly-typed reactive state authored in the Designer — declare <code>Health: int</code>, <code>PlayerName: string</code>, <code>Tint: Color</code> on the document.</p>
  </a>

  <a class="feature-card" href="{% link concepts/bindings.md %}">
    <span class="card-label">Data flow</span>
    <h3 class="card-title">Two-way Bindings</h3>
    <p class="card-body">Read AND write back to Variables with five update triggers — OnChange, OnLostFocus, OnSubmit, OnRelease, Manual.</p>
  </a>

  <a class="feature-card" href="{% link concepts/converters.md %}">
    <span class="card-label">Transform pipeline</span>
    <h3 class="card-title">66 Built-in Converters</h3>
    <p class="card-body">Math, Logic, String, Color, Date — composable chains between Variables and element properties, plus your own <code>[SuiConverter]</code>.</p>
  </a>

  <a class="feature-card" href="{% link concepts/events-and-actions.md %}">
    <span class="card-label">Scripting</span>
    <h3 class="card-title">Events: Code + Doo</h3>
    <p class="card-body">Wire <code>OnClick</code> / <code>OnHover</code> to a C# handler in a <code>.partial.cs</code> sidecar, or to a Doo graph stored inside the document.</p>
  </a>

  <a class="feature-card" href="{% link concepts/input-and-update-triggers.md %}">
    <span class="card-label">Forms</span>
    <h3 class="card-title">Input Widgets</h3>
    <p class="card-body">TextEntry, Slider, Toggle, DropDown — with explicit update triggers and a <code>wrapper.Apply.&lt;Field&gt;()</code> namespace for manual commits.</p>
  </a>

  <a class="feature-card" href="{% link concepts/interactive-states.md %}">
    <span class="card-label">Styling</span>
    <h3 class="card-title">Interactive States</h3>
    <p class="card-body">Hover / Pressed / Disabled / Focused style overrides per widget, with <code>transition</code> easing and hover/press sound assets.</p>
  </a>

</div>

## Quick start

<div class="quickstart-grid">

  <a class="quickstart-card" href="{% link getting-started/install.md %}">
    <div class="card-number">1</div>
    <h3 class="card-title">Install</h3>
    <p class="card-body">Bring the SUI Designer addon into your s&box project — two minutes from clone to first launch.</p>
  </a>

  <a class="quickstart-card" href="{% link getting-started/your-first-ui.md %}">
    <div class="card-number">2</div>
    <h3 class="card-title">Your first UI</h3>
    <p class="card-body">Build a HUD from scratch, declare a Variable, bind a ProgressBar, and use the generated wrapper from gameplay code.</p>
  </a>

  <a class="quickstart-card" href="{% link tutorials/index.md %}">
    <div class="card-number">3</div>
    <h3 class="card-title">Run a tutorial</h3>
    <p class="card-body">Follow an end-to-end worked example — survival HUD, inventory screen, settings panel, or death modal.</p>
  </a>

</div>

## Browse by section

<div class="section-grid">

  <a class="section-card" href="{% link getting-started/index.md %}">
    <span class="card-label">Start here</span>
    <h3 class="card-title">Getting started</h3>
    <p class="card-body">Install, first UI, and Test in Play — the on-ramp for new users.</p>
  </a>

  <a class="section-card" href="{% link user-guide/editor-tour.md %}">
    <span class="card-label">Editor reference</span>
    <h3 class="card-title">User guide</h3>
    <p class="card-body">End-to-end tour of every panel: Palette, Hierarchy, Details, Canvas, Toolbar, Compile Results.</p>
  </a>

  <a class="section-card" href="{% link concepts/variables.md %}">
    <span class="card-label">Mental model</span>
    <h3 class="card-title">Concepts</h3>
    <p class="card-body">Variables, Bindings, Converters, Composition, Events, Interactive states, Update triggers.</p>
  </a>

  <a class="section-card" href="{% link elements/canvas.md %}">
    <span class="card-label">Per-widget docs</span>
    <h3 class="card-title">Elements</h3>
    <p class="card-body">21 element types — containers, visuals, input widgets, inventory primitives, and SuiReference.</p>
  </a>

  <a class="section-card" href="{% link workflows/test-in-play.md %}">
    <span class="card-label">How-to</span>
    <h3 class="card-title">Workflows</h3>
    <p class="card-body">Test in Play, compile output, user SCSS, bind a Variable, manual commit, embed sub-UIs.</p>
  </a>

  <a class="section-card" href="{% link architecture/overview.md %}">
    <span class="card-label">For contributors</span>
    <h3 class="card-title">Architecture</h3>
    <p class="card-body">Document model, canvas renderer, layout solver, generator, compile writer, preview system.</p>
  </a>

  <a class="section-card" href="{% link reference/sui-json-schema.md %}">
    <span class="card-label">Lookup tables</span>
    <h3 class="card-title">Reference</h3>
    <p class="card-body">JSON schema, allowed CSS, converters catalog, wrapper API, update-trigger and binding-mode matrices.</p>
  </a>

  <a class="section-card" href="{% link tutorials/survival-hud.md %}">
    <span class="card-label">Worked examples</span>
    <h3 class="card-title">Tutorials</h3>
    <p class="card-body">Survival HUD, inventory screen, death modal, settings screen, health HUD with converters.</p>
  </a>

  <a class="section-card" href="{% link support/troubleshooting.md %}">
    <span class="card-label">Help</span>
    <h3 class="card-title">Support</h3>
    <p class="card-body">Troubleshooting, FAQ, and the changelog — answers when something does not behave as expected.</p>
  </a>

</div>

<p class="docs-footer">Built with Jekyll + just-the-docs theme. Source on <a href="https://github.com/KiKoZl1/sbox-ui-designer">GitHub</a>.</p>
