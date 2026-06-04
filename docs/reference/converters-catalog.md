---
layout: default
title: Converters catalog
parent: Reference
nav_order: 6
---

# Converters catalog
{: .no_toc }

Every builtin converter (64 total) with signature, category, description, defaults. Source: `Code/Runtime/SuiBuiltinConverters.cs`.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Identity scheme

Builtin identity is `builtin.<Name>` (e.g. `builtin.Clamp`). The names are **forward-compatible** — once shipped, no builtin has its name changed. Retired builtins keep the old name (deprecated) and gain a new one for the replacement. This protects existing `.sui` bindings from silently breaking on engine updates.

User-declared converters via `[SuiConverter]` use `user.<Name>` by default (the name passed to the attribute).

## Math (10)

| Name | Signature | Description |
|---|---|---|
| `Add` | `Add(float a, float b) → float` | `a + b` |
| `Subtract` | `Subtract(float a, float b) → float` | `a - b` |
| `Multiply` | `Multiply(float a, float b) → float` | `a * b` |
| `Divide` | `Divide(float numerator, float denominator) → float` | `numerator / denominator` (0 if denominator is 0) |
| `Modulo` | `Modulo(float a, float b) → float` | `a % b` (0 if b is 0) |
| `Power` | `Power(float baseV, float exp) → float` | `baseV ^ exp` |
| `Absolute` | `Absolute(float v) → float` | `|v|` |
| `Negate` | `Negate(float v) → float` | `-v` |
| `Min` | `Min(float a, float b) → float` | smaller of a, b |
| `Max` | `Max(float a, float b) → float` | larger of a, b |

## Range / Interpolation (9)

| Name | Signature | Description |
|---|---|---|
| `Clamp` | `Clamp(float v, float min, float max) → float` | Clamp v between min and max |
| `Clamp01` | `Clamp01(float v) → float` | Clamp v between 0 and 1 |
| `Map` | `Map(float v, float fromMin, float fromMax, float toMin, float toMax) → float` | Remap v from one range to another. Returns `toMin` when `fromMin == fromMax` |
| `Lerp` | `Lerp(float a, float b, float t) → float` | Linear interpolation a→b by t |
| `InverseLerp` | `InverseLerp(float a, float b, float v) → float` | Where v falls in the a→b range (0..1) |
| `SmoothStep` | `SmoothStep(float a, float b, float t) → float` | Smooth (eased) interpolation a→b by t |
| `Round` | `Round(float v) → int` | Round to nearest |
| `Floor` | `Floor(float v) → int` | Round down |
| `Ceil` | `Ceil(float v) → int` | Round up |

## Conversion (6)

| Name | Signature | Description |
|---|---|---|
| `IntToFloat` | `IntToFloat(int v) → float` | int → float |
| `FloatToInt` | `FloatToInt(float v, FloatToIntMode mode = Round) → int` | float → int with mode: `Round` / `Floor` / `Ceil` / `Truncate` |
| `LongToInt` | `LongToInt(long v) → int` | long → int |
| `IntToString` | `IntToString(int v) → string` | int → string |
| `FloatToString` | `FloatToString(float v, int decimals = 2) → string` | float → string with fixed decimals |
| `Parse` | `Parse(string v) → float` | string → float (0 if unparseable). Uses InvariantCulture (D-026) so '.' is always the decimal separator |

## Logic (10)

| Name | Signature | Description |
|---|---|---|
| `Equal<T>` | `Equal(T a, T b) → bool` | `a == b` (`EqualityComparer<T>.Default`) |
| `NotEqual<T>` | `NotEqual(T a, T b) → bool` | `a != b` |
| `GreaterThan` | `GreaterThan(float a, float b) → bool` | `a > b` |
| `LessThan` | `LessThan(float a, float b) → bool` | `a < b` |
| `GreaterOrEqual` | `GreaterOrEqual(float a, float b) → bool` | `a >= b` |
| `LessOrEqual` | `LessOrEqual(float a, float b) → bool` | `a <= b` |
| `If<T>` | `If(bool cond, T ifTrue, T ifFalse) → T` | Ternary |
| `And` | `And(bool a, bool b) → bool` | `a && b` |
| `Or` | `Or(bool a, bool b) → bool` | `a || b` |
| `Not` | `Not(bool v) → bool` | `!v` |

