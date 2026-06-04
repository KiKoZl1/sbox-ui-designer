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

> **Localization (V1.5 posture).** V1.5 treats `string` Variables as **raw text** — no FText-equivalent, no `{key}` substitution, no automatic translation lookup at render time. For localized UIs the supported workaround is: wire a user-side `LocalizationService` Component that exposes translated strings as Component properties, then bind a `Component:`-typed Variable to it and reference `Service.Strings.MyKey` in your bindings. A first-class `LocalizedString` Variable type is on the roadmap for a future milestone if/when s&box matures its localization infra — not in V1.5.

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

### Authoring a Color Variable

When you pick **Type = Color** in the Add Variable dialog (or change an existing Variable's type to Color), the Default editor becomes a swatch button. Clicking it opens the **`SuiColorPickerPopup`** — the designer's own color picker, introduced to work around the engine `Editor.ColorPicker.OpenColorPopup` bugs (V1.0 ISSUE-001 → ISSUE-003: SV gradient not repainting on hue change, slider lag, intermittent commits, mis-positioned initial state).

The popup gives you:

- **SV square** — click/drag to set saturation + value
- **Hue slider** — vertical strip on the right
- **Alpha slider** — horizontal strip under the SV square
- **Hex input** — `#RRGGBB` or `#RRGGBBAA`
- **RGBA numeric inputs** — 0–255 per channel
- **Old / new swatches** — side-by-side compare; click Old to revert
- **Recent colors palette** — your last picks, click to reuse

Values round-trip losslessly: the picker stores state as `ColorHsv` internally so hex ↔ rgb ↔ rgba conversions don't drift between edits.

The **same picker** also appears in two other places, so the UX is consistent everywhere a Color is authored:

1. The **binding popup's literal editor** — when you bind a Color-typed property to a literal (no Variable), the literal editor is a swatch that opens `SuiColorPickerPopup`.
2. **Converter literal args** — when a converter takes a `Color` argument (e.g. tint converters), its arg editor uses the same picker.

Source of truth: `Editor/Widgets/SuiColorPickerPopup.cs`.

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

## Source kinds — V1.5 ships Manual only

Every Variable carries a `Source.Kind` field. The `SuiVariableSourceKind` enum is a closed set; V1.5 has **exactly one** member:

| Kind | Where the value comes from |
|---|---|
| **`Manual`** | Gameplay code writes the wrapper's generated `[Property]` directly |

Earlier alpha builds shipped two more kinds — `FromComponent` (pull from a sibling Component property) and `FromActionGraph` (compute via `.action` asset). Both were **ripped at M4 close per DEVIATIONS D-017**: Doo replaces ActionGraph as the visual scripting backend, and the Component source was never user-pickable from the dialog anyway. If you have old `.sui` files with the legacy kinds, Force Regen (see [Upgrade guide]({% link UPGRADE_V1_0_TO_V1_5.md %})) normalises them to Manual on first open.

Source of truth: `Code/Runtime/SuiVariableSource.cs`.

## See also

- [Bindings]({% link concepts/bindings.md %}) — how to actually plug a Variable into an element property
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how Variables become `[Property]` mirrors
- [Composition]({% link concepts/composition.md %}) — `IsPublic` Variables in the parent's Sub-UI Props editor
- [Converters]({% link concepts/converters.md %}) — transform a Variable's value before it hits the property
- [Binding a Variable workflow]({% link workflows/binding-a-variable.md %}) — step-by-step bind dialog
