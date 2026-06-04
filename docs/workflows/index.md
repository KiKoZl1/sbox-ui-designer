---
layout: default
title: Workflows
nav_order: 6
has_children: true
permalink: /workflows/
---

# Workflows

End-to-end task guides — multi-step operations that aren't tied to a single panel.

## Authoring

- [Binding a Variable]({% link workflows/binding-a-variable.md %}) — the bind popup, step by step
- [Working with converters]({% link workflows/working-with-converters.md %}) — Compose, Format, custom `[SuiConverter]`
- [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}) — `UpdateTrigger.Manual` + `wrapper.Apply.<Field>()`
- [Embedding sub-UIs]({% link workflows/embedding-sub-uis.md %}) — `SuiReference` + ForEach
- [Events & Element refs]({% link workflows/events-and-refs.md %}) — wire `OnClick`, expose `@ref`

## Build & ship

- [Test in Play]({% link workflows/test-in-play.md %}) — preview your UI on a real player
- [Compile + output management]({% link workflows/compile-and-output.md %}) — folder picker, manifest, backup folder, Force Regen
- [User SCSS customization]({% link workflows/user-scss-customization.md %}) — add custom styles that survive recompile
- [Undo / Redo and commands]({% link workflows/undo-redo-commands.md %}) — how the command stack works

## Migration

- [Upgrading from V1.0]({% link workflows/upgrading-from-v1-0.md %}) — pointer to the full migration guide