## String (14)

| Name | Signature | Description |
|---|---|---|
| `Concat` | `Concat(string a, string b) → string` | `a + b` |
| `Format` | `Format(string template, params object[] args) → string` | `string.Format(template, args)` — variadic |
| **`Compose`** (V1.5 D-027) | `Compose(params object[] parts) → string` | Build from ordered parts — no `{N}` placeholders |
| `Uppercase` | `Uppercase(string v) → string` | Invariant culture upper |
| `Lowercase` | `Lowercase(string v) → string` | Invariant culture lower |
| `TitleCase` | `TitleCase(string v) → string` | Title Case Each Word |
| `Length` | `Length(string v) → int` | Char count |
| `Substring` | `Substring(string v, int start, int length) → string` | Slice — safe for OOB args |
| `Replace` | `Replace(string v, string oldStr, string newStr) → string` | Replace every occurrence |
| `Contains` | `Contains(string haystack, string needle) → bool` | substring check |
| `StartsWith` | `StartsWith(string s, string prefix) → bool` | prefix check |
| `EndsWith` | `EndsWith(string s, string suffix) → bool` | suffix check |
| `Trim` | `Trim(string s) → string` | Strip leading + trailing whitespace |
| `IndexOf` | `IndexOf(string s, string needle) → int` | -1 if not found |
| `Split` | `Split(string s, string delimiter) → string[]` | Split on every delimiter occurrence |

## Color (10)

| Name | Signature | Description |
|---|---|---|
| `MakeColor` | `MakeColor(float r, float g, float b, float a) → Color` | Build Color (channels clamped to 0..1 per D-026) |
| `ColorFromHex` | `ColorFromHex(string hex) → Color` | Parse hex string |
| `ColorLerp` | `ColorLerp(Color a, Color b, float t) → Color` | Lerp between two colors |
| `WithAlpha` | `WithAlpha(Color c, float a) → Color` | Replace alpha |
| `Darken` | `Darken(Color c, float amount) → Color` | Darken c by amount (0..1) |
| `Lighten` | `Lighten(Color c, float amount) → Color` | Lighten c toward white by amount (0..1) |
| `ColorMultiply` | `ColorMultiply(Color c, float scalar) → Color` | Multiply RGB by scalar (alpha preserved) |
| `GetAlpha` | `GetAlpha(Color c) → float` | Return alpha channel |
| `Invert` | `Invert(Color c) → Color` | Invert RGB (alpha preserved) |
| `Grayscale` | `Grayscale(Color c) → Color` | NTSC luminance (0.299R + 0.587G + 0.114B) |

## Collection (6)

| Name | Signature | Description |
|---|---|---|
| `Count<T>` | `Count(List<T> list) → int` | Item count (0 if null) |
| `IsEmpty<T>` | `IsEmpty(List<T> list) → bool` | True if null or empty |
| `First<T>` | `First(List<T> list, T fallback) → T` | First item or fallback |
| `Last<T>` | `Last(List<T> list, T fallback) → T` | Last item or fallback |
| `ElementAt<T>` | `ElementAt(List<T> list, int index, T fallback) → T` | Item at index or fallback (OOB-safe) |
| `Slice<T>` | `Slice(List<T> list, int start, int count) → List<T>` | Sub-list (OOB-safe) |

## Variadic notes

`Format` and `Compose` use `params object[]` for the tail args. The codegen supports variadic via `LastParamIsVariadic = true` on the `[SuiConverter]` attribute. The Bind popup renders a custom arg-add affordance for these converters — Format keeps the standard "+ Add Arg" picker; Compose gets a single **+** menu (Text / Variable).

## Default parameter values

Several builtins use default parameters:

- `FloatToInt(v, mode = Round)` — `mode` defaults to `Round`.
- `FloatToString(v, decimals = 2)` — `decimals` defaults to 2.

The Bind popup renders these args with a "Default" tag + "Use Default" checkbox so the user can skip them.

## See also

- [Converters concept]({% link concepts/converters.md %}) — the mental model
- [Working with converters workflow]({% link workflows/working-with-converters.md %})
- [Bindings]({% link concepts/bindings.md %})
- [`Code/Runtime/SuiBuiltinConverters.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Runtime/SuiBuiltinConverters.cs) — source of truth
