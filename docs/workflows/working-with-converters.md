---
layout: default
title: Working with converters
parent: Workflows
nav_order: 7
---

# Working with converters
{: .no_toc }

How to pick the right converter, build a chain, and write your own. Covers Compose vs Format vs custom `[SuiConverter]`.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## When you need a converter

Three common signals you need a converter in a chain:

1. **Type mismatch** — the target property wants `float` but the Variable is `int`. Fix: `IntToFloat`.
2. **Range mismatch** — `Health` is 0..100 but `ProgressBar.Value` is 0..1. Fix: `Divide(100)` or `Map(0, 100, 0, 1)`.
3. **String formatting** — bind a Text element's `Text` to "75 / 100 HP". Fix: `Compose("HP: ", Health, " / ", MaxHealth)`.

The Bind popup's type-tinted variable picker (D-026) tells you when a converter is needed — yellow = convertible, red = incompatible. Adding the right step turns yellow → green.

## Pattern: clamp + scale a stat

For a typical `Health: int (0..100)` → `ProgressBar.Value (0..1)`:

```
Health (int)
    │  IntToFloat()
    ▼
    │  Clamp( 0, 100 )
    ▼
    │  Divide( 100 )
    ▼
ProgressBar.Value (float, 0..1)
```

In the Bind popup:

1. Pick `Health` as source.
2. Add step `builtin.IntToFloat` — no extra args needed.
3. Add step `builtin.Clamp` — Args[0] is the chain feed, Args[1] = literal `0`, Args[2] = literal `100`.
4. Add step `builtin.Divide` — Args[0] = chain feed, Args[1] = literal `100`.

Result: the chain feeds left-to-right, every step has its args typed in.

## Pattern: format a label

For a Text element bound to "HP: 75 / 100":

```
Health (int)            <-- chain source
   │
   ▼  Compose( "HP: ", chain, " / ", MaxHealth )
   ▼
Text.Text (string)
```

In the Bind popup:

1. Pick `Health` as source.
2. Add step `builtin.Compose` — the popup shows a single **+** button instead of `+ Add Arg`.
3. Click **+** → menu opens — pick **Text** → literal editor opens with the cursor in the text field. Type `"HP: "` + OK.
4. Click **+** → menu opens — pick **Variable** → use the chain feed.
5. Click **+** → menu opens — pick **Text** → type `" / "` + OK.
6. Click **+** → menu opens — pick **Variable** → pick `MaxHealth`.

Result: `Compose("HP: ", chain, " / ", MaxHealth)`.

Codegen — the binding is inlined directly into the `<label>` body as a Razor expression:

```razor
<label>@(global::SboxUiDesigner.Runtime.SuiBuiltinConverters.Compose( "HP: ", Health, " / ", MaxHealth ))</label>
```

## Pattern: derive a color from a state

For an Image's `Tint` bound to player team (0 = red, 1 = blue, otherwise white):

Option A — `If` chain (works for 2 branches):

```
TeamIndex (int)
   │  Equal( chain, 0 )                   → bool
   ▼  If( chain, redColor, blueColor )    → Color
   ▼
Image.Tint
```

Option B — custom converter (cleanest for ≥3 branches):

```csharp
[SuiConverter( "TeamColor", Category = "Game" )]
public static Color TeamColor( int teamIndex )
    => teamIndex switch
    {
        0 => Color.Red,
        1 => Color.Blue,
        _ => Color.White,
    };
```

Now in the Bind popup, pick `TeamColor` directly as the only chain step.

## Compose vs Format vs Concat — which to use

| Need | Use |
|---|---|
| `"Prefix: " + var + " / " + var2` | **`Compose`** — easy mode, no placeholder syntax |
| `"75.0 HP / 100"` with culture-aware number formatting | **`Format`** with `"{0:F1} HP / {1}"` |
| Quick two-string concat | **`Concat(a, b)`** — two-arg shortcut |

Default recommendation: **Compose** for "literal + variable concatenation". Format for power users.

## Writing a custom converter

### Quickest path — scaffolder

1. Bottom panel → **Bindings** tab → **New custom converter…**.
2. Dialog asks for: Name, Category, Return type, Parameter rows (each with Name, Type, Default checkbox + literal).
3. Click OK. The scaffolder emits the method into `Code/GameConverters.cs`:

```csharp
[SuiConverter( "RemainingTime", Category = "Game" )]
public static string RemainingTime( float seconds = 0 )
{
    // TODO: implement
    return default;
}
```

Open the file and fill in the body.

### Hand-write — the rules

Three constraints:

- **Pure** — no side effects (the generator inlines the call into the chain).
- **`public static`** — anywhere in the project's code.
- **Tagged with `[SuiConverter("DisplayName", Category = "...")]`** — the attribute drives the catalog name + category.

```csharp
using SboxUiDesigner.Runtime;

public static class MyConverters
{
    [SuiConverter( "FormatGoldAmount", Category = "Game",
        Description = "Show gold with thousand separators" )]
    public static string FormatGoldAmount( int gold )
        => gold.ToString( "N0" );
}
```

The Designer picks it up automatically via reflection. The Bind popup's suggester (typeahead) includes user converters (D-026 polish — was builtins-only before).

### Variadic parameters

`params object[]` is supported (D-026 codegen). Useful when the converter takes a variable number of inputs (e.g. a "pick first non-null" converter).

```csharp
[SuiConverter( "Coalesce", Category = "Logic", LastParamIsVariadic = true )]
public static T Coalesce<T>( params T[] values )
    where T : class
{
    foreach ( var v in values )
        if ( v != null ) return v;
    return null;
}
```

Flag `LastParamIsVariadic = true` on the attribute so the Bind popup renders the right arg-add affordance.

### Default parameter values

Add `= defaultValue` to a parameter and the scaffolder offers a checkbox in the converter dialog so the user can skip it (D-026 — default params support):

```csharp
[SuiConverter( "FloatToString", Category = "Conversion" )]
public static string FloatToString( float v, int decimals = 2 )
    => v.ToString( "F" + decimals );
```

In the Bind popup, the `decimals` arg row gets a "Default: 2" tag and a Use Default checkbox.

## Validation

V1.5 D-026 hardened the chain validator. Errors that the Compile Results panel will surface:

- **`step #N (X) expects input type Y, but step #N-1 (Z) returns W`** — type mismatch.
- **`unknown converter ref: <name>`** — the user deleted a custom converter or typo.
- Builtin signatures (Parse uses InvariantCulture; MakeColor clamps each channel; etc.) — see [Converters concept]({% link concepts/converters.md %}#validation).

## See also

- [Converters concept]({% link concepts/converters.md %}) — the mental model
- [Converters catalog]({% link reference/converters-catalog.md %}) — all 66 builtin signatures
- [Bindings]({% link concepts/bindings.md %})
- [Binding a Variable workflow]({% link workflows/binding-a-variable.md %})
- [Health HUD with converters tutorial]({% link tutorials/health-hud-with-converters.md %})
