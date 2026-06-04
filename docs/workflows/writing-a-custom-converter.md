---
layout: default
title: Writing a custom converter
parent: Workflows
nav_order: 11
---

# Writing a custom converter
{: .no_toc }

Author your own `[SuiConverter]`-tagged C# method and have it show up in the Bind popup alongside the builtins. The scaffolder takes care of the file + markers; you fill in the method body.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## When you actually need a custom converter

For most use cases the [builtin catalog]({% link reference/converters-catalog.md %}) (66 converters across Math / Range / Conversion / Logic / String / Color / Collection) chains together to give you what you want. Custom converters earn their keep when:

- The transform is game-specific (e.g. "Hp percentage with a HUD-relevant smoothing curve").
- You need access to engine APIs (`GameTime`, materials, components) that a chain of pure builtins can't reach.
- The chain to express the transform with builtins is getting absurd — 7 steps that one method would say in 3 lines.

Otherwise prefer chains — they survive renames + survive C# refactors + show their work in the Bind popup.

---

## The contract

A SUI converter is a `public static` method marked with `[SuiConverter]`:

```csharp
[SuiConverter( "MyName", Category = "User", Description = "What it does" )]
public static <ReturnType> MyName( <Input1> a, <Input2> b, ... )
{
    return ...;
}
```

The attribute lives in `SboxUiDesigner.Runtime`. The method must be `public static`. Parameters and return type must be **types the TypeRef schema knows** (primitives + engine types + `Color` + `Vector*` + a few specials — see `SuiTypeMapper.cs`). Everything else falls back to `object` and the Bind popup can't pick it.

The Bind popup picks up new converters via TypeLibrary reflection on the next compile — no manual registration.

---

## Option A — let the Designer scaffold it

The Designer ships a **Custom converter dialog** that authors the C# stub for you. The flow:

1. **Open** any `.sui` document.
2. **Bind popup** on any property → **Converters** → **+ Add step** → scroll to the bottom of the picker → **+ Custom**.
3. **Fill in** the dialog:
   - **Name** — must be a legal C# identifier (e.g. `HpTintColor`).
   - **Category** — defaults to `User`. Anything you put here groups your converters under that label in the Bind popup.
   - **Description** — short string shown in the picker.
   - **Return type** — pick from the TypeRef dropdown.
   - **Inputs** — `+ Add input`, name + type + optional default literal per row.
4. **OK** — the dialog calls `SuiUserConverterScaffolder.Scaffold(config)`.

What ships to disk:

- File created at `Code/GameConverters.cs` (first call only — subsequent calls extend the file).
- The new method is inserted between `// SUI:USER-CONVERTERS:BEGIN` and `// SUI:USER-CONVERTERS:END` markers.
- The method body is `return default;` — **you fill in the implementation**.

Example output for a `HpTintColor( int hp, Color full, Color low ) → Color`:

```csharp
// Code/GameConverters.cs (auto-generated scaffold)

using Sandbox;
using SboxUiDesigner.Runtime;

namespace Game;

public static class GameConverters
{
    // SUI:USER-CONVERTERS:BEGIN

    [SuiConverter( "HpTintColor", Category = "User", Description = "Red when hp < 30, otherwise green" )]
    public static Color HpTintColor( int hp, Color full, Color low )
    {
        // TODO: implement
        return default;
    }

    // SUI:USER-CONVERTERS:END
}
```

Replace `return default;` with your transform, save, recompile, refresh the Bind popup — the converter appears under **User → HpTintColor**.

---

## Option B — author by hand

If you'd rather skip the dialog:

1. Create `Code/GameConverters.cs` (or any `.cs` file under `Code/` — the SUI scanner reflects all loaded assemblies).
2. Write the static method with the `[SuiConverter]` attribute:

```csharp
using Sandbox;
using SboxUiDesigner.Runtime;

namespace Game;

public static class GameConverters
{
    [SuiConverter( "HpTintColor", Category = "User", Description = "Red when hp < 30, otherwise green" )]
    public static Color HpTintColor( int hp, Color full, Color low )
        => hp < 30 ? low : full;
}
```

