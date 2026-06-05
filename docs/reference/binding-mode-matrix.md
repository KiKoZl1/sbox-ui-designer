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
| `IsHighlighted` | ✓ | ✓ | — | OneWay | bool |

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
| `InventorySlot.IsHighlighted` | ✓ | ✓ | — | OneWay | bool |
| `ItemIcon.ImagePath` | ✓ | ✓ | — | OneWay | string |
| `ItemIcon.Tint` | ✓ | ✓ | — | OneWay | Color |
| `ItemIcon.IsHighlighted` | ✓ | ✓ | — | OneWay | bool |

`IsHighlighted` is defined **per-type** (Button / InventorySlot / ItemIcon) — it is **not** a universal entry. Other element types (Text, Image, ProgressBar, Grid, Hotbar, input widgets) have no Highlighted state and reject `IsHighlighted` bindings at validation time. Drive Highlighted from any `bool` Variable; pair with the **Highlighted Style** picker in the Designer's Appearance section to author the lit look. See the [tab strip example]({% link concepts/interactive-states.md %}#worked-example--tab-strip-with-highlighted).

### Input widgets (V1.5 M4)

TwoWay-capable; TwoWay is the default. DropDown bind target is `Value` (int — the engine `DropDown.Value` returns the selected `Option.Value` index) per DEVIATIONS D-024.

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

## Matrix gaps deferred to V1.6

The Details panel surfaces chain icons for a few properties that intentionally have **no** entry in `SuiBindingModeMatrix._matrix` / `_universal` for V1.5. The bind popup rejects them via `IsBindable=false`, which is the correct behaviour — the rows below are listed here so users understand the gap is deliberate, not a stale doc.

Per-type slugs surfaced in `Editor/Widgets/SuiDetailsWidget.cs` that are **not** matrix keys:

| Slug | Surfaced at | Workaround for V1.5 |
|---|---|---|
| `TextEntry.PlaceholderText` | `SuiDetailsWidget.cs:805` | Set the literal in the Details panel — `Placeholder` is the matrix key if you want it bound. |
| `TextEntry.ReadOnly` | `SuiDetailsWidget.cs:813` | Drive via the universal `Enabled` property. |
| `Toggle.LabelText` | `SuiDetailsWidget.cs:860` | Bind a sibling `Text` element next to the toggle. |
| `DropDown.SelectedIndex` | `SuiDetailsWidget.cs:867` | Bind `DropDown.Value` instead — engine returns the selected `Option.Value` (int). |

Universal slugs surfaced but **not** in `_universal`:

| Slug | Surfaced at | Workaround for V1.5 |
|---|---|---|
| `IsDisabled` | `SuiDetailsWidget.cs:1473` | Drive disabled state via the universal `Enabled` property (see `widgets/Button.md`). |
| `BackgroundImage` | `SuiDetailsWidget.cs:1547` | Set the literal in the Details panel; image-path binding lands in V1.6. |

Tracked for V1.6. Until the runtime matrix is extended, these slugs stay non-bindable.

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
