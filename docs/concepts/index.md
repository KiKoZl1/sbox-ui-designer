---
layout: default
title: Concepts
nav_order: 5
has_children: true
permalink: /concepts/
---

# Concepts

Deeper explanations of the ideas behind the editor. Read these once and the rest of the docs (workflows, references) snap into place.

If you're new to flex/CSS layout, [Layout modes]({% link concepts/layout-modes.md %}) is the place to start. If you're already comfortable with the V1.0 designer and want to learn the V1.5 wiring story, start with [Variables]({% link concepts/variables.md %}) → [Bindings]({% link concepts/bindings.md %}) → [Wrapper generation]({% link concepts/wrapper-generation.md %}).

<div class="section-grid" markdown="0">
  <a class="section-card" href="{% link concepts/layout-modes.md %}">
    <span class="card-tag">LAYOUT</span>
    <h3>Layout modes</h3>
    <p>Every element has a <code>Layout.Mode</code> — <code>Absolute</code> or <code>Flex</code>. The most important decision when designing a UI.</p>
  </a>
  <a class="section-card" href="{% link concepts/anchors-and-pivot.md %}">
    <span class="card-tag">LAYOUT</span>
    <h3>Anchors and Pivot</h3>
    <p>How elements snap to their parents — UMG-style anchoring, applied to s&amp;box.</p>
  </a>
  <a class="section-card" href="{% link concepts/styling.md %}">
    <span class="card-tag">LAYOUT</span>
    <h3>Styling</h3>
    <p>Colors, borders, opacity — the Appearance section of the Details panel.</p>
  </a>
  <a class="section-card" href="{% link concepts/visibility-overflow.md %}">
    <span class="card-tag">LAYOUT</span>
    <h3>Visibility, Overflow, Pointer Events</h3>
    <p>Three independent attributes that control how an element renders and receives input.</p>
  </a>
  <a class="section-card" href="{% link concepts/wrapper-generation.md %}">
    <span class="card-tag">V1.5 DATA</span>
    <h3>Wrapper generation</h3>
    <p>The <code>&lt;Name&gt;.cs</code> file and the <code>SuiPanel&lt;TView&gt;</code> pattern — what every <code>.sui</code> emits when compiled.</p>
  </a>
  <a class="section-card" href="{% link concepts/variables.md %}">
    <span class="card-tag">V1.5 DATA</span>
    <h3>Variables</h3>
    <p>Typed, named UI-local state declared on a <code>.sui</code> document — the bridge between gameplay code and your UI.</p>
  </a>
  <a class="section-card" href="{% link concepts/bindings.md %}">
    <span class="card-tag">V1.5 DATA</span>
    <h3>Bindings</h3>
    <p>Connect one element property to one Variable, optionally through a chain of converters.</p>
  </a>
  <a class="section-card" href="{% link concepts/converters.md %}">
    <span class="card-tag">V1.5 DATA</span>
    <h3>Converters</h3>
    <p>Pure functions that transform a value as it flows from a Variable to an element property. 66 builtins + your own with <code>[SuiConverter]</code>.</p>
  </a>
  <a class="section-card" href="{% link concepts/composition.md %}">
    <span class="card-tag">V1.5 DATA</span>
    <h3>Composition / Sub-UIs</h3>
    <p>Embed one <code>.sui</code> inside another via <code>SuiReference</code>. ForEach iterates a List Variable into a child template.</p>
  </a>
  <a class="section-card" href="{% link concepts/events-and-actions.md %}">
    <span class="card-tag">V1.5 DATA</span>
    <h3>Events &amp; Actions</h3>
    <p>Wire UI interactions to gameplay. Two modes: <strong>Code</strong> (C# handler) or <strong>Doo</strong> (visual graph inside the <code>.sui</code>).</p>
  </a>
  <a class="section-card" href="{% link concepts/interactive-states.md %}">
    <span class="card-tag">V1.5 POLISH</span>
    <h3>Interactive states</h3>
    <p>Hover / Pressed / Disabled / Focused state overrides for Button, InventorySlot, and ItemIcon — with transitions, sounds, cursors.</p>
  </a>
  <a class="section-card" href="{% link concepts/input-and-update-triggers.md %}">
    <span class="card-tag">V1.5 POLISH</span>
    <h3>Input &amp; Update triggers</h3>
    <p>When a TwoWay binding commits the UI value back to its source — <code>OnChange / OnLostFocus / OnSubmit / OnRelease / Manual</code>.</p>
  </a>
  <a class="section-card" href="{% link concepts/sandbox-ui-css-limitations.md %}">
    <span class="card-tag">UNDER THE HOOD</span>
    <h3>Sandbox.UI CSS limitations</h3>
    <p>The parser gotchas — silent-drop selectors, ignored <code>!important</code>, and other styles the engine quietly throws away.</p>
  </a>
  <a class="section-card" href="{% link concepts/reactivity-and-buildhash.md %}">
    <span class="card-tag">UNDER THE HOOD</span>
    <h3>Reactivity &amp; BuildHash</h3>
    <p>How a SUI document decides when to re-render — and why mutating <code>Hud.Health = 50</code> repaints the bar without callbacks.</p>
  </a>
</div>
