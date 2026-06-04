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

### Grid / InventoryGrid / Hotbar

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `Columns` | ✓ | ✓ | — | OneWay | int |
| `Rows` | ✓ | ✓ | — | OneWay | int |
| `CellWidth` | ✓ | ✓ | — | OneWay | float |
| `CellHeight` | ✓ | ✓ | — | OneWay | float |
| `Gap` | ✓ | ✓ | — | OneWay | float |

(Hotbar exposes only `Columns` since it's always single-row.)

### InventorySlot / ItemIcon

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `InventorySlot.SlotIndex` | ✓ | ✓ | — | OneWay | int |
| `InventorySlot.IconPath` | ✓ | ✓ | — | OneWay | string |
| `InventorySlot.Count` | ✓ | ✓ | — | OneWay | int |
| `ItemIcon.ImagePath` | ✓ | ✓ | — | OneWay | string |
| `ItemIcon.Tint` | ✓ | ✓ | — | OneWay | Color |

### Input widgets (V1.5 M4)

TwoWay-capable; TwoWay is the default. DropDown bind target is `Value` (int via `Option.Value` index) per DEVIATIONS D-024.

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `TextEntry.Value` | ✓ | ✓ | **✓** | **TwoWay** | string |
| `TextEntry.Placeholder` | ✓ | ✓ | — | OneWay | string |
| `Slider.Value` | ✓ | ✓ | **✓** | **TwoWay** | float |
| `Slider.Min` | ✓ | ✓ | — | OneWay | float |
| `Slider.Max` | ✓ | ✓ | — | OneWay | float |
| `Toggle.Checked` | ✓ | ✓ | **✓** | **TwoWay** | bool |
| `DropDown.Value` | ✓ | ✓ | **✓** | **TwoWay** | int |

## Universal entries (any element type)

Style / layout / state knobs from `SuiStyleData` + `SuiLayoutData` — bindable on **any** element type. Always OneWay.

| Property | OneTime | OneWay | TwoWay | Default | TargetType |
|---|---|---|---|---|---|
| `Visibility` | ✓ | ✓ | — | OneWay | bool |
| `Enabled` | ✓ | ✓ | — | OneWay | bool |
| `BackgroundColor` | ✓ | ✓ | — | OneWay | Color |
| `BorderColor` | ✓ | ✓ | — | OneWay | Color |
| `BorderWidth` | ✓ | ✓ | — | OneWay | float |
| `BorderRadius` | ✓ | ✓ | — | OneWay | float |
| `Opacity` | ✓ | ✓ | — | OneWay | float |
| `Width` | ✓ | ✓ | — | OneWay | float |
| `Height` | ✓ | ✓ | — | OneWay | float |

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
