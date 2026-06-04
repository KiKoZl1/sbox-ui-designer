---
layout: default
title: Reference
nav_order: 8
has_children: true
permalink: /reference/
---

# Reference

Look-up material — schemas, lists, matrices, generated-API surface.

<div class="section-grid" markdown="0">
  <a class="section-card" href="{% link reference/sui-json-schema.md %}">
    <span class="card-tag">SCHEMA</span>
    <h3>SUI JSON schema</h3>
    <p>The on-disk format of a <code>.sui</code> file — what each field means, what's optional, and what values are valid (V3).</p>
  </a>
  <a class="section-card" href="{% link reference/element-types.md %}">
    <span class="card-tag">SCHEMA</span>
    <h3>Element types</h3>
    <p>Every element type, what it does, and what fields it cares about. Full type list with field matrix.</p>
  </a>
  <a class="section-card" href="{% link reference/allowed-css.md %}">
    <span class="card-tag">SCHEMA</span>
    <h3>Allowed CSS</h3>
    <p>The exact list of CSS properties the SUI generator will emit. Anything outside this list is a hard error.</p>
  </a>
  <a class="section-card" href="{% link reference/converters-catalog.md %}">
    <span class="card-tag">V1.5 CATALOG</span>
    <h3>Converters catalog</h3>
    <p>Every builtin converter (66 total) with signature, category, description, defaults. Source: <code>SuiBuiltinConverters.cs</code>.</p>
  </a>
  <a class="section-card" href="{% link reference/wrapper-api.md %}">
    <span class="card-tag">V1.5 CATALOG</span>
    <h3>Wrapper API</h3>
    <p>The <code>SuiPanel&lt;TView&gt;</code> base class — what every generated wrapper provides for free. Plus <code>Apply</code> + <code>ContentHash</code>.</p>
  </a>
  <a class="section-card" href="{% link reference/update-triggers.md %}">
    <span class="card-tag">V1.5 CATALOG</span>
    <h3>Update-trigger matrix</h3>
    <p>Per-widget × trigger table — which <code>UpdateTrigger</code> values each TwoWay binding can use.</p>
  </a>
  <a class="section-card" href="{% link reference/binding-mode-matrix.md %}">
    <span class="card-tag">V1.5 CATALOG</span>
    <h3>Binding-mode matrix</h3>
    <p>(element type, property) × mode table — what modes the validator + Bind popup allow.</p>
  </a>
  <a class="section-card" href="{% link reference/tools-menu.md %}">
    <span class="card-tag">EDITOR</span>
    <h3>Tools menu</h3>
    <p>Every item under the Designer's <strong>Tools</strong> menu — Validate / Regen / Force Regen / Asset Registry / sample installer.</p>
  </a>
  <a class="section-card" href="{% link reference/sample-index.md %}">
    <span class="card-tag">EDITOR</span>
    <h3>Sample index</h3>
    <p>Every <code>.sui</code> shipped with <code>Assets/SuiSamples/</code>, indexed by feature. Pick the closest one, open it, learn by remix.</p>
  </a>
  <a class="section-card" href="{% link reference/keyboard-shortcuts.md %}">
    <span class="card-tag">EDITOR</span>
    <h3>Keyboard shortcuts</h3>
    <p>Every hotkey wired into the SUI Designer window. Source: <code>[Shortcut(...)]</code> attributes in <code>SuiDesignerWindow.cs</code>.</p>
  </a>
  <a class="section-card" href="{% link reference/known-issues.md %}">
    <span class="card-tag">MISC</span>
    <h3>Known issues</h3>
    <p>Open bugs and limitations with workarounds where available. Authoritative list lives in <code>ISSUES.md</code> in the source repo.</p>
  </a>
</div>
