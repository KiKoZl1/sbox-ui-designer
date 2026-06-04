---
layout: default
title: Upgrading from V1.0
parent: Workflows
nav_order: 10
---

# Upgrading from V1.0

V1.5 is fully backward-compatible. Drop in the V1.5 build, open any `.sui`, click **Migrate all** when the upgrade prompt appears.

The full procedure — including audit table, fallback steps, FAQ — lives in the dedicated upgrade guide:

[**V1.0 → V1.5 upgrade guide**]({% link UPGRADE_V1_0_TO_V1_5.md %})

## TL;DR

1. Commit your project (just in case).
2. Replace `Libraries/kikozl.sbox_ui_designer/` with the V1.5 build.
3. Open S&Box editor.
4. Open any `.sui` document — the upgrade prompt appears.
5. Click **Migrate all**.
6. (Recommended) restart the editor if the summary modal advises it.
7. Done.

## What changes for gameplay code

The only **shape change** worth mentioning is in generated code, not in the API:

> The generated panel renderer was renamed from `<Name>.razor` to `<Name>Panel.razor`.

Your `[Property] public MyHud Widget` declaration still works — the public type `MyHud` now points to the wrapper class (`MyHud.cs`), which extends `SuiPanel<MyHudPanel>` and provides `Add()` / `Show()` / `Hide()` / `Remove()`.

Practical impact:

- `[Property] public MyHud Widget` + `Widget.Show()` — **no changes**.
- `new MyHud()` cast as `PanelComponent` — swap to `widget.Add(scene); widget.Show();`.
- Hand-edited `<Name>.partial.cs` referencing the renderer by old name — rename `<Name>` → `<Name>Panel` in the file (only if you wrote one; the generator doesn't).

## What's preserved untouched

- `*.User.scss` — created once on first compile; never overwritten.
- `*.partial.cs` — your hand-written event handlers.
- `GameConverters.cs` — only your own functions; the scaffolder writes inside its `SUI:GENERATED:BEGIN` / `END` markers.

## See also

- [V1.0 → V1.5 upgrade guide]({% link UPGRADE_V1_0_TO_V1_5.md %}) — full procedure
- [Changelog]({% link support/changelog.md %}#v15--2026-06-03) — V1.5 release notes
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — the new shape explained
