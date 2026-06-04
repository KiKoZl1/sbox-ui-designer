---
layout: default
title: Reactivity & BuildHash
parent: Concepts
nav_order: 14
---

# Reactivity & `BuildHash`
{: .no_toc }

How a SUI document decides when to re-render — and why mutating `Hud.Health = 50` repaints the bar without any callbacks. The full reactivity contract in one page.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The contract in one sentence

The Razor runtime re-runs `BuildRenderTree` whenever `BuildHash()` returns a different value than the previous tick — and SUI generates a `BuildHash()` override that combines every Variable referenced by any non-`OneTime` binding (plus every embedded child's `ContentHash`).

Mutate a wrapper Variable, the View's `BuildHash` changes, the renderer re-runs. Mutate a wrapper field that's NOT in any binding, the hash doesn't change — no re-render. Cheap to use; predictable.

---

## Two related hashes

There are two hash methods in the SUI runtime. They serve different jobs:

| Method | Where it lives | Purpose |
|---|---|---|
| **`BuildHash()`** | The renderer Panel (`<Name>Panel`) | The engine's standard hook — triggers Razor re-render when changed |
| **`ContentHash()`** | The wrapper class (`<Name> : SuiPanel<TView>`) | The recursive aggregate — used by the **parent's** `BuildHash` so nested mutations bubble up |

`BuildHash` is the engine contract. `ContentHash` is the SUI plumbing that makes depth-N composition reactive.

---

## How `BuildHash` is emitted

Source: `Code/Generation/SuiBuildHashEmitter.cs`. For each `.sui`, the generator walks every element's `Bindings` list and collects:

1. The Variable referenced by `binding.Source.VariableId`.
2. Every converter argument that's a Variable (chain feed + literal args mixed with `Args[i].Kind == Variable`).
3. Every embedded child wrapper's `ContentHash()` (forwarded as a Razor-evaluable expression).

`OneTime` bindings are **excluded** — by design, they only read once and never refresh. Variables not touched by any reactive binding are excluded too — they don't affect the rendered tree, so they don't need to invalidate it.

The emit shape:

```csharp
protected override int BuildHash()
{
    var h = new global::System.HashCode();
    h.Add( Health );                  // OneWay binding referenced this Variable
    h.Add( MaxHealth );                // converter arg referenced this Variable
    h.Add( StaminaBar?.ContentHash() ); // embedded child
    return h.ToHashCode();
}
```

You don't author this — the generator emits it for you.

---

## How `ContentHash` is emitted

Source: `Code/Generation/SuiWrapperEmitter.cs` `EmitContentHash`. The wrapper overrides `ContentHash()` from `SuiPanel<TView>` and combines:

1. `IsShown` — so `Hide()` / `Show()` on an embedded wrapper invalidates the parent's hash.
2. Every Variable on the document whose `Source.Kind == Manual` (these are the ones the user writes from gameplay code).
3. Every embedded `SuiReference` child's `ContentHash()` — single child = direct hash; ForEach child = list count + per-item hash.

This recursion is **not optional**. Without it, mutating a deep leaf (`hud.HealthBar.NestedTooltip.Text = "X"`) wouldn't change the middle's hash, wouldn't change the grandparent's hash, and the leaf change would look invisible.

```csharp
// Generated wrapper override
public override int ContentHash()
{
    var h = new global::System.HashCode();
    h.Add( IsShown );                            // visibility bubbles up
    h.Add( Health );                              // own Manual Variables
    h.Add( MaxHealth );
    h.Add( StaminaBar?.ContentHash() ?? 0 );      // single embedded child
    h.Add( ChatMessages?.Count ?? 0 );             // ForEach — count signal
    if ( ChatMessages != null )
        foreach ( var __item in ChatMessages )
            h.Add( __item?.ContentHash() ?? 0 );   // per-item
    return h.ToHashCode();
}
```

The base case (`SuiPanel<TView>.ContentHash()`) returns `IsShown ? 1 : 0` so even an empty leaf wrapper bubbles visibility changes.

---

## Why both hashes exist

Razor's re-render hook is on the Panel — the `BuildHash()` method of the renderer class. The wrapper class doesn't have one (it's not a Panel). So:

- **Within one document**, mutating a Variable changes the wrapper's backing field → setter forwards it to the View → View's `BuildHash` includes it → re-render.
- **Across documents** (parent embeds child via SuiReference), mutating `parent.Child.Field` changes the child's `ContentHash` → parent's `BuildHash` includes the child's `ContentHash` expression → parent re-renders → embedded `<ChildPanel>` tag re-renders with fresh attributes.

The two hashes meet at the boundary between renderer and wrapper.

---

## What triggers a re-render

- ✅ Assigning a Variable property: `Hud.Health = 50`.
- ✅ Mutating a nested wrapper field: `Hud.StaminaBar.ActualValue = 75`.
- ✅ Editing a `List<T>` for a ForEach: `Hud.ChatMessages.Add(...)`.
- ✅ Toggling visibility: `Hud.StaminaBar.Hide()` (changes `IsShown` → propagates).

## What does NOT trigger a re-render

- ❌ Mutating a Variable not referenced by any binding (it's not in the hash).
- ❌ Mutating a property on the View directly (`Hud.View.X = ...`) — the wrapper's hash doesn't see View-only state.
- ❌ Mutating an object held by a Variable (`Hud.Player.Health -= 10` where `Player` is a `Component`-typed Variable) — the hash sees the reference, which hasn't changed. Cache-busts here need a fresh reference assignment or a manual `RefreshView()`.

For the third case, call `Hud.RefreshView()` to force-push the wrapper's current state to the View. It's the escape hatch for "I mutated something the hash doesn't catch."

---

## Cost — is this expensive?

No. `HashCode.Combine` is one CPU instruction per field. The generated `BuildHash` runs once per render tick. The recursive `ContentHash` adds one virtual call + a few field reads per embedded child per tick. Even depth-5 composition with 50 leaves comfortably fits inside the engine's per-frame UI budget.

The only path that needs care: ForEach iterating a 10000-item list will hash 10000 entries per tick. If your list is that big, throttle the list reference instead (rebuild a smaller view, or paginate).

---

## How this compares to other reactive runtimes

- **React** runs a virtual-DOM diff every render to decide what's changed; we **fingerprint** the inputs and only re-render when the fingerprint changes.
- **MobX / Vue** track each individual property access; we **eagerly hash everything in the binding graph** — coarser but simpler + zero proxy overhead.
- **UnrealMG** binds widgets to functions called once per tick; we let the engine decide via `BuildHash` and only run `BuildRenderTree` when the hash flips.

The trade-off: SUI re-renders the whole panel when any reactive field changes. For 99% of HUDs this is fine — the panel is small. For panels with 100+ children, structure them as parent + embeds so each embed only re-renders when its own slice of state moves.

---

## Source links

- [`Code/Generation/SuiBuildHashEmitter.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Generation/SuiBuildHashEmitter.cs) — `BuildHash` emit
- [`Code/Generation/SuiWrapperEmitter.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Generation/SuiWrapperEmitter.cs) — `ContentHash` emit (`EmitContentHash`)
- [`Code/Runtime/SuiPanel.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Runtime/SuiPanel.cs) — `ContentHash` base override

## See also

- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how `[Property]` mirrors flow into the View
- [Bindings]({% link concepts/bindings.md %}) — what counts as "reactive"
- [Composition]({% link concepts/composition.md %}) — depth-N reactivity rationale
- [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}) — `RefreshView` escape hatch usage
