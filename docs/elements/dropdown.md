---
layout: default
title: DropDown
parent: Element reference
nav_order: 19
---

# DropDown
{: .no_toc }

A selection dropdown. Backed by `Sandbox.UI.DropDown`. TwoWay binds against `Value` (int via `Option.Value` index) per DEVIATIONS D-024 — `Sandbox.UI.DropDown` exposes no `SelectedIndex` property.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What it is

`DropDown` is V1.5 M4 (PRD 21 § 3.4). Drop one onto the canvas under **INPUT WIDGETS → DropDown**. Author the options list inline + bind `Value` to an `int` Variable with `TwoWay` mode and you have a settings dropdown.

Auto-sets `pointer-events: all`.

## Properties (DropDown section)

| Field | Default | Notes |
|---|---|---|
| **DropDown Options** | `[]` | Inline list editor — string per row (e.g. `["Low", "Medium", "High"]`) |
| **DropDown Selected Index** | `0` | Design-time preview which option is selected |

V1.5 ships static options (authored in the Designer). **Dynamic options** (bind `Options` to a `List<Option>` Variable) is deferred to V1.6 (DEVIATIONS D-024).

## Bindable properties

| Property | Mode | Target type |
|---|---|---|
| `Value` | OneTime / OneWay / **TwoWay** (default) | `int` (via `Option.Value` index) |
| Style + Universal | OneWay | per matrix |

`Value` UpdateTrigger options: `OnChange` / `Manual`. The Bind popup **hides the dropdown** since there's no meaningful third option.

## Events surfaced

| Event | Signature |
|---|---|
| `OnValueChanged` | `Action<int>` |

## Why int, not string

PRD 21 § 11 #4 flagged this as an engine spike — `Sandbox.UI.DropDown` exposes no `SelectedIndex`, only `Value` (object) / `Selected` (Option) / `ValueChanged` (Action<string>).

V1.5 DEVIATIONS D-024 ships:

- **TwoWay binds against `DropDown.Value` (object)** at the engine level.
- Codegen pre-fills each `Sandbox.UI.Option` with `Value = <index>` at construction time (`new Option("Low", 0)`, `new Option("Medium", 1)`, …) so the bound C# field stays an `int`.
- The bound C# Variable stays an `int` matching the option's index.

## Codegen — `OnChange` trigger (default)

```razor
<DropDown Value:bind=@GraphicsPreset>
    <option value="0">Low</option>
    <option value="1">Medium</option>
    <option value="2">High</option>
    <option value="3">Ultra</option>
</DropDown>
```

Wait — that's not actually how `Sandbox.UI.DropDown` consumes options. The real codegen builds the Options list at construction:

```razor
<DropDown Value:bind=@GraphicsPreset @ref="GraphicsPresetRef" />

@code {
    public int GraphicsPreset { get; set; }

    protected override void OnAfterTreeRender( bool firstTime )
    {
        if ( firstTime && GraphicsPresetRef != null )
        {
            GraphicsPresetRef.Options.Add( new global::Sandbox.UI.Option( "Low",    0 ) );
            GraphicsPresetRef.Options.Add( new global::Sandbox.UI.Option( "Medium", 1 ) );
            GraphicsPresetRef.Options.Add( new global::Sandbox.UI.Option( "High",   2 ) );
            GraphicsPresetRef.Options.Add( new global::Sandbox.UI.Option( "Ultra",  3 ) );
        }
    }
}
```

The bound `int GraphicsPreset` updates per click.

## Codegen — `Manual` trigger

```razor
<DropDown @ref="GraphicsPresetRef" />
```

No bind. The wrapper exposes `Settings.Apply.GraphicsPreset()`. See [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}).

## Tutorial — drop + bind + read

1. **Drop** the DropDown at (40, 180), size 320×32.
2. **Variable** — add `GraphicsPreset: int` with `Default = 1` (Medium).
3. **Options** — in the Details DropDown section, click **+ Add** and type "Low", "Medium", "High", "Ultra".
4. **Bind** — Property: `Value` → Source: `GraphicsPreset` → Mode: `TwoWay`.
5. **Save + Compile**.
6. **Use from code**:

```csharp
[Property] public Game.UI.SettingsPanel Settings { get; set; } = new();

void OnApplyClick()
{
    var preset = (GraphicsPreset)Settings.GraphicsPreset;  // cast int → enum
    GraphicsSystem.ApplyPreset( preset );
}
```

## Deferred to V1.6 (DEVIATIONS D-024)

- **Dynamic Options** — binding `DropDown.Options` to a `List<Option>` Variable so the menu rebuilds when the list changes.

## See also

- [Bindings]({% link concepts/bindings.md %})
- [Input & Update triggers]({% link concepts/input-and-update-triggers.md %})
- [Settings screen tutorial]({% link tutorials/settings-screen.md %})
