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
    Manual,         // never auto-commit; user calls wrapper.Apply.<ElementName>Value() explicitly. Only TextEntry + Slider expose this; Toggle / DropDown can't pick it (atomic widgets).
}
```

See [Input & Update triggers]({% link concepts/input-and-update-triggers.md %}) for the mental model.

## Matrix

| Widget | Property | OnChange | OnLostFocus | OnSubmit | OnRelease | Manual | Default | Combo visible? |
|---|---|---|---|---|---|---|---|---|
| **TextEntry** | `Value` | ✓ | ✓ | ✓ | — | ✓ | OnChange | Yes |
| **Slider** | `Value` | ✓ | — | — | ✓ | ✓ | OnChange | Yes |
| **Toggle** | `Checked` | ✓ | — | — | — | — | OnChange | No (only 1 meaningful choice) |
| **DropDown** | `Value` | ✓ | — | — | — | — | OnChange | No |
| (any other TwoWay) | (any) | ✓ | — | — | — | — | OnChange | No |

✓ = exposed by `AllowedUpdateTriggers`. The Bind popup's UpdateTrigger combo is **hidden entirely when only one trigger is meaningful** (no UI noise for widgets that have no real choice).

Toggle / DropDown intentionally do **not** allow `Manual`: their interaction is atomic (one click = one value change), so deferring the commit doesn't model any real UX. Earlier alpha builds exposed `Manual` here but the codegen never emitted matching `Apply.<Name>Value()` methods for those widgets — picking Manual was a no-op. Resolved at M4 close (2026-06-04 matrix cleanup).

## Codegen per trigger

### `OnChange` (default)

Native Sandbox.UI `Property:bind=` syntax for the widget. Writes back on every change.

### `OnLostFocus` / `OnSubmit` (TextEntry only)

`Value=` one-way read + `@ref` + `onblur` / `onsubmit` handler:

```razor
<TextEntry Value="@PlayerName" @ref="PlayerNameFieldRef"
           onblur=@(() => PlayerName = PlayerNameFieldRef.Text) />
```

The `Ref` suffix is appended to the element's Name in the Hierarchy (`PlayerNameField`), not to the bound Variable's name (`PlayerName`). See [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}) for the full Apply API naming rule.

### `OnRelease` (Slider only)

Visual-buffer float decoupled from the bound Variable. `Tick()` detects the `HasActive` true→false transition and commits.

### `Manual` (TextEntry + Slider only)

No bind, no auto-write handler. The wrapper exposes `Apply.<ElementName>Value()` and `Apply.All()` — the method name is derived from the element's Name in the Hierarchy + the literal suffix `"Value"`. See [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}).

Toggle + DropDown can't pick `Manual` — they're atomic (1 click = 1 commit), and the matrix no longer offers the choice.

## When TwoWay isn't allowed

`UpdateTrigger` is ignored for `OneWay` and `OneTime` bindings (they never write back). The Bind popup hides the dropdown when Mode isn't TwoWay.

## See also

- [Bindings concept]({% link concepts/bindings.md %})
- [Input & Update triggers concept]({% link concepts/input-and-update-triggers.md %})
- [Binding-mode matrix]({% link reference/binding-mode-matrix.md %})
- [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %})
- [`Code/Runtime/SuiBindingModeMatrix.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Runtime/SuiBindingModeMatrix.cs) — source of truth
