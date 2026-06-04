---
layout: default
title: Update-trigger matrix
parent: Reference
nav_order: 8
---

# Update-trigger matrix
{: .no_toc }

Per-widget × trigger table — which `UpdateTrigger` values each TwoWay binding can use. Source: `SuiBindingModeMatrix.AllowedUpdateTriggers`.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The triggers

```csharp
public enum SuiBindingUpdateTrigger
{
    OnChange,       // every change (keystroke / drag tick / click). Default.
    OnLostFocus,    // TextEntry only — commit on blur (click outside / Tab)
    OnSubmit,       // TextEntry only — commit on Enter key
    OnRelease,      // Slider only — commit on mouse-up after drag
    Manual,         // never auto-commit; user calls wrapper.Apply.<Field>() explicitly
}
```

See [Input & Update triggers]({% link concepts/input-and-update-triggers.md %}) for the mental model.

## Matrix

| Widget | Property | OnChange | OnLostFocus | OnSubmit | OnRelease | Manual | Default | Combo visible? |
|---|---|---|---|---|---|---|---|---|
| **TextEntry** | `Value` | ✓ | ✓ | ✓ | — | ✓ | OnChange | Yes |
| **Slider** | `Value` | ✓ | — | — | ✓ | ✓ | OnChange | Yes |
| **Toggle** | `Checked` | ✓ | — | — | — | ✓ | OnChange | No (hidden — only 1 meaningful choice) |
| **DropDown** | `Value` | ✓ | — | — | — | ✓ | OnChange | No |
| (any other TwoWay) | (any) | ✓ | — | — | — | ✓ | OnChange | No |

✓ = exposed by `AllowedUpdateTriggers`. The Bind popup's UpdateTrigger combo is **hidden entirely when only one trigger is meaningful** (no UI noise for widgets that have no real choice).

## Codegen per trigger

### `OnChange` (default)

Native Sandbox.UI `Property:bind=` syntax for the widget. Writes back on every change.

### `OnLostFocus` / `OnSubmit` (TextEntry only)

`Value=` one-way read + `@ref` + `onblur` / `onsubmit` handler:

```razor
<TextEntry Value="@PlayerName" @ref="PlayerNameRef"
           onblur=@(() => PlayerName = PlayerNameRef.Text) />
```

### `OnRelease` (Slider only)

Visual-buffer float decoupled from the bound Variable. `Tick()` detects the `HasActive` true→false transition and commits.

### `Manual`

No bind, no auto-write handler. The wrapper exposes `Apply.<Name>()` and `Apply.All()`. See [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}).

## When TwoWay isn't allowed

`UpdateTrigger` is ignored for `OneWay` and `OneTime` bindings (they never write back). The Bind popup hides the dropdown when Mode isn't TwoWay.

## See also

- [Bindings concept]({% link concepts/bindings.md %})
- [Input & Update triggers concept]({% link concepts/input-and-update-triggers.md %})
- [Binding-mode matrix]({% link reference/binding-mode-matrix.md %})
- [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %})
- [`Code/Runtime/SuiBindingModeMatrix.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Runtime/SuiBindingModeMatrix.cs) — source of truth