3. Save + recompile. The Bind popup picks it up on the next dialog open.

The `Code/GameConverters.cs` filename is a convention — the scanner doesn't care where the file lives as long as the assembly loads.

---

## Identity scheme

Custom converters use the **`user.<Name>`** identity on disk. The Designer remembers the binding by this identity, so renaming a method requires a `.sui` re-bind. Built-ins use `builtin.<Name>` and are stable across releases.

If you ship a converter publicly and want forward-compat, **don't rename it** — keep the old name as a `[Obsolete]` wrapper that delegates to the new one. Same forward-compat policy the built-ins follow (see PRD 18 § 5.2 / C3 commitment).

---

## Variadic converters (`params object[]`)

The `LastParamIsVariadic` attribute flag opts the converter into the Bind popup's "+ Add Arg" UI:

```csharp
[SuiConverter( "Join", Category = "User",
    Description = "Concat parts with a separator",
    LastParamIsVariadic = true )]
public static string Join( string separator, params object[] parts )
    => parts == null ? "" : string.Join( separator ?? "", parts );
```

The Bind popup shows the first arg (`separator`) as a normal input, then a + Add Arg button to append values to the `params` array.

The two built-in variadics are `Format` (uses the standard "+ Add Arg" picker) and `Compose` (uses a single + menu with Text / Variable submenus — DEVIATIONS D-027).

---

## What types are accepted

The TypeRef schema accepts (see `Code/Generation/SuiTypeMapper.cs`):

- Primitives — `int`, `long`, `float`, `double`, `bool`, `string`.
- Engine — `Color`, `Vector2`, `Vector3`, `Vector4`, `Angles`, `Rotation`, `Transform`.
- Asset refs — `Texture`, `Resource` (concrete via `ResourceType`).
- Specials — `SoundEvent`, `Material`.

Everything else (custom structs, your own enums, unknown components) falls back to `object` in the Bind popup. The method still works at runtime; the popup just can't drive its args from typed Variables. For an enum your own code defines, use `Enum:<FullTypeName>` as the TypeRef.

Generic methods (`If<T>`, `Equal<T>` etc.) work fine for built-ins because the runtime resolves `T` at codegen. For user converters, stick to concrete types — the TypeRef inference for generic args isn't fully wired in V1.5.

---

## Use the converter from a binding

Once the Bind popup sees your converter:

1. **Bind dialog → Converters → + Add step** → pick **User → HpTintColor** (or whatever Category you used).
2. **Args** — each arg can be `Chain` (the feed from the previous step) / a Variable / a literal. Match your method signature.
3. **OK** + recompile. The generated Razor calls your method directly:

```razor
<div style="background-color: @(global::Game.GameConverters.HpTintColor( Hp, Color.Green, Color.Red ));">
```

No reflection at render time — the codegen emits a direct call.

---

## Common pitfalls

- **Method NOT static.** TypeLibrary reflection looks for static methods only. Instance methods don't show up in the picker.
- **Method in `Assets/`** instead of `Code/`. `Assets/` is for runtime resources and isn't compiled into the assembly the editor loads. Use `Code/`. The auto-migrate path in `SuiUserConverterScaffolder` moves legacy `Assets/GameConverters.cs` to `Code/GameConverters.cs` on the next scaffold.
- **No `[SuiConverter]` attribute.** The method exists but isn't picked up. Add the attribute (no args required — defaults are sensible).
- **Return type the schema doesn't know.** The Bind popup will allow OneTime / OneWay only and tint the binding yellow. Use a TypeRef-aware return type if you want full type-flow guarantees.
- **Generic args.** Stick to concrete types in V1.5 user converters. Generic resolution lives in the built-in path.

---

## See also

- [Converters concept]({% link concepts/converters.md %}) — the mental model
- [Converters catalog reference]({% link reference/converters-catalog.md %}) — every builtin with signature
- [Working with converters workflow]({% link workflows/working-with-converters.md %}) — using existing converters
- [`Code/Runtime/SuiConverterAttribute.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Runtime/SuiConverterAttribute.cs)
- [`Editor/SuiUserConverterScaffolder.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/SuiUserConverterScaffolder.cs)
