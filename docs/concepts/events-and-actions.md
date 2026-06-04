---
layout: default
title: Events & Actions
parent: Concepts
nav_order: 10
---

# Events & Actions
{: .no_toc }

Wire UI interactions to your gameplay code. Two authoring modes: **Code** (C# handler on the wrapper) or **Doo** (visual scripting stored inside the `.sui`).
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The two modes

| Mode | What ships | When to use |
|---|---|---|
| **Code** | `[Property] public Action OnX` on the wrapper | You want a C# handler in your Component code |
| **Doo** | `[Property] public Doo OnX` on the wrapper, with a default Body stored inside the `.sui` | You want a visual graph that ships with the widget, no C# code required |

Both modes use the same Events tab in the bottom panel; you pick the mode per slot.

## Event matrix

Each element type has a fixed set of slots that the matrix exposes. The seed (PRD 20 § 3.1):

| Element | Slots |
|---|---|
| `Button` | `OnClick`, `OnHover`, `OnUnhover` |
| `InventorySlot` | `OnClick`, `OnRightClick`, `OnDoubleClick`, `OnHover` |
| Panel-like | `OnClick`, `OnRightClick`, `OnHover`, `OnUnhover` |

**Input widgets (TextEntry / Slider / Toggle / DropDown)** — no event slots in V1.5. `SuiEventMatrix` does not register rows for these element types (`Code/Runtime/SuiEventMatrix.cs:119-121`). Wire reactivity through the widget's `TwoWay` binding + `UpdateTrigger` instead — see [Bindings]({% link concepts/bindings.md %}) and [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}). Event slots for input widgets are tracked for a future milestone.

`OnRightClick` is wired to plain `onclick` with `e.Button == "mouseright"` until M5 surfaces a `MousePanelEvent`-aware handler shape (DEVIATIONS D-018).

`OnHover` / `OnUnhover` map to Razor attributes `onmouseover` / `onmouseout` (engine source confirmed via PanelInput.cs — `onhover` is silently dropped — DEVIATIONS D-018).

## Code mode

The generator emits:

```csharp
[Property, Group("Events")] public Action OnFireClick { get; set; }
```

Your Component assigns it:

```csharp
public sealed class HudController : Component
{
    [Property] public Game.UI.InteractiveHud Hud { get; set; } = new();

    protected override void OnStart()
    {
        Hud.OnFireClick = HandleFire;
        Hud.Show();
    }

    void HandleFire() => Log.Info( "fired!" );
}
```

The renderer's Razor:

```razor
<div class="fire-button" @onclick=@OnFireClick>FIRE</div>
```

`SyncFieldsTo` copies the wrapper's Action into the renderer's matching field. Reassigning `Hud.OnFireClick` at runtime requires `Hud.RefreshView()` to push the new delegate through.

### Known gap — Action Graph picker on Code-mode slots

The s&box inspector automatically offers an **Action Graph** picker next to any `Action` property. For Code-mode slots on SUI wrappers, the picker **persists to scene JSON fine** but the delegate is **lost when entering Play** — the Play-mode snapshot doesn't appear to round-trip an `Action` that lives in a non-Component wrapper. Equivalent slots on built-in Components (`Sandbox.Mapping.Button`) work because the property lives directly on a Component.

**Practical guidance (DEVIATIONS D-020):**

- Use **Code mode** when you want a C# handler on the host Controller.
- Use **Doo mode** when you want visual scripting that survives Play.
- The Action Graph picker is **cosmetic** on SUI wrappers — don't ship logic through it.

## Doo mode

[Doo](https://docs.facepunch.com/s/sbox-dev/doc/doo) is the engine's new visual scripting backend (replacing ActionGraph — DEVIATIONS D-017). The generator emits:

```csharp
[Property, Group("Events"), Doo.ArgumentHint<float>("value")]
public global::Sandbox.Doo OnVolumeChanged { get; set; }
    = global::Sandbox.Json.Deserialize<global::Sandbox.Doo>(
        @"{""body"":[/* embedded body */]}" );
```

Three details to notice:

1. The **default Body** is deserialized from a JSON blob the wrapper carries. Where does the blob come from? **It's stored inside the `.sui`** (DEVIATIONS D-018, WBP-style). One `.sui` = one default Doo per slot, shared by every instance. Instance overrides still work through the engine's inspector.
2. `Doo.ArgumentHint<T>("name", Help="...")` exposes parameter names to the editor so the Doo's input pin is well-labelled.
3. The setter is **defensive** — `value ?? FactoryFromConstJson()` (DEVIATIONS D-019). The engine's scene serializer writes `"OnFoo": null` for properties the user never touched, then restores them with the null on reload — without the defensive setter the default Body would be destroyed every save/load round trip.

### Authoring a Doo body

The Events tab's **Doo mode** row has an inline **BlockTree** preview + an **Open Full Editor** button. Click Open → the engine's `DooEditorWidget` opens as a floating popup. Author your blocks → close the popup → the Doo Body writes back to the `.sui`.

`DooEditorWidget : PopupWidget` so it can't be hosted inline; we mirror its `BlockTree(Doo)` ctor for the inline preview. `Doo : IJsonConvert` so Sandbox.Json round-trips it inside the `.sui` natively.

### Doo default-body persistence (why your Body doesn't disappear)

**Symptom this prevents:** "I opened the scene, my `OnFoo` Doo Body is now empty."

When the engine's scene serializer writes a wrapper instance, it emits `"OnFoo": null` for any `[Property] Doo` slot the user never touched in the inspector. On reload it then assigns that `null` back into your property — which, with a normal auto-setter, would wipe the default Body the wrapper carried from the `.sui`.

The generator avoids this by emitting a **defensive setter** (DEVIATIONS D-019):

```csharp
private global::Sandbox.Doo _OnFoo = FactoryFromConstJson();
[Property, Group("Events")]
public global::Sandbox.Doo OnFoo
{
    get => _OnFoo;
    set => _OnFoo = value ?? FactoryFromConstJson();
}
```

The `value ?? FactoryFromConstJson()` ensures that a scene round-trip writing `null` rehydrates the default Body from the embedded JSON instead of clearing it. A real assignment from the inspector (a non-null Doo the user authored as an override) wins — only `null` is rejected.

If you ever see a Doo Body genuinely lost after reopening the scene, file an issue — that's a regression in this contract, not normal behaviour.

## Exposing an element via `@ref`

Sometimes you want to poke an element directly from gameplay code (focus a TextEntry, scroll a ScrollPanel, manipulate any `Sandbox.UI.Panel` field). Flag the element with **Common → Expose as Variable** in the Details panel:

```csharp
[Property, Group("ElementRefs")]
public Sandbox.UI.Button FireButton { get; private set; }
```

…declared on the **renderer Panel** (`<Name>Panel`). Reach it via the wrapper's `View` property:

```csharp
Hud.View?.FireButton.OnMouseDown = OnPress;
Hud.View?.PlayerName?.Focus();
```

Exposed elements render **in bold** on the Hierarchy panel so you can scan and see which elements are reachable from code.

`@ref` field name = sanitized element `Name` — **no suffix added**. If the element is named `PlayerName`, the field is `PlayerName` (see `Code/Generation/SuiElementRefEmitter.cs:25-27`).

The `Ref` suffix you may see in generated code (e.g. `PlayerNameRef`) belongs to a **different system**: TextEntry's OnLostFocus / OnSubmit / Manual commit pathway emits an implicit `@ref` field named `<element>Ref` so the codegen can read `.Text` on commit (see [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %})). That auto-ref is internal — your Expose-as-Variable field stays at the sanitized element `Name`.

Cross-paradigm collisions (Variable / SuiReference field / Code handler / Doo slot / `@ref` field all sharing one identifier) surface in Compile Results via `SuiNameConflictDetector` — both contributors are named so you don't have to grep.

## Combined Code + Doo + `@ref`

These three are not mutually exclusive — a single element can have a Code event slot AND a Doo event slot AND `@ref` exposure. Common use:

- Doo body handles UI feedback (sound, animation, transient state changes).
- Code Action handles game-logic side effects (network RPC, state machine transitions).
- `@ref` lets the controller imperatively focus / scroll / poke the element when needed.

## Cross-paradigm collision check

`SuiNameConflictDetector` runs on every save. The five sources that must produce unique identifiers across the document:

1. Variable names.
2. SuiReference field names (sanitised element `Name`).
3. Code-mode event handler names.
4. Doo-mode event slot property names.
5. `@ref` exposed element field names.

Every collision is named in Compile Results with both contributors. No fix-recompile-fix cycle.

## Sample

`Assets/SuiSamples/InteractiveHud.sui` ships the full setup — `HpLabel` (Code), `FireButton` (Code + Doo), `PauseButton` (Code), and `HudPanel` exposed via `@ref`. Companion controller: `Code/BindTest/InteractiveHudController.cs`.

## See also

- [Events & Element refs workflow]({% link workflows/events-and-refs.md %}) — the original M3 doc (still good)
- [Wrapper generation]({% link concepts/wrapper-generation.md %})
- [Bindings]({% link concepts/bindings.md %})
