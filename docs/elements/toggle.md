---
layout: default
title: Toggle
parent: Element reference
nav_order: 18
---

# Toggle
{: .no_toc }

A boolean checkbox. Backed by `Sandbox.UI.Checkbox`. V1.5 ships **only the default Checkbox visual** — pill / switch variants are deferred (DEVIATIONS D-025).
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What it is

`Toggle` is V1.5 M4 (PRD 21 § 3.3). Drop one onto the canvas under **INPUT WIDGETS → Toggle**. Bind `Checked` to a `bool` Variable with `TwoWay` mode and you have a settings checkbox.

Auto-sets `pointer-events: all`.

## Properties (Toggle section)

| Field | Default | Notes |
|---|---|---|
| **Toggle Checked** | `false` | Design-time preview |
| **Toggle Label Text** | `""` | Optional label rendered next to the checkbox |

The engine `Toggle` type doesn't exist — only `Checkbox` does (PRD 21 § 11 #3 confirmed via M0 spike). Pill / Switch variants in PRD 21 § 3.3 are achieved purely via SCSS over the `Checkbox` primitive in V1.6 — V1.5 ships only the default checkmark-box visual.

## Bindable properties

| Property | Mode | Target type |
|---|---|---|
| `Checked` | OneTime / OneWay / **TwoWay** (default) | `bool` |
| Style + Universal | OneWay | per matrix |

`Checked` UpdateTrigger options: `OnChange` (atomic — single click) / `Manual`. The Bind popup **hides the dropdown** since there's no meaningful third option.

## Events surfaced

| Event | Signature |
|---|---|
| `OnValueChanged` | `Action<bool>` |

## Codegen — `OnChange` trigger (default)

```razor
<Checkbox Checked:bind=@MusicEnabled />
@if ( !string.IsNullOrEmpty( ToggleLabelText ) )
{
    <label class="sui-toggle-label">@ToggleLabelText</label>
}
```

Native `Checked:bind=` writes the new bool into the wrapper's `[Property] bool MusicEnabled` on click.

## Codegen — `Manual` trigger

```razor
<Checkbox Checked="@MusicEnabled" @ref="MusicToggleRef" />
```

No bind. **Known gap (V1.5):** the wrapper emits an `@ref` but does NOT generate an `Apply.*` method for Toggle (the Apply codegen only fires for TextEntry + Slider — see `Code/Generation/SuiWrapperEmitter.cs` `EmitManualCommitMethods`). User code must read the checkbox state manually:

```csharp
void OnSaveClick()
{
    if ( Settings.View?.MusicToggleRef is { } cb )
        Settings.MusicEnabled = cb.Checked;
    Settings.Apply.All();   // covers TextEntry / Slider Manual bindings
}
```

A future release will extend `Apply` to cover Toggle (and DropDown) — see [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}).

## Tutorial — drop + bind + read

1. **Drop** the Toggle at (40, 140), size 320×32.
2. **Variable** — add `MusicEnabled: bool` with `Default = true`.
3. **Bind** — Property: `Checked` → Source: `MusicEnabled` → Mode: `TwoWay` (only option).
4. **Save + Compile**.
5. **Use from code**:

```csharp
[Property] public Game.UI.SettingsPanel Settings { get; set; } = new();

void Update()
{
    AudioSystem.MusicMuted = !Settings.MusicEnabled;
}
```

## Deferred to V1.6 (DEVIATIONS D-025)

- Pill / Switch visual variants — author-side SCSS over the existing `Checkbox` is the V1.5 workaround:

```scss
// MyPanel.User.scss
.sui-toggle-A1 .checkbox {
  /* style as a pill switch */
  background-color: var(--off-color);
  ...
}
```

A `ToggleStyle` enum + bundled SCSS class libraries ship in V1.6.

## See also

- [Bindings]({% link concepts/bindings.md %})
- [Input & Update triggers]({% link concepts/input-and-update-triggers.md %})
- [Settings screen tutorial]({% link tutorials/settings-screen.md %})
