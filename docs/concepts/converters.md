---
layout: default
title: Converters
parent: Concepts
nav_order: 8
---

# Converters
{: .no_toc }

Pure functions that transform a value as it flows from a Variable to an element property. SUI Designer ships 66 builtins and lets you add your own with one `[SuiConverter]` attribute.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The mental model

Bindings carry an ordered chain of converter steps:

```
Variable Health (int)
       │
       ▼  builtin.IntToFloat
       │
       ▼  builtin.Clamp(0, 100)
       │
       ▼  builtin.Divide(100)
       │
       ▼  ProgressBar.Value (float, 0..1)
```

Each step is a pure function. The chain output's type must satisfy the target property's expected type — the validator catches mismatches at compile time.

## Builtin catalog (66)

Seven categories:

| Category | Count | Examples |
|---|---|---|
| **Math** | 10 | Add, Subtract, Multiply, Divide, Modulo, Power, Absolute, Negate, Min, Max |
| **Range** | 9 | Clamp, Clamp01, Map, Lerp, InverseLerp, SmoothStep, Round, Floor, Ceil |
| **Conversion** | 5 | IntToFloat, FloatToInt (with mode: Round/Floor/Ceil/Truncate), LongToInt, IntToString, FloatToString, Parse |
| **Logic** | 10 | Equal, NotEqual, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual, If, And, Or, Not |
| **String** | 13 | Concat, **Compose** (V1.5), Format, Uppercase, Lowercase, TitleCase, Length, Substring, Replace, Contains, StartsWith, EndsWith, Trim, IndexOf, Split |
| **Color** | 9 | MakeColor, ColorFromHex, ColorLerp, WithAlpha, Darken, Lighten, ColorMultiply, GetAlpha, Invert, Grayscale |
| **Collection** | 6 | Count, IsEmpty, First, Last, ElementAt, Slice |

Every builtin lives in `Code/Runtime/SuiBuiltinConverters.cs` as a `public static` method tagged with `[SuiConverter]`. Full per-method signature + defaults: see [Converters catalog]({% link reference/converters-catalog.md %}).

## Codegen

For a binding `ProgressBar.Value ← Health (int) | IntToFloat | Clamp(0, 100) | Divide(100)`:

```razor
@code {
    public int Health { get; set; }
    private float _ProgressBarValue =>
        global::SboxUiDesigner.Runtime.SuiBuiltinConverters.Divide(
            global::SboxUiDesigner.Runtime.SuiBuiltinConverters.Clamp(
                global::SboxUiDesigner.Runtime.SuiBuiltinConverters.IntToFloat( Health ),
                0f,
                100f ),
            100f );
}
```

Pure static calls, no runtime indirection. The generator inlines the chain.

## The chain feed

Each step has args. The chain feed (value from the previous step) plugs into a specific arg position — default 0, but you can move it via the **chain reposition** UI (the `Args[0]` button in the chain editor is clickable to move the feed elsewhere). Useful for `Subtract(N, chain)` or `Divide(chain, N)` style flows.

Other args can be:

- **Variables** — referenced by GUID. Tinted in the picker (green / yellow / red) by type compatibility.
- **Literals** (V1.5 D-026) — typed in the literal input dialog (string / int / float / bool / Color / Vector).

## `Compose` vs `Format`

Two string-builder converters with different ergonomics:

### `Compose(params object[] parts) → string`

V1.5 D-027 — "easy mode" string composition. Parts are ordered, no placeholder syntax:

```
Compose( "HP: ", Health, " / ", MaxHealth )
   → "HP: 75 / 100"
```

The Bind popup detects `ConverterRef == "builtin.Compose"` and renders a single **+** button that opens a menu (Text / Variable) instead of the generic `+ Add Arg` Variable picker; the Text path auto-opens the literal editor so the user starts typing immediately. The chain feed is **not** consumed automatically — Compose is a pure composer.

### `Format(string template, params object[] args) → string`

`string.Format` semantics — `{0}`, `{1}`, etc. Use when you need format strings or culture-specific number formatting:

