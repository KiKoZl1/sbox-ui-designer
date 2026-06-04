---
layout: default
title: Composition / Sub-UIs
parent: Concepts
nav_order: 9
---

# Composition / Sub-UIs
{: .no_toc }

One `.sui` can embed another. `SuiReference` is the composition element — point it at another `.sui` by GUID and the designer paints the child's tree inside the reference's rectangle, the wrapper exposes a named C# field for it, and ForEach lets you iterate over a List Variable.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Why composition

Three motivations:

1. **Reuse** — design a `health_bar.sui` once, drop it into every HUD that needs an HP bar.
2. **Smaller files** — a 200-element HUD splits into 8 smaller `.sui` docs that are each easier to navigate.
3. **Dynamic lists** — a chat panel binds `Messages: List<ChatMessage>` and a `SuiReference` with ForEach renders one `ChatLine.sui` per item.

UMG users will recognise the pattern — this is the same shape as UMG's "User Widget" + "Widget Switcher with bound list".

## The `SuiReference` element

Drop one onto the canvas in three ways:

1. From the **USER WIDGETS** dynamic palette category (one entry per `.sui` in the project — drag or click).
2. From the generic **COMPOSITION → SuiReference** palette entry (then pick the target via the picker).
3. The Details panel's **Change source…** affordance on an existing SuiReference (rebind to a different `.sui`).

The element holds a `SourceGuid` pointing at the target `.sui`. The target is resolved via `SuiAssetRegistry` — GUID-based, so renaming or moving the target doesn't break the embed.

## Recursive paint on the canvas

V1.5 M2-K0 (DEVIATIONS D-010) shipped recursive canvas paint: the canvas resolves the SuiReference, applies any per-instance Props overrides, and renders the **child's actual element tree** inside the reference's rect. Bounding-box-scaled so resizing the reference rescales children proportionally (UMG-like). A **purple dashed border** marks SuiReference rects so you know which boxes are sub-UIs.

The child's Canvas root frame is suppressed via the `SkipRootFrame` flag so its outline doesn't bleed past the reference bounds.

## Generated code

For a parent `Hud.sui` with a SuiReference named `StaminaBar` pointing at `progress_bar.sui` (which compiles to `Game.UI.ProgressBar`):

```csharp
public sealed class Hud : SuiPanel<HudPanel>
{
    // Named-instance field — same name as the SuiReference in the Hierarchy
    [Property, Group("Children")]
    public global::Game.UI.ProgressBar StaminaBar { get; set; } = new();

    protected override void SyncFieldsTo( HudPanel view )
    {
        view.StaminaBar = StaminaBar;
        StaminaBar.MarkEmbedded();   // tells the child wrapper "your mount is owned by the parent"
    }
}
```

The renderer's Razor inlines the child's tag with the named ref:

```razor
@if ( StaminaBar == null || StaminaBar.IsShown )
{
    <ProgressBarPanel @ref="StaminaBar.View" ActualValue=@StaminaBar.ActualValue />
}
```

From gameplay code:

```csharp
Hud.StaminaBar.ActualValue = 75;   // direct C# property access by name
```

No PropId routing, no string keys — UMG-style direct field access.

## Embedded vs standalone semantics

