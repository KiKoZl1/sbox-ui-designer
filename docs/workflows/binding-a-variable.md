---
layout: default
title: Binding a Variable
parent: Workflows
nav_order: 6
---

# Binding a Variable
{: .no_toc }

The Bind popup, step by step. The dialog covers picking the target property, source Variable, mode, update trigger, and optional converter chain.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Prerequisites

- A `.sui` open in the Designer.
- At least one Variable declared in the **Variables** tab. (If you don't have any: open the tab, click **+ Add Variable**, give it a name + type + default.)
- An element selected in the canvas with a bindable property.

## Opening the Bind popup

Two entry points:

1. **From the Details panel** — every bindable property has a small chain icon next to its label. Click it.
2. **From the Bindings tab** — bottom panel → Bindings → **+ Add Binding**. Pick the target element + property in the dialog header.

The popup opens centered on the editor.

## The popup layout

```
┌──────────────────────────────────────────────────────────┐
│  Bind ProgressBar.Value                                  │
├──────────────────────────────────────────────────────────┤
│  Source Variable    [ Health        ▾ ]   (Expects: float) │
│                                                          │
│  Mode               (●) OneWay  ( ) OneTime  ( ) TwoWay   │
│  Update Trigger     [ OnChange   ▾ ]   (hidden if N/A)   │
│                                                          │
│  Converter chain                                         │
│  ┌────────────────────────────────────────────────────┐  │
│  │  + Add Step                                         │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  Fallback Value     [ (use type default) ]               │
├──────────────────────────────────────────────────────────┤
│                                  [ Cancel ]  [   OK   ]  │
└──────────────────────────────────────────────────────────┘
```

## Step 1 — Pick the source Variable

The dropdown lists every Variable on the document. Each row is **tinted** by type compatibility with the target:

| Tint | Meaning |
|---|---|
| **Green** | Exact type match (no conversion needed) |
| **Yellow** | Convertible (e.g. `int` → `float` via builtin) |
| **Red** | Incompatible (would compile-error without a converter chain) |
| **Grey** | No declared target type (universal binding entry) |

The "Expects: X" hint on the right shows the target property's expected type. If you pick a yellow / red Variable, the converter chain section appears with a suggested first step.

## Step 2 — Pick the mode

Only modes allowed by `SuiBindingModeMatrix` for the `(element, property)` pair are enabled. The default mode is pre-selected (e.g. TwoWay for `TextEntry.Value`, OneWay for `Text.Text`).

See [Bindings]({% link concepts/bindings.md %}#binding-modes) for what each mode means.

## Step 3 — Pick the update trigger (TwoWay only)

The dropdown only appears when the binding is **TwoWay** AND the (element, property) pair allows >1 trigger. Hidden otherwise (no UI noise for widgets that have no real choice).

Visible options come from `SuiBindingModeMatrix.AllowedUpdateTriggers`. Default is `OnChange`.

See [Input & Update triggers]({% link concepts/input-and-update-triggers.md %}) for the per-widget table.

## Step 4 — Build the converter chain (optional)

Click **+ Add Step** to add a converter to the chain. Each step is a row:

```
┌──────────────────────────────────────────────────────────┐
│  ↑ ↓                                                     │
│  Converter   [ builtin.Clamp     ▾ ]                     │
│  Args:                                                   │
│    [ 0: 🔗 (chain feed) ]                                │
│    [ 1: → 0.0 (literal float) ]                          │
│    [ 2: → 100.0 (literal float) ]                        │
│  + Add Arg                                                │
│                                              [ Remove ]  │
└──────────────────────────────────────────────────────────┘
```

### Converter picker

The dropdown lists every builtin (sorted by Category — Math / Range / Conversion / Logic / String / Color / Collection) plus every user-declared `[SuiConverter]`. The suggester (typeahead) filters by name.

### Args

Each arg has a `Kind`:

- **Chain** — the value from the previous step. Default at position 0; click to relocate the chain feed to a different position (D-026 — chain reposition).
- **Literal** — a typed constant. Click to open `SuiLiteralInputDialog` and type a string / int / float / bool / Color / Vector value.
- **Variable** — pick another Variable from the picker.

### Reordering

Each step row has **↑ / ↓** buttons. Hidden (not just disabled) when no neighbour exists.

### Removing

Each row has a **Remove** button. Removing a step preserves the rest of the chain — the next step's chain feed reattaches automatically.

## Step 5 — Pick a fallback value (optional)

The **Fallback Value** field lets you pick a value used when the source Variable resolves to null. Null = use the property's type default.

## Step 6 — Click OK

The binding is added to `element.Bindings` and the Details panel updates inline:

- The bindable field's chain icon turns filled.
- The literal value display is replaced by `Health → Clamp(0,100) → Divide(100)`.

Save the document (`Ctrl+S`). Compile (`Ctrl+B`).

## TwoWay + converters — the auto-switch dialog

If you add a converter to a `TwoWay` binding, the Designer pops a `SuiConfirmDialog`:

> Adding a converter to a TwoWay binding will switch it to OneWay (converters can't round-trip). Continue?
>
> [ Cancel ]  [ OK, switch to OneWay ]

OK switches the binding's mode to `OneWay` (D-026 — TwoWay + converter auto-switch). Cancel keeps TwoWay (the converter is not added).

## Editing an existing binding

Click the chain icon next to the bound property in Details, or click the row in the Bindings tab. The popup reopens pre-filled. Edit + OK saves the changes.

To unbind, click **Clear** in the popup or delete the row from the Bindings tab.

## See also

- [Bindings concept]({% link concepts/bindings.md %}) — the mental model
- [Converters concept]({% link concepts/converters.md %}) — the chain step library
- [Working with converters workflow]({% link workflows/working-with-converters.md %}) — Compose, Format, custom
- [Input & Update triggers]({% link concepts/input-and-update-triggers.md %})
