---
layout: default
title: Bindings
parent: Concepts
nav_order: 7
---

# Bindings
{: .no_toc }

A binding connects one element property to one Variable, optionally through a chain of converters. Bindings are how the data you declared as [Variables]({% link concepts/variables.md %}) actually drives the visual output.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What a binding is

A `SuiBinding` lives on a `SuiElement` (under `element.Bindings`). It carries:

- **`Property`** — the target property name (e.g. `Value`, `Text`, `Tint`, `BackgroundColor`).
- **`Source`** — the data origin (a Variable on this document, identified by GUID).
- **`Mode`** — how data flows. See below.
- **`UpdateTrigger`** — when TwoWay writes commit. See [Input & Update triggers]({% link concepts/input-and-update-triggers.md %}).
- **`Converters`** — ordered chain applied between Source and Property.
- **`FallbackValue`** — value used when the Source is null.

A bound property overrides the literal in `Props`. The Details panel shows the binding chain in place of the literal value + a small chain icon.

## Binding modes

The matrix `SuiBindingModeMatrix` decides which modes are allowed for each `(elementType, property)` pair. The closed set:

| Mode | Direction | When read | When written |
|---|---|---|---|
| **`OneTime`** | Source → Property | First render only | Never |
| **`OneWay`** | Source → Property | Every `BuildHash()` change | Never |
| **`TwoWay`** | Source ↔ Property | Every change | Per `UpdateTrigger` (default `OnChange`) |

`TwoWay` is only allowed for input widgets (`TextEntry.Value` / `Slider.Value` / `Toggle.Checked` / `DropDown.Value`) — these are the V1.5 widgets that actually produce values back from user input. See [Binding-mode matrix]({% link reference/binding-mode-matrix.md %}) for the per-property table.

`SuiBindingMode.OneWayToSource` exists in the enum but is hard-wired to **disallowed** by `SuiBindingModeMatrix.IsModeAllowed` (always returns `false`). The Bind popup never offers it. Reserved for V1.6.

## How bindings generate

For a `ProgressBar.Value` bound to `Variable Health` with `OneWay`:

```razor
@* ProgressBarPanel.razor — emitted *@
<div class="health-bar sui-health-bar">
    <div class="fill" style="@FillStyle"></div>
</div>

@code {
    public int Health { get; set; }  // mirrored from wrapper

    private string FillStyle => $"width: {Math.Clamp(Health, 0, 100)}%;";
}
```

For a `TextEntry.Value` bound to `Variable PlayerName` with `TwoWay`:

```razor
<TextEntry Value:bind=@PlayerName @ref="PlayerNameRef" />
```

The native Sandbox.UI `Value:bind=` syntax does the two-way wiring. The wrapper's `[Property] string PlayerName` is the canonical store.

## The `Source` field

```jsonc
{
  "VariableId": "var_a3f9b21c"
}
```

V1.5 ships a single source shape — a Variable referenced by its stable `VariableId` GUID. Cross-document refs / scene globals are reached by first exposing them as a Variable (see [Variables — Source kinds]({% link concepts/variables.md %}#source-kinds-manual--fromcomponent--fromactiongraph)).

## The converter chain

Each binding can carry an ordered list of converter steps:

```
Variable (raw)  →  Step 1  →  Step 2  →  Step 3  →  Property
```

Each step is `(Ref, Args[])`. The chain feed (the value from the previous step) plugs into a specific arg position (default 0 — clickable in the UI to relocate the chain feed). Other args can be:

- **Variables** — referenced by `var_XXXXXXXX`.
- **Literals** — typed in the literal input dialog (string / int / float / bool / Color / Vector).

See [Converters]({% link concepts/converters.md %}) for the full mental model + builtin catalog.

## Fallback values

Each binding has an optional `FallbackValue` — used when the Source resolves to null at render time. Null `FallbackValue` means "use the property's type default" (e.g. `default(float) = 0`).

Useful for `string` Variables that haven't been initialised yet (`null` text would otherwise show as the literal "null" in some configurations).

## Bindable properties

`SuiBindingModeMatrix.BindableProperties(elementType)` enumerates everything bindable on a given type. It's a union of:

### Per-type entries

Each element type declares its own bindable property list, e.g.:

- `Text` — Text, FontSize, FontFamily, FontWeight, Color, TextAlign, LineHeight, LetterSpacing.
- `Image` — ImagePath, Tint, FitMode.
- `ProgressBar` — Value, Min, Max, FillColor, Direction.
- `TextEntry` — Value (TwoWay), Placeholder.
- `Slider` — Value (TwoWay), Min, Max.
- `Toggle` — Checked (TwoWay).
- `DropDown` — Value (TwoWay).

### Universal entries

Properties bindable on **any** element type (style / layout / state knobs from `SuiStyleData` + `SuiLayoutData`):

- State — `Visibility`, `Enabled` (bool).
- Style — `BackgroundColor`, `BorderColor`, `BorderWidth`, `BorderRadius`, `Opacity`.
- Layout — `Width`, `Height`.

See [Binding-mode matrix]({% link reference/binding-mode-matrix.md %}) for the complete table with allowed modes + default mode + expected target type per entry.

## Authoring a binding

The Bind popup walks you through it. Five fields:

1. **Property** — the target property on the selected element.
2. **Source Variable** — picker tinted green / yellow / red / grey based on type compatibility with the target.
3. **Mode** — only allowed modes appear; default pre-selected.
4. **UpdateTrigger** — only shown when TwoWay + the (element, property) pair allows >1 trigger.
5. **Converters** — empty by default; add steps + literal args + reorder + reposition the chain feed.

See [Binding a Variable workflow]({% link workflows/binding-a-variable.md %}).

## Storage on disk

In the `.sui` JSON, the binding lives at `element.Bindings[i]`:

```jsonc
{
  "Id": "bind_c4d6e7a8",
  "Property": "Value",
  "Mode": "OneWay",
  "UpdateTrigger": "OnChange",
  "Source": { "VariableId": "var_a3f9b21c" },
  "Converters": [
    {
      "Ref": "builtin.Clamp",
      "Args": [
        { "Kind": "Chain" },
        { "Kind": "Literal", "Type": "float", "Value": 0 },
        { "Kind": "Literal", "Type": "float", "Value": 100 }
      ]
    }
  ],
  "FallbackValue": null
}
```

See [SUI JSON schema]({% link reference/sui-json-schema.md %}#bindings-block-per-element).

## Broken bindings

V1.5 D-026 ships the "broken binding" visual: red ⚠ icon + border + tinted bg + tooltip when the binding references a deleted Variable or unknown converter. The Compile Results panel surfaces every broken binding before generation runs — see [Troubleshooting]({% link support/troubleshooting.md %}#my-binding-shows-a-red--icon).

## See also

- [Variables]({% link concepts/variables.md %})
- [Converters]({% link concepts/converters.md %})
- [Input & Update triggers]({% link concepts/input-and-update-triggers.md %}) — when TwoWay writes commit
- [Binding-mode matrix]({% link reference/binding-mode-matrix.md %}) — per-property allowed modes
- [Binding a Variable workflow]({% link workflows/binding-a-variable.md %}) — step-by-step bind popup
