---
layout: default
title: Architecture
nav_order: 7
has_children: true
permalink: /architecture/
---

# Architecture

Developer-facing tour of how SUI Designer is wired internally. Read these if you're extending the addon, debugging generator output, or curious how the pieces fit.

<div class="section-grid" markdown="0">
  <a class="section-card" href="{% link architecture/overview.md %}">
    <span class="card-tag">ARCHITECTURE</span>
    <h3>Overview</h3>
    <p>The 30-second map of all subsystems — how SUI Designer is wired from <code>.sui</code> file to running ScreenPanel.</p>
  </a>
  <a class="section-card" href="{% link architecture/document-model.md %}">
    <span class="card-tag">ARCHITECTURE</span>
    <h3>Document model</h3>
    <p>The shape of a <code>.sui</code> document — what gets serialized, how the tree is stored, how elements are identified.</p>
  </a>
  <a class="section-card" href="{% link architecture/canvas-renderer.md %}">
    <span class="card-tag">ARCHITECTURE</span>
    <h3>Canvas renderer</h3>
    <p>How the editor canvas paints <code>.sui</code> documents using the Qt <code>Editor.Paint</code> API — independent of the s&amp;box runtime CSS engine.</p>
  </a>
  <a class="section-card" href="{% link architecture/layout-solver.md %}">
    <span class="card-tag">ARCHITECTURE</span>
    <h3>Layout solver</h3>
    <p>How SUI Designer computes where each element ends up on the canvas — and how it converts a target rect back into anchor/pivot values.</p>
  </a>
  <a class="section-card" href="{% link architecture/generator.md %}">
    <span class="card-tag">ARCHITECTURE</span>
    <h3>Generator pipeline</h3>
    <p>How a <code>.sui</code> document becomes a pair of in-memory <code>.razor</code> + <code>.razor.scss</code> strings — the pure-function half of compilation.</p>
  </a>
  <a class="section-card" href="{% link architecture/compile-writer.md %}">
    <span class="card-tag">ARCHITECTURE</span>
    <h3>Compile writer</h3>
    <p>How <code>SuiCompileWriter</code> takes the generator output and safely writes it to disk — without ever clobbering user-edited files.</p>
  </a>
  <a class="section-card" href="{% link architecture/preview-system.md %}">
    <span class="card-tag">ARCHITECTURE</span>
    <h3>Preview system</h3>
    <p>How "Test in Play" compiles a <code>.sui</code> into a real <code>Panel</code>, opens a stage scene, and mounts the UI on a player at runtime.</p>
  </a>
</div>