```
Format( "{0:F1} HP / {1}", CurrentHp, MaxHp )
   → "75.0 HP / 100"
```

Compose is the default recommendation for "literal + variable concatenation"; Format is for power users.

## Validation

V1.5 D-026 hardened the chain validator. The compiler now checks:

- **Cross-step type compatibility** — `meta_N.ReturnType` must satisfy `meta_{N+1}.Inputs[0].Type`. Errors surface in Compile Results as `step #N (X) expects input type Y, but step #N-1 (Z) returns W`.
- **Unknown converter ref** — `SuiConverterCatalog.Find(step.ConverterRef) != null` — catches the "user deleted a custom converter" case that previously fell through to a silent `default`.
- **`Parse()` uses `CultureInfo.InvariantCulture`** — `"1.5"` always parses regardless of OS locale (`pt-BR` / `de-DE` users used to get silent zero returns).
- **`MakeColor()` clamps** each channel to `[0, 1]`.

## Custom converters via `[SuiConverter]`

Drop a `public static` method tagged with `[SuiConverter]` anywhere in your code:

```csharp
using SboxUiDesigner.Runtime;

public static class MyConverters
{
    [SuiConverter( "RemainingTime", Category = "Game",
        Description = "Format seconds as mm:ss" )]
    public static string RemainingTime( float seconds )
        => TimeSpan.FromSeconds( seconds ).ToString( @"mm\:ss" );

    [SuiConverter( "TeamColor", Category = "Game",
        Description = "Index → predefined team color" )]
    public static Color TeamColor( int teamIndex )
        => teamIndex switch
        {
            0 => Color.Red,
            1 => Color.Blue,
            _ => Color.White,
        };
}
```

The Designer picks them up via reflection — they appear in the Bind popup alongside builtins (V1.5 D-026 polish — the suggester now includes custom converters too).

The Designer also has a **+ New custom converter** dialog that scaffolds the method into `Code/GameConverters.cs` for you (with default-parameter support — checkbox + literal per arg row).

## Variadic converters

`params object[]` parameters are supported (V1.5 D-026 codegen). The wrapper's emit ensures all the variadic values reach the call site — C# auto-wraps the tail. This is what makes `Format` and `Compose` work.

## TwoWay + converters — the auto-switch

Converters can't round-trip — `Map(0, 100, 0, 1)` has no automatic inverse. When you add a converter to a `TwoWay` binding, the Designer pops a `SuiConfirmDialog` warning that the binding will switch to `OneWay` on OK. Cancel to keep TwoWay (without the converter).

## Catalog identity

Every builtin's identity is `builtin.<Name>` (e.g. `builtin.Clamp`). User converters' identity is `user.<MethodName>` by default.

Identities are **forward-compatible** — once shipped, a builtin never has its name changed. Retired builtins keep the old name (deprecated) and gain a new name for the replacement. This protects existing `.sui` bindings from silently breaking on engine updates (PRD 17 § 2.3 invariant C3).

## Doo-authored custom converters (deferred)

M3 D-017 retargeted SUI Designer's visual-scripting integration from ActionGraph to **Doo**. For custom converters specifically, the Doo authoring path was deferred — M3 focused on Doo-backed [events]({% link concepts/events-and-actions.md %}) (handlers + bodies). V1.5 ships C# `[SuiConverter]`-tagged methods only as the supported custom-converter authoring surface.

Doo-based custom converters land in a future milestone alongside additional Doo integration polish. Until then, if you need a non-builtin transform, use the [`[SuiConverter]`](#custom-converters-via-suiconverter) attribute (or the **+ New custom converter** dialog) to scaffold a static method — those compose with builtins exactly the same way.

## See also

- [Converters catalog]({% link reference/converters-catalog.md %}) — every builtin signature
- [Bindings]({% link concepts/bindings.md %}) — where converters live
- [Working with converters workflow]({% link workflows/working-with-converters.md %}) — Compose vs Format vs custom
- [Variables]({% link concepts/variables.md %}) — the inputs converters consume
