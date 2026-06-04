---
layout: default
title: Workflows
nav_order: 6
has_children: true
permalink: /workflows/
---

# Workflows

End-to-end task guides — multi-step operations that aren't tied to a single panel.

<div class="section-grid" markdown="0">
  <a class="section-card" href="{% link workflows/binding-a-variable.md %}">
    <span class="card-tag">AUTHORING</span>
    <h3>Binding a Variable</h3>
    <p>The Bind popup, step by step. Pick the target property, source Variable, mode, update trigger, and optional converter chain.</p>
  </a>
  <a class="section-card" href="{% link workflows/working-with-converters.md %}">
    <span class="card-tag">AUTHORING</span>
    <h3>Working with converters</h3>
    <p>How to pick the right converter, build a chain, and write your own. Covers Compose vs Format vs custom <code>[SuiConverter]</code>.</p>
  </a>
  <a class="section-card" href="{% link workflows/manual-commit-with-apply.md %}">
    <span class="card-tag">AUTHORING</span>
    <h3>Manual commit with Apply</h3>
    <p><code>UpdateTrigger.Manual</code> + <code>wrapper.Apply.&lt;ElementName&gt;Value()</code> — the explicit-save pattern for forms and Apply/Cancel dialogs.</p>
  </a>
  <a class="section-card" href="{% link workflows/embedding-sub-uis.md %}">
    <span class="card-tag">AUTHORING</span>
    <h3>Embedding sub-UIs</h3>
    <p>Drop a <code>.sui</code> inside another via <code>SuiReference</code>. Pass per-instance Props. Use ForEach for dynamic lists.</p>
  </a>
  <a class="section-card" href="{% link workflows/events-and-refs.md %}">
    <span class="card-tag">AUTHORING</span>
    <h3>Events &amp; Element refs</h3>
    <p>Wire <code>OnClick</code> from the SUI Designer. Buttons that fire callbacks, sliders that emit values, panels exposed as typed <code>@ref</code>.</p>
  </a>
  <a class="section-card" href="{% link workflows/writing-a-custom-converter.md %}">
    <span class="card-tag">AUTHORING</span>
    <h3>Writing a custom converter</h3>
    <p>Author your own <code>[SuiConverter]</code> method and have it show up in the Bind popup. The scaffolder takes care of the file + markers.</p>
  </a>
  <a class="section-card" href="{% link workflows/test-in-play.md %}">
    <span class="card-tag">BUILD &amp; SHIP</span>
    <h3>Test in Play</h3>
    <p>A deeper dive on Test in Play — full sequence from button click to mounted ScreenPanel on a real TPS player.</p>
  </a>
  <a class="section-card" href="{% link workflows/compile-and-output.md %}">
    <span class="card-tag">BUILD &amp; SHIP</span>
    <h3>Compile + output management</h3>
    <p>Where <code>Ctrl+B</code> writes files, how ownership works, and how to recover if something goes wrong.</p>
  </a>
  <a class="section-card" href="{% link workflows/user-scss-customization.md %}">
    <span class="card-tag">BUILD &amp; SHIP</span>
    <h3>User SCSS customization</h3>
    <p>How to add custom styles that survive recompile — using the <code>.User.scss</code> sidecar.</p>
  </a>
  <a class="section-card" href="{% link workflows/undo-redo-commands.md %}">
    <span class="card-tag">BUILD &amp; SHIP</span>
    <h3>Undo / Redo and commands</h3>
    <p>How every edit becomes a reversible command, and how the undo stack tracks them.</p>
  </a>
  <a class="section-card" href="{% link workflows/upgrading-from-v1-0.md %}">
    <span class="card-tag">MIGRATION</span>
    <h3>Upgrading from V1.0</h3>
    <p>V1.5 is fully backward-compatible. Drop in the V1.5 build, open any <code>.sui</code>, click <strong>Migrate all</strong>. Pointer to the full guide.</p>
  </a>
</div>