A wrapper can be **standalone** (your code's `[Property]` field, mounted via `Hud.Show()`) or **embedded** (claimed as a child by another wrapper via `SyncFieldsTo` + `MarkEmbedded()`).

| Operation | Standalone | Embedded |
|---|---|---|
| `Add()` | Mounts ScreenPanel + Panel | No-op |
| `Show()` | Auto-mounts if needed + `View.Style.Display = Flex` | Just flips `IsShown` flag; parent re-renders |
| `Hide()` | `View.Style.Display = None` | Just flips `IsShown` flag |
| `Remove()` | Destroys the mount | (caller error — never call Remove on an embedded wrapper) |
| `ContentHash()` | Hashes own state | Propagates through the parent's recursive hash |

The wrapper tracks the `IsEmbedded` flag (set by parent's `SyncFieldsTo` → `MarkEmbedded()`). Calling `Show()` on an embedded wrapper that wasn't claimed would spawn a phantom standalone mount (V1.5-M2-K7-bugfix — the original "TestInnerHide press Slot3 spawned a duplicate HudBindtest" bug).

## Recursive `ContentHash` (DEVIATIONS D-015)

Every wrapper's `ContentHash()` aggregates:

1. `IsShown` (visibility toggles propagate)
2. Each own Manual Variable
3. `<ChildField>?.ContentHash() ?? 0` per single-instance SuiReference
4. `<ForEachField>?.Count ?? 0` plus `__item?.ContentHash() ?? 0` per ForEach iteration

The renderer's `BuildHash()` mirrors this set. Recursion has no opt-out. Without recursive aggregation, a mutation at the leaf (`grand.parent.hud.Health -= 10`) wouldn't change the middle's hash, wouldn't change the grandparent's hash, wouldn't re-render — the flat per-Variable hash shipped first and broke depth 2+.

## ForEach — dynamic lists

A SuiReference with ForEach enabled iterates a `List<T>` Variable, instantiating one child per item:

```jsonc
"ForEach": {
  "SourceVariableId": "var_messages",   // List<ChatMessage> on this doc
  "ItemPropId":       null,             // reserved — not consumed by V1.5 codegen
  "IndexPropId":      null              // reserved
}
```

The wrapper field becomes a `List<ChatLine>`:

```csharp
[Property] public List<global::Game.UI.ChatLine> ChatMessages { get; set; } = new();
```

Code edits the list, the parent re-renders:

```csharp
Hud.ChatMessages.Add( new ChatLine { Text = "Hello", Color = Color.Green } );
Hud.ChatMessages[0] = new ChatLine { Text = "Updated!" };
```

### Member-name matching (V1.5)

ForEach in V1.5 uses **member-name matching**, not explicit mappings — items in the bound `List<T>` need member names that match the child's `IsPublic` Variable names exactly. The Razor generator iterates `__item` and emits `<ChildPanel VarName=@(__item?.VarName ?? default) ... />` for every `IsPublic` Variable on the child. No mapping table to maintain.

PRD 19 § 5.6 originally sketched an explicit mapping table (Pattern B); the shipped V1.5 emit collapsed it to direct member access.

Path-binding inside the child (`@item.SubProp`) for nested property access is deferred to V1.6 (DEVIATIONS D-007).

ForEach also works with **primitive lists** (`List<string>`, `List<int>`) when the child has a single primary `IsPublic` Variable typed to match the element type.

### Individual mounted-child instances are NOT addressable

The list is the source of truth; children are derived views. You can't write `Hud.MessagesContainer[3].Text = "X"` — same semantics as React `.map()` / Vue `v-for`. Edit the list, the children rebuild.

## `IsPublic` Variables as per-instance Props

When a child's Variable is flagged `IsPublic`, the parent's Details panel for that SuiReference grows a **Props editor**:

```
SuiReference: StaminaBar
  Source: progress_bar.sui
  Props:
    MaxValue: 100         ← parent override (was 50 by default)
    FillColor: #ef4444    ← parent override (was #4ade80 by default)
    Direction: LeftToRight
```

The parent's per-instance overrides ship into the wrapper's `SyncFieldsTo` so each embed instance can have different defaults. Combined with gameplay code, you get `Hud.StaminaBar.ActualValue = 75` — overrides + runtime mutation work side by side.

## Cycle detection

`SuiReferenceCycleDetector` runs on save. Embedding a `.sui` that (transitively) embeds itself produces a Compile Results error with the cycle chain named. Documents are also filtered from their **own** USER WIDGETS palette listing to prevent the obvious one-step cycle without a modal warning.

## See also

- [SuiReference element]({% link elements/sui-reference.md %}) — the per-element page
- [Variables]({% link concepts/variables.md %}) — `IsPublic` and what it does
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — named-instance fields + `ContentHash`
- [Embedding sub-UIs workflow]({% link workflows/embedding-sub-uis.md %}) — step-by-step
