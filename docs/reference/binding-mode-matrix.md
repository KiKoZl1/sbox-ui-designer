---
layout: default
title: Binding-mode matrix
parent: Reference
nav_order: 9
---

# Binding-mode matrix
{: .no_toc }

(element type, property) × mode table — what modes the validator + Bind popup allow. Source: `SuiBindingModeMatrix._matrix` + `_universal`.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## How to read this

Each (element type, property) pair declares:

- **Allowed modes** — which of `OneTime / OneWay / TwoWay` the binding can use.
- **Default mode** — what the Bind popup pre-selects.
- **TargetType** — the C# type the property expects (drives the "Expects: X" hint + type-tinted picker in the Bind popup).

`OneWayToSource` is reserved for V1.6 — never allowed in V1.5.

## Per-type entries

### Text

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `Text` | ✓ | ✓ | — | OneWay | string |
| `FontSize` | ✓ | ✓ | — | OneWay | float |
| `FontFamily` | ✓ | ✓ | — | OneWay | string |
| `FontWeight` | ✓ | ✓ | — | OneWay | string |
| `Color` | ✓ | ✓ | — | OneWay | Color |
| `TextAlign` | ✓ | ✓ | — | OneWay | string |
| `LineHeight` | ✓ | ✓ | — | OneWay | float |
| `LetterSpacing` | ✓ | ✓ | — | OneWay | float |

### Image

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `ImagePath` | ✓ | ✓ | — | OneWay | string |
| `Tint` | ✓ | ✓ | — | OneWay | Color |
| `FitMode` | ✓ | ✓ | — | OneWay | string |

### Button

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `ButtonText` | ✓ | ✓ | — | OneWay | string |

### ProgressBar

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `Value` | ✓ | ✓ | — | OneWay | float |
| `Min` | ✓ | ✓ | — | OneWay | float |
| `Max` | ✓ | ✓ | — | OneWay | float |
| `FillColor` | ✓ | ✓ | — | OneWay | Color |
| `Direction` | ✓ | ✓ | — | OneWay | string |

### Grid / InventoryGrid

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `Columns` | ✓ | ✓ | — | OneWay | int |
| `Rows` | ✓ | ✓ | — | OneWay | int |
| `CellWidth` | ✓ | ✓ | — | OneWay | float |
| `CellHeight` | ✓ | ✓ | — | OneWay | float |
| `Gap` | ✓ | ✓ | — | OneWay | float |

### Hotbar

Hotbar is always single-row, so only `Columns` is bindable — `Rows`, `CellWidth`, `CellHeight`, and `Gap` are **not** defined in `SuiBindingModeMatrix._matrix` for Hotbar and `IsBindable` returns false for them.

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `Columns` | ✓ | ✓ | — | OneWay | int |

### InventorySlot / ItemIcon

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `InventorySlot.SlotIndex` | ✓ | ✓ | — | OneWay | int |
| `InventorySlot.IconPath` | ✓ | ✓ | — | OneWay | string |
| `InventorySlot.Count` | ✓ | ✓ | — | OneWay | int |
| `ItemIcon.ImagePath` | ✓ | ✓ | — | OneWay | string |
| `ItemIcon.Tint` | ✓ | ✓ | — | OneWay | Color |

### Input widgets (V1.5 M4)

TwoWay-capable; TwoWay is the default. DropDown bind target is `SelectedIndex` (int via `Option.Value` index, exposed as `Value` on the wrapper) per DEVIATIONS D-024.

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `TextEntry.Value` | ✓ | ✓ | **✓** | **TwoWay** | string |
| `TextEntry.PlaceholderText` | ✓ | ✓ | — | OneWay | string |
| `TextEntry.ReadOnly` | ✓ | ✓ | — | OneWay | bool |
| `Slider.Value` | ✓ | ✓ | **✓** | **TwoWay** | float |
| `Slider.Min` | ✓ | ✓ | — | OneWay | float |
| `Slider.Max` | ✓ | ✓ | — | OneWay | float |
| `Toggle.Checked` | ✓ | ✓ | **✓** | **TwoWay** | bool |
| `Toggle.LabelText` | ✓ | ✓ | — | OneWay | string |
| `DropDown.SelectedIndex` | ✓ | ✓ | **✓** | **TwoWay** | int |

> **Note:** Some slugs above (`TextEntry.PlaceholderText`, `TextEntry.ReadOnly`, `Toggle.LabelText`, `DropDown.SelectedIndex`) are surfaced by the Details panel's chain icons but may not yet exist as matrix keys in `SuiBindingModeMatrix._matrix`. Until the runtime matrix is extended, the bind popup will reject them via `IsBindable=false`. Track the alignment against `Editor/Widgets/SuiDetailsWidget.cs` `bindingProperty:` strings.

## Universal entries (any element type)

Style / layout / state knobs from `SuiStyleData` + `SuiLayoutData` — bindable on **any** element type. Always OneWay.

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `Visibility` | ✓ | ✓ | — | OneWay | bool |
| `Enabled` | ✓ | ✓ | — | OneWay | bool |
| `IsDisabled` | ✓ | ✓ | — | OneWay | bool |
| `BackgroundColor` | ✓ | ✓ | — | OneWay | Color |
| `BackgroundImage` | ✓ | ✓ | — | OneWay | string |
| `BorderColor` | ✓ | ✓ | — | OneWay | Color |
| `BorderWidth` | ✓ | ✓ | — | OneWay | float |
| `BorderRadius` | ✓ | ✓ | — | OneWay | float |
| `Opacity` | ✓ | ✓ | — | OneWay | float |
| `Width` | ✓ | ✓ | — | OneWay | float |
| `Height` | ✓ | ✓ | — | OneWay | float |

> **Note:** `IsDisabled` and `BackgroundImage` are surfaced by the Details panel's chain icons (see `Editor/Widgets/SuiDetailsWidget.cs` lines 1473, 1547) but may not yet exist as entries in `SuiBindingModeMatrix._universal`. Until that runtime entry lands, `IsBindable` returns false for these and the bind popup will reject the attempt. For now, drive disabled state via the universal `Enabled` property (see `widgets/Button.md` workaround).

## Validator behaviour

`SuiBindingModeMatrix.IsBindable(elementType, property)` — true if the property is bindable.

`SuiBindingModeMatrix.IsModeAllowed(elementType, property, mode)` — true if the mode is allowed.

`SuiBindingModeMatrix.DefaultMode(elementType, property)` — the default mode.

`SuiBindingModeMatrix.GetTargetType(elementType, property)` — the expected TypeRef (drives the Bind popup's "Expects: X" hint).

`SuiBindingModeMatrix.BindableProperties(elementType)` — every property bindable on a type (per-type + universal).

Bindings that fail `IsModeAllowed` surface as a Compile Results error.

## See also

- [Bindings concept]({% link concepts/bindings.md %})
- [Binding a Variable workflow]({% link workflows/binding-a-variable.md %})
- [Update-trigger matrix]({% link reference/update-triggers.md %}) — once a binding is TwoWay, this matrix decides which triggers apply
- [`Code/Runtime/SuiBindingModeMatrix.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Runtime/SuiBindingModeMatrix.cs) — source of truth
