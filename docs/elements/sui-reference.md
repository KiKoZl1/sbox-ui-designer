---
layout: default
title: SuiReference
parent: Element reference
nav_order: 20
---

# SuiReference
{: .no_toc }

Embeds another `.sui` document by GUID. The composition element. Drop one onto the canvas and the designer paints the child's tree inside the reference's rectangle; the wrapper exposes a named C# field for it; ForEach lets you iterate over a `List<T>` Variable.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What it is

`SuiReference` is V1.5 M2 (PRD 19). It's the only "non-paint" element — instead of carrying its own visuals it points at another `.sui` document and the generator inlines the child wherever you place the reference.

Three ways to create one:

1. **USER WIDGETS palette** — every `.sui` in the project appears as its own palette item (DEVIATIONS D-002). Drag or click → SuiReference created with `SourceGuid` already bound.
2. **Generic SuiReference entry** — the COMPOSITION palette category has a generic SuiReference. Drop it → SuiReferencePicker opens → pick a target.
3. **Change source… affordance** — on an existing SuiReference, the Details panel has a button to swap source.

## Per-instance fields (Details panel)

| Field | Notes |
|---|---|
| **Source** | The target `.sui` (read-only display + Change source… button) |
| **Name** | C# identifier — becomes the field name on the parent wrapper. Must be unique within the document; renaming to a sibling-conflicting name auto-appends a numeric suffix (`StaminaBar` → `StaminaBar_2`) so the named-instance field rule never silently produces colliding C# fields. |
| **Props** | Per-instance overrides for the child's `IsPublic` Variables (typed editors matching each Variable's Type) |
| **ForEach** | Optional iteration mapping. See [ForEach](#foreach--dynamic-lists) |

The Layout / Style sections behave like any other element — the SuiReference rect is what the child renders inside.

## Recursive paint on the canvas

V1.5 M2-K0 (DEVIATIONS D-010) shipped the canvas recursive paint:

- Resolves the SuiReference via `SuiAssetRegistry`.
- Applies per-instance Props overrides.
- Paints the **child's actual element tree** inside the reference rect.
- Bounding-box-scaled — resizing the reference rescales children proportionally (UMG-like).
- **Purple dashed border** marks the SuiReference rect.
- Child's Canvas root frame suppressed via `SkipRootFrame` so its outline doesn't bleed past the reference bounds.

You see what your sub-UI will look like at design time — no need to Test in Play for visual feedback.

## Generated code

For a parent `Hud.sui` with a SuiReference named `StaminaBar` pointing at `progress_bar.sui` (which compiles to `Game.UI.ProgressBar`):

```csharp
public sealed class Hud : SuiPanel<HudPanel>
{
    [Property, Group("Children")]
    public global::Game.UI.ProgressBar StaminaBar { get; set; } = new();

    protected override void SyncFieldsTo( HudPanel view )
    {
        view.StaminaBar = StaminaBar;
        StaminaBar.MarkEmbedded();
    }

    public override int ContentHash()
        => base.ContentHash() ^ (StaminaBar?.ContentHash() ?? 0);
}
```

The renderer's Razor inlines the child:

```razor
@if ( StaminaBar == null || StaminaBar.IsShown )
{
    <ProgressBarPanel @ref="StaminaBar.View"
                      ActualValue=@StaminaBar.ActualValue
                      FillColor=@StaminaBar.FillColor />
}
```

The `@if` guard implements the embedded `Show()` / `Hide()` semantics — toggling visibility on an embedded wrapper just flips its `IsShown` flag; the parent's recursive `ContentHash` picks up the change and re-renders with the nested tag wrapped by the `@if`.

## From gameplay code

```csharp
[Property] public Game.UI.Hud Hud { get; set; } = new();

Hud.Show();
Hud.StaminaBar.ActualValue = 75;       // direct C# property access by name
Hud.StaminaBar.FillColor   = Color.Red;
Hud.StaminaBar.Hide();                 // embedded — just toggles visibility
```

## ForEach — dynamic lists

When the SuiReference has ForEach enabled, it iterates a `List<T>` Variable, instantiating one child per item. Schema (`Code/Runtime/SuiForEachData.cs`):

```jsonc
"ForEach": {
  "SourceVariableId": "var_messages",
  "ItemPropId":       null,
  "IndexPropId":      null
}
```

- `SourceVariableId` — GUID of a `List<T>` Variable on the parent document.
- `ItemPropId` / `IndexPropId` — reserved fields; not consumed by the V1.5 codegen.

**How children receive per-item data:** the Razor generator iterates `__item` and forwards every `IsPublic` Variable on the child by **name match** — `<ChildPanel VarName=@(__item?.VarName ?? default) ... />`. So the items in your `List<T>` need member names matching the child's public Variable names; codegen wires them up automatically. No explicit mapping table.

The wrapper field becomes a `List<TChild>`:

```csharp
[Property] public List<global::Game.UI.ChatLine> ChatMessages { get; set; } = new();
```

Code edits the list, the parent re-renders:

```csharp
Hud.ChatMessages.Add( new ChatLine { Text = "Hello", Color = Color.Green } );
Hud.ChatMessages[0] = new ChatLine { Text = "Updated!" };
```

The renderer emits a `foreach` over the list, with member-match assignments:

```razor
@foreach ( var __item in ChatMessages )
{
    @if ( __item == null || __item.IsShown )
    {
        <ChatLinePanel Text=@(__item?.Text ?? "") Color=@(__item?.Color ?? default(Color)) />
    }
}
```

(Child wrapper `ChatLine` exposes `Text` + `Color` as `IsPublic` Variables — codegen forwards them by name.)

### Member-name matching, not explicit mappings

V1.5 ships **member-name matching** — items in your bound `List<T>` need to expose fields/properties whose names match the child's `IsPublic` Variable names exactly. The Razor generator does `__item?.<VarName>` access; mismatched names produce `null` (defaults). PRD 19 § 5.6 originally sketched an explicit mapping table (Pattern B); the shipped V1.5 emit is the name-match model.

Works with primitive lists too (`List<string>`, `List<int>`) if the child has a single primary `IsPublic` Variable typed to match the list element. Path-binding inside the child (`@item.SubProp`) for nested property access is deferred to V1.6 (DEVIATIONS D-007).

### Individual children are NOT addressable

The list is the source of truth; children are derived views. `Hud.MessagesContainer[3].Text = "X"` doesn't work — same semantics as React `.map()` / Vue `v-for`.

## Reference cycles

`SuiReferenceCycleDetector` runs on save. Embedding a `.sui` that (transitively) embeds itself produces a Compile Results error with the cycle chain named. Documents are also filtered from their **own** USER WIDGETS listing.

## See also

- [Composition concept]({% link concepts/composition.md %}) — the full mental model
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — named-instance fields + `ContentHash`
- [Variables]({% link concepts/variables.md %}) — `IsPublic` for Props editor
- [Embedding sub-UIs workflow]({% link workflows/embedding-sub-uis.md %}) — step-by-step
