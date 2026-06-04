---
layout: default
title: Input & Update triggers
parent: Concepts
nav_order: 12
---

# Input & Update triggers
{: .no_toc }

When a `TwoWay` binding actually writes the UI's current value back into the source Variable. V1.5 M4 — DEVIATIONS D-028 + D-029.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The problem

A `TextEntry.Value` bound `TwoWay` to a `PlayerName: string` Variable — should it write to the Variable on every keystroke? On Enter? On focus loss? On a Save button click?

V1.5 ships five triggers covering every common case, gated by a per-widget matrix so you only see the options that actually apply.

## The five triggers

```csharp
public enum SuiBindingUpdateTrigger
{
    OnChange,       // every change (keystroke / drag tick / click). Default.
    OnLostFocus,    // TextEntry only — commit on blur (click outside / Tab)
    OnSubmit,       // TextEntry only — commit on Enter key
    OnRelease,      // Slider only — commit on mouse-up after drag
    Manual,         // never auto-commit; user calls wrapper.Apply.<ElementName>Value() explicitly
}
```

Default is `OnChange` — pre-V1.5 behaviour. Existing V1.5 documents authored before D-028 shipped still load with `OnChange` and behave identically.

## Per-widget matrix

`SuiBindingModeMatrix.AllowedUpdateTriggers(elementType, property)` decides which triggers each widget exposes. The Bind popup **hides the dropdown entirely** when only one trigger is available (no UI noise for widgets that have no real choice).

| Widget | Property | Triggers exposed |
|---|---|---|
| `TextEntry` | `Value` | `OnChange` / `OnLostFocus` / `OnSubmit` / `Manual` |
| `Slider` | `Value` | `OnChange` / `OnRelease` / `Manual` |
| `Toggle` | `Checked` | `OnChange` / `Manual` (combo hidden — only choice that matters) |
| `DropDown` | `Value` | `OnChange` / `Manual` |
| (everything else with TwoWay) | (any) | `OnChange` / `Manual` |

See [Update-trigger matrix]({% link reference/update-triggers.md %}) for the per-widget table.

## What each trigger actually emits

### `OnChange` — TextEntry

```razor
<TextEntry Value:bind=@PlayerName />
```

Native Sandbox.UI `Value:bind=` syntax wires per-keystroke updates. No extra handler.

### `OnLostFocus` / `OnSubmit` — TextEntry

```razor
<TextEntry Value="@PlayerName"
           @ref="PlayerNameRef"
           onblur=@(() => PlayerName = PlayerNameRef.Text) />
```

The bound field gets a one-way `Value=` (read into the widget on render) plus an `onblur` (or `onsubmit`) handler that writes the field back when the event fires. `@ref` lets the handler reach the live widget's `.Text`.

### `OnChange` — Slider

Native `Value:bind=` writes per drag tick.

### `OnRelease` / `Manual` — Slider

```razor
<div class="sui-slider" ...>
  <!-- track / fill / thumb / tooltip -->
</div>

@code {
    public float Volume { get; set; }
    private float _volumeVisual;   // buffer driven by handlers

    protected override void Tick()
    {
        // Detect HasActive true → false transition for OnRelease
        // Idle ticks resync _volumeVisual = Volume so external writes flow back
    }
}
```

Slider markup is 100% custom (DEVIATIONS D-022) so the wrapper has full control over commit timing. A separate visual-buffer float decouples the displayed position from the bound Variable, which the wrapper writes on mouse release (OnRelease) or never auto-writes (Manual).

### `Manual` — TextEntry

```razor
<TextEntry Value="@PlayerName" @ref="PlayerNameRef" />
```

No `onblur` / `onsubmit` handler at all. The wrapper exposes a commit method (see [The Apply API](#the-apply-api)) — gameplay code calls it explicitly.

## The Apply API

For `Manual` triggers, the wrapper grows a nested `Apply` namespace exposed as `public ApplyApi Apply { get; }`. Each method is named after the **element's Name in the Hierarchy**, with `"Value"` appended:

```csharp
// .sui has elements named PlayerNameField (TextEntry), VolumeSlider (Slider),
// GraphicsDropdown (DropDown), each bound Manual:
Settings.Apply.PlayerNameFieldValue();   // commit one Manual binding
Settings.Apply.VolumeSliderValue();      // commit another
Settings.Apply.All();                    // commit every Manual field at once
```

The `Apply` class is **only emitted when at least one Manual binding exists** in the document — wrappers with no Manual bindings have no `Apply` property (autocomplete stays clean; `wrapper.Apply.X` compile-errors clearly when there's nothing to apply).

The `.All()` method invokes every Manual method in declaration order — the canonical "Save button" pattern in one call:

```csharp
[Property] public Game.UI.SettingsPanel Settings { get; set; } = new();

void OnSaveClick()
{
    Settings.Apply.All();   // flush every Manual binding the document declares
    SaveSettingsToDisk();
}
```

See [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}) for the full naming-rule callout + a worked codegen example.

## Why these triggers (the use cases)

| Use case | Trigger |
|---|---|
| Realtime — typing while game UI updates | `OnChange` |
| Form validation (only accept on Tab / Enter, no intermediate states) | `OnLostFocus` / `OnSubmit` |
| Expensive bound side effect (slider drives a costly recalc, fire once at release) | `OnRelease` |
| Explicit save (Apply / Cancel buttons, nothing commits until Apply) | `Manual` + `Apply.All()` |

## Cross-document export

Variables flagged `IsPublic` flow to the parent's wrapper as named-instance fields (`Parent.Child.VarName`). The `UpdateTrigger` only controls *when* the writes happen — not *whether* the bind is two-way. A parent reading `Parent.Child.Volume` after the slider releases will see the released value (for `OnRelease`) or the immediate value (for `OnChange`).

## See also

- [Bindings]({% link concepts/bindings.md %}) — the mode + chain mental model
- [Update-trigger matrix]({% link reference/update-triggers.md %}) — per-widget table
- [Manual commit with Apply workflow]({% link workflows/manual-commit-with-apply.md %}) — step-by-step
- [Settings screen tutorial]({% link tutorials/settings-screen.md %}) — exercises every trigger
- [TextEntry]({% link elements/text-entry.md %}) / [Slider]({% link elements/slider.md %}) / [Toggle]({% link elements/toggle.md %}) / [DropDown]({% link elements/dropdown.md %})
