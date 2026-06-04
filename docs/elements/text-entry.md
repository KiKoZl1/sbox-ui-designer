---
layout: default
title: TextEntry
parent: Element reference
nav_order: 16
---

# TextEntry
{: .no_toc }

A single-line text input. Backed by `Sandbox.UI.TextEntry`. The first SUI input widget that **reads** user input back into a Variable via `TwoWay` binding.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What it is

`TextEntry` is V1.5 M4 (PRD 21 § 3.1). Drop one onto the canvas under **INPUT WIDGETS → TextEntry**. Bind `Value` to a `string` Variable with `TwoWay` mode and you have a player-editable name field, a chat input, a search box.

The widget auto-sets `pointer-events: all`. Default `MaxLength = 0` (unbounded).

## Properties (TextEntry section)

| Field | Default | Notes |
|---|---|---|
| **Placeholder Text** | `""` | Shown when the value is empty |
| **Max Length** | `0` | `0 = unbounded`; positive caps input length |
| **Read Only** | `false` | Disables interaction; styles as disabled |
| **Preview Value** | `""` | Design-time only — what the canvas shows |

Font Size / Weight / Color / Padding come from the **Text** section (TextEntry inherits the Text family fields).

## Bindable properties

| Property | Mode | Target type |
|---|---|---|
| `Value` | OneTime / OneWay / **TwoWay** (default) | `string` |
| `Placeholder` | OneTime / OneWay | `string` |
| Style + Universal | OneWay | per matrix |

`Value` is TwoWay by default. Per-binding `UpdateTrigger` (V1.5 D-028) — pick `OnChange` (per keystroke) / `OnLostFocus` (commit on blur) / `OnSubmit` (commit on Enter) / `Manual` (call `wrapper.Apply.<ElementName>Value()`). See [Input & Update triggers]({% link concepts/input-and-update-triggers.md %}).

## Events surfaced

**None in V1.5.** `SuiEventMatrix` does not register any event slots for `TextEntry` — the Designer's Add Event dialog hides TextEntry elements, and `SuiDocumentValidator` rejects any event keyed on a TextEntry as "not surfaced".

Value commits flow through the `UpdateTrigger` on a `TwoWay` binding (see Codegen sections below):

- **`OnChange`** — per-keystroke write into the wrapper Variable (substitute for `OnValueChanged`).
- **`OnSubmit`** — write on Enter (substitute for an `OnSubmit` event).
- **`OnLostFocus`** — write on blur (substitute for an `OnBlur` event).
- **`Manual`** — call `wrapper.Apply.<ElementName>Value()` to commit.

If you need a side effect on commit, wrap the bound Variable with a property setter on the host Controller that triggers your handler. `OnFocus` / `OnBlur` exposure as first-class Designer events is tracked for V1.6.

## Codegen — `OnChange` trigger

For a `TextEntry.Value ← PlayerName: string` with the default `OnChange` trigger:

```razor
<TextEntry Value:bind=@PlayerName />
```

Native Sandbox.UI two-way bind syntax — writes per keystroke into the wrapper's `[Property] string PlayerName`.

## Codegen — `OnLostFocus` / `OnSubmit` trigger

```razor
<TextEntry Value="@PlayerName"
           @ref="PlayerNameRef"
           onblur=@(() => PlayerName = PlayerNameRef.Text) />
```

The `Value="@PlayerName"` reads into the widget per render; `onblur` writes back when the field loses focus. Same shape for `OnSubmit` but with the `onsubmit` handler.

## Codegen — `Manual` trigger

For a TextEntry **element named `PlayerNameField`** bound `Manual` to a Variable `PlayerName`:

```razor
<TextEntry Value="@PlayerName" @ref="PlayerNameFieldRef" />
```

No write-back handler. The wrapper exposes `Settings.Apply.PlayerNameFieldValue()` (method name = element name + `"Value"`) — call it from gameplay code (typically a Save button handler). See [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}).

## Use it from gameplay code

```csharp
public sealed class SettingsController : Component
{
    [Property] public Game.UI.SettingsPanel Settings { get; set; } = new();

    protected override void OnStart()
    {
        Settings.PlayerName = "Player";   // initial value
        Settings.Show( SuiInputMode.All ); // need keyboard for typing
    }

    void OnSaveClick()
    {
        Settings.Apply.All();                  // flush any Manual bindings
        Log.Info( $"saving: {Settings.PlayerName}" );
    }
}
```

`SuiInputMode.All` makes the host panel accept keyboard focus so the TextEntry can receive input. See [Wrapper API]({% link reference/wrapper-api.md %}#input-mode).

## Tutorial — drop + bind + read

1. **Drop** the TextEntry onto the canvas at (40, 60), size 240×32.
2. **Variable** — add `PlayerName: string` with `Default = "Player"`, `IsPublic = false`.
3. **Bind** — Bind dialog → Property: `Value` → Source: `PlayerName` → Mode: `TwoWay` → UpdateTrigger: `OnChange`.
4. **Save + Compile**.
5. **Use from code** (see above) — typing in the field updates `Settings.PlayerName` instantly.

## Dropped from V1.5 (DEVIATIONS D-023)

These fields exist in PRD 21 but **do not ship in V1.5**:

- **`IsPassword`** — `Sandbox.UI.TextEntry` has no `IsPassword` property. Password masking requires a custom widget or engine-side work. Deferred to V1.6.
- **`AutoFocus`** — deferred. Workaround: call `panel.<Name>Ref?.Focus()` from `OnAfterTreeRender(true)`.
- **`Multiline`** / **`Numeric`** — engine `TextEntry` has them but they're not exposed yet (V1.5 default is single-line text).

## See also

- [Bindings]({% link concepts/bindings.md %})
- [Input & Update triggers]({% link concepts/input-and-update-triggers.md %})
- [Wrapper API]({% link reference/wrapper-api.md %})
- [Settings screen tutorial]({% link tutorials/settings-screen.md %})
