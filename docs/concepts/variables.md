---
layout: default
title: Variables
parent: Concepts
nav_order: 6
---

# Variables
{: .no_toc }

Typed, named UI-local state declared on a `.sui` document. Variables are the bridge between your gameplay code and the visual properties of your UI.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What a Variable is

A `SuiVariable` is one slot of typed state on a `.sui` document. Each Variable has:

- A stable **GUID** (`var_XXXXXXXX`) that survives renames — bindings reference Variables by GUID, not by display name.
- A **Name** — a valid C# identifier, displayed in the Variables tab + used as the wrapper field name.
- A **Type** — from the closed set documented below.
- A **Default** — a JSON-shaped value matching the Type.
- An **IsPublic** flag — when true, the Variable is parent-settable (when this `.sui` is embedded as a `SuiReference`) AND reachable from gameplay code as `Parent.ChildName.VarName`.

The generator emits one `[Property]` on the wrapper class per Variable. Gameplay code touches the wrapper field; the wrapper syncs to the View; the View re-renders.

## Why Variables exist

Two big jobs:

1. **State you want to drive from code** — `Health`, `PlayerName`, `IsPaused`. Bind element properties to the Variable so changing the wrapper field updates the UI.
2. **Per-instance parameters when this `.sui` is embedded as a sub-UI** — flag the Variable as `IsPublic` and the parent can override its default when dropping the embed onto its canvas.

Without Variables, the only way to drive a UI from code is hand-writing a `.partial.cs` that overrides properties imperatively. With Variables, you author the wiring visually + the codegen does the property-mirror plumbing for you.

## Declaring a Variable

In the Designer:

1. Click the **Variables** tab in the bottom panel.
2. Click **+ Add Variable**.
3. The dialog asks for: **Name**, **Type**, **Default** (typed editor matching the type), **IsPublic**, **Group**, **Description**.
4. Click OK.

The new Variable appears in the tab. Bindings can target it immediately. Save the `.sui` and the next compile adds the `[Property]` to the wrapper.

## Variable types

The closed set (PRD 18 § 3.3):

### Primitives

- `string`, `int`, `long`, `float`, `bool`

### Engine types

- `Color` — RGBA, hex / rgb / rgba string default
- `Vector2`, `Vector3`, `Vector4`
- `Angles`, `Rotation`, `Transform`

### Asset refs

- `Texture` — resolves to a texture asset path
- `Resource` — generic; **set `ResourceType` to the concrete type name** (e.g. `ModelResource`)

### Generic

- `Enum:<full.type.name>` — any C# enum reachable in the project
- `Component:<full.type.name>` — a Component reference (e.g. for cross-Component data wiring)

### Collections

- `List<T>` where `T` is any of the above (primitives, engine types, enums, structs, classes)

The Variables dialog renders a typed editor matching the Type — Color spawns the color picker, `List<T>` spawns a list editor, enums spawn a dropdown.

## The `IsPublic` flag (V1.5-M2-K)

Default: `false` — the Variable is **internal** to the document (lives on the wrapper, drives bindings, but doesn't surface to parents).

When you flag it `true`:

1. It appears in the **parent's Sub-UI Props editor** when this `.sui` is embedded via `SuiReference`. The parent sets it per-instance — overriding the Default for that embed.
2. It becomes reachable from gameplay code as `Parent.ChildName.VarName`:

   ```csharp
   Hud.StaminaBar.ActualValue = 75;   // StaminaBar is a SuiReference on Hud's canvas
   ```

`IsPublic` is the V1.5-M2-K replacement for the deprecated AcceptedProp concept (see DEVIATIONS D-005). One concept, flag-based exposure.

## How Variables generate

For a Variable `Health: int` with `Default = 100`, the wrapper emits:

```csharp
[Property, Group("Internal")]
public int Health { get; set; } = 100;
```

If `IsPublic = true`, the group is `"Public"` instead. The renderer (`<Name>Panel`) gets a matching field set by `SyncFieldsTo`.

For `List<T>` Variables driving a ForEach embed:

```csharp
[Property] public List<ChatMessage> Messages { get; set; } = new();
```

The wrapper exposes `Messages.Add(...)` directly — there's no further wiring to do.

## Group + Description + IsAdvanced

Three optional fields for organizing the Variables tab + the inspector:

- **Group** — string label that the wrapper emits as `[Group("MyGroup")]`. The Variables tab also groups by this string. Useful for organizing 10+ Variables into thematic clusters ("Stats", "Combat", "Settings").
- **Description** — a `///` summary on the generated property + tooltip in the Variables tab.
- **IsAdvanced** — collapses the Variable under an "Advanced" section in the Variables tab.

## How bindings reference Variables

A `SuiBinding` stores `Source.VariableId` (the GUID) — not the display name. Renaming a Variable updates every binding instantly (the chain reference is still valid).

If you delete a Variable, every binding that referenced it shows a **red ⚠ icon** (broken binding, DEVIATIONS D-026). The Compile Results panel surfaces them before generation runs so you never ship a `default`-emitting silent failure.

## Source kinds (Manual / FromComponent / FromActionGraph)

Every Variable carries a `Source.Kind` field controlling where its runtime value comes from. Three values declared in `SuiVariableSourceKind` (closed enum, additions require a schema migration):

| Kind | Where the value comes from | V1.5 status |
|---|---|---|
| **`Manual`** | Gameplay code writes the wrapper's generated `[Property]` directly | Default. Fully wired. |
| **`FromComponent`** | Pulled from a sibling Component property on every refresh | Schema present (`ComponentVariableId` + `PropertyPath`); codegen treats non-Manual Variables as **not auto-assigned** (`EmitVariableAssignments` skips them) — wiring is partial |
| **`FromActionGraph`** | Computed by a `.action` asset on every `BuildHash()` | Schema present (`ActionGraphAssetPath`); same partial wiring as FromComponent |

V1.5 default + fully-exercised path is `Manual`. The two pull-based kinds are intentionally shipped as data-model placeholders so future runs can drive Variables from external sources without breaking existing `.sui` files — but the Designer's wiring + emit for them remains polish work. If you set a Variable to FromComponent in the current build, the wrapper still exposes the `[Property]` mirror, but it won't auto-pull — you'll need to assign it manually each frame.

Source of truth: `Code/Runtime/SuiVariableSource.cs` + `Code/Generation/SuiWrapperEmitter.cs` `EmitVariableAssignments` (filters `Kind == Manual`).

## See also

- [Bindings]({% link concepts/bindings.md %}) — how to actually plug a Variable into an element property
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how Variables become `[Property]` mirrors
- [Composition]({% link concepts/composition.md %}) — `IsPublic` Variables in the parent's Sub-UI Props editor
- [Converters]({% link concepts/converters.md %}) — transform a Variable's value before it hits the property
- [Binding a Variable workflow]({% link workflows/binding-a-variable.md %}) — step-by-step bind dialog
