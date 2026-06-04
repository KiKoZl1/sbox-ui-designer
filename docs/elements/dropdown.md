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

`Value` UpdateTrigger: `OnChange` only. The Bind popup hides the dropdown — DropDown is atomic (1 pick = 1 commit), so deferred-commit modes (`Manual` etc.) don't model any real UX. Matrix-restricted at M4 close.

## Events surfaced

**None in V1.5.** `SuiEventMatrix` has no entry for `DropDown` — the Events tab won't show any slots and codegen emits no event `[Property]` on the wrapper. Wire reactivity via the TwoWay `Value` binding (int index) with `UpdateTrigger.OnChange` — DropDown is atomic (1 pick = 1 commit), so the bound Variable updates the instant the user picks an option.

## Why int, not string

PRD 21 § 11 #4 flagged this as an engine spike — `Sandbox.UI.DropDown` exposes no `SelectedIndex`, only `Value` (object) / `Selected` (Option) / `ValueChanged` (Action<string>).

V1.5 DEVIATIONS D-024 ships:

- **TwoWay binds against `DropDown.Value` (object)** at the engine level.
- Codegen pre-fills each `Sandbox.UI.Option` with `Value = <index>` at construction time (`new Option("Low", 0)`, `new Option("Medium", 1)`, …) so the bound C# field stays an `int`.
- The bound C# Variable stays an `int` matching the option's index.

## Codegen — `OnChange` trigger (default)

For a DropDown element named `GraphicsDropdown` bound TwoWay to a Variable `GraphicsPreset: int`, with options `["Low", "Medium", "High", "Ultra"]`:

```razor
<DropDown class="dropdown sui-el-graphics-dropdown"
          Options=@GraphicsDropdownOptions
          Value:bind="@GraphicsPreset" />

@code {
    public int GraphicsPreset { get; set; } = 1;

    public global::System.Collections.Generic.List<global::Sandbox.UI.Option> GraphicsDropdownOptions { get; set; }
        = new global::System.Collections.Generic.List<global::Sandbox.UI.Option> {
            new global::Sandbox.UI.Option( "Low",    0 ),
            new global::Sandbox.UI.Option( "Medium", 1 ),
            new global::Sandbox.UI.Option( "High",   2 ),
            new global::Sandbox.UI.Option( "Ultra",  3 ),
        };
}
```

Two things to note:

1. The Options list is a **public `List<Sandbox.UI.Option>` field on the renderer Panel** named `<ElementName>Options` (`GraphicsDropdownOptions` here). It's wired into the `<DropDown>` tag via `Options=@...`. The bound Variable `GraphicsPreset` updates per click via native `Value:bind`.
2. Each `Option.Value` carries the index, so the bound `int` field reads exactly which option is selected.

## Tutorial — drop + bind + read

1. **Drop** the DropDown at (40, 180), size 320×32.
2. **Variable** — add `GraphicsPreset: int` with `Default = 1` (Medium).
3. **Options** — in the Details DropDown section, click **+ Add** and type "Low", "Medium", "High", "Ultra".
4. **Bind** — Property: `Value` → Source: `GraphicsPreset` → Mode: `TwoWay`.
5. **Save + Compile**.
6. **Use from code**:

```csharp
[Property] public Game.UI.SettingsPanel Settings { get; set; } = new();

// User-declared enum in your gameplay code (matches option order: Low=0, Medium=1, High=2, Ultra=3).
public enum GraphicsQuality { Low = 0, Medium = 1, High = 2, Ultra = 3 }

void OnApplyClick()
{
    var preset = (GraphicsQuality)Settings.GraphicsPreset;  // cast bound int → user enum
    GraphicsSystem.ApplyPreset( preset );
}
```

## Deferred to V1.6 (DEVIATIONS D-024)

- **Dynamic Options** — binding `DropDown.Options` to a `List<Option>` Variable so the menu rebuilds when the list changes.

## See also

- [Bindings]({% link concepts/bindings.md %})
- [Input & Update triggers]({% link concepts/input-and-update-triggers.md %})
- [Settings screen tutorial]({% link tutorials/settings-screen.md %})
