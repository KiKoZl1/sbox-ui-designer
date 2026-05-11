---
layout: default
title: Architecture
nav_order: 7
has_children: true
permalink: /architecture/
---

# Architecture

Developer-facing tour of how SUI Designer is wired internally. Read these if you're extending the addon, debugging generator output, or curious how the pieces fit.

- [Overview]({% link architecture/overview.md %}) — the 30-second map of all subsystems
- [Document model]({% link architecture/document-model.md %}) — `SuiDocument` / `SuiElement` / how `.sui` files are persisted
- [Canvas renderer]({% link architecture/canvas-renderer.md %}) — paint pipeline, hit-testing, gizmos
- [Layout solver]({% link architecture/layout-solver.md %}) — `SuiLayoutSolver` + `SuiFlexLayout` forward and inverse math
- [Generator pipeline]({% link architecture/generator.md %}) — `.sui` → in-memory Razor + SCSS
- [Compile writer]({% link architecture/compile-writer.md %}) — file ownership, manifest, backups
- [Preview system]({% link architecture/preview-system.md %}) — Test in Play, scene swap, runtime mount
