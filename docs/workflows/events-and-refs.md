---
layout: default
title: Events & Element refs
parent: Workflows
nav_order: 5
---

# Events & Element References (V1.5 M3)
{: .no_toc }

Hook UI interactions to your gameplay code from inside the SUI Designer:
buttons that fire callbacks, sliders that emit values, panels that expose a
typed handle to `@ref`. Two interaction shapes today:

- **Code mode** — generator emits an `Action` slot on the wrapper. Your
  Component assigns a method reference: `Hud.OnFireClick = HandleFire;`
- **Doo mode** — generator emits a `Sandbox.Doo` slot. The Doo's `Body` is
  authored inside the `.sui` (WBP-like — same Graph travels with the
  widget). One `.sui` = one default Doo per slot; instance overrides still
  work through the engine's `[Property] Doo` inspector path.

## Adding an event

1. Bottom panel → **Events** tab → **+ Add Event**.
2. Pick the element (the popup pre-selects whatever is selected in the
   canvas / hierarchy).
3. Pick the event from the type's matrix (Button → `OnClick / OnHover /
   OnUnhover`; Panel-like → adds `OnRightClick`; InventorySlot → also
   `OnDoubleClick`).
4. Pick **Code** or **Doo**.
5. Type the handler name. The popup pre-suggests `On<Element><EventSuffix>`
   (e.g. `OnFireButtonClick`).
6. **Code mode** — OK saves; the generator emits the slot. Your `.cs`
   Component assigns it: `Hud.OnFireClick = HandleFire;`
7. **Doo mode** — click **Open Full Editor** to author the Doo's Body in
   the engine's DooEditor. Close the editor, then OK in the popup. The
   Body persists into the `.sui`.

The Events tab lists every wired event project-wide. Per-row Edit reopens
the popup pre-filled; Delete clears the slot (undo-safe via the standard
command stack).

## Exposing an element as `@ref`

Pick the element → Details → **Common** → check **Expose as Variable**.

The generator emits `@ref="<ElementName>"` on the markup tag and declares
a typed property on the renderer Panel:

```csharp
public Sandbox.UI.Button FireButton { get; private set; }
```

Reach it from gameplay code via `Hud.View?.FireButton`. Exposed elements
render in bold on the Hierarchy panel so you can scan and see which
elements are reachable from code.

## What gets generated

For an `InteractiveHud.sui` with:

- Variable `Health: int`
- HudPanel marked `ExposeAsVariable`
- FireButton with `OnClick = Code "OnFireClick"`, `OnHover = Doo "OnFireHover"`

The wrapper class (`InteractiveHud.cs`):

```csharp
public sealed class InteractiveHud : SuiPanel<InteractiveHudPanel>
{
    [Property, Group("Public")] public int Health { get; set; } = 100;

    [Property, Group("Events")] public Action OnFireClick { get; set; }
    [Property, Group("Events")] public global::Sandbox.Doo OnFireHover { get; set; }
        = global::Sandbox.Json.Deserialize<global::Sandbox.Doo>( @"{""body"":[ /* schema-embedded */ ]}" );

    protected override void SyncFieldsTo( InteractiveHudPanel view )
    {
        view.Health = Health;
        view.OnFireClick = OnFireClick;
        view.OnFireHover = () => Host?.RunDoo( OnFireHover );
    }
}
```

The renderer (`InteractiveHudPanel.razor`):

```razor
<div class="hud-panel" @ref="HudPanel">
    <label>@HpLabelText</label>
    <div class="fire-button" onclick=@OnFireClick onmouseover=@OnFireHover>FIRE</div>
</div>

@code
{
    public int Health { get; set; }
    public Action OnFireClick { get; set; }
    public Action OnFireHover { get; set; }
    public global::Sandbox.UI.Panel HudPanel { get; private set; }
}
```

## Use cases not covered by the matrix

Events outside the matrix (press vs release, hold time, progress
completion, etc.) drop down to **`@ref` + low-level Sandbox.UI**:

```csharp
public sealed class HudController : Component
{
    [Property] public Game.UI.InteractiveHud Hud { get; set; } = new();
    private RealTimeSince _pressStart;

    protected override void OnStart()
    {
        Hud.Show();
        Hud.View.FireButton.OnMouseDown = OnPress;
        Hud.View.FireButton.OnMouseUp = OnRelease;
    }

    void OnPress()   { _pressStart = 0; }
    void OnRelease() { Log.Info( $"held {_pressStart}s" ); }
}
```

Custom event slots — `widget.OnHpZero` declared by the user that other
Components Subscribe to — land in V1.6+.

## Cross-paradigm collisions

Every public identifier on the generated wrapper has to be unique across
five sources:

1. Variable names
2. SuiReference field names (sanitised element `Name`)
3. Code-mode event handler names
4. Doo-mode event slot property names
5. `@ref` exposed element field names

The validator runs `SuiNameConflictDetector` on every save and surfaces
every collision in the Compile Results panel with both contributors named
("name collision on 'Health': Variable 'Health' vs FireButton.OnClick
(Code handler)"). No fix-recompile-fix-recompile cycle.

## Sample

`Assets/SuiSamples/InteractiveHud.sui` ships the full setup —
`HpLabel`, `FireButton` (Code + Doo), `PauseButton` (Code), and `HudPanel`
exposed via `@ref`. The companion Controller
`Code/BindTest/InteractiveHudController.cs` shows the gameplay-side
wiring.

## Known gap — Action Graph picker on Code-mode slots

When a slot is authored in **Code mode**, codegen emits
`[Property] public Action OnFoo { get; set; }` on the wrapper. Because
the field is typed `Action`, the s&box inspector automatically offers
an **Action Graph** picker next to it — a third authoring path on top
of "bind via C# handler" (Code) and "author Doo blocks in the SUI
Designer" (Doo).

That picker **persists fine to the scene JSON** (save/load from disk
keeps the graph), but the delegate is **lost when entering Play**:
the engine's Play-mode snapshot does not appear to round-trip an
`Action` property that lives inside a non-Component wrapper
(`SuiPanel<TView>` is a plain class). Equivalent slots on built-in
Components like `Sandbox.Mapping.Button` work because the property
lives directly on a `Component`.

**Practical guidance**

* Use **Code mode** when you want a C# handler on the host Controller.
* Use **Doo mode** when you want visual scripting that survives Play.
* The Action Graph picker is cosmetic on SUI wrappers — don't ship
  logic through it.

Reconsider only if a user case justifies refactoring the wrapper to be
a `Component` (would unlock the Action Graph round-trip but is a large
M2-K7 architecture change for a path the engine team plans to deprecate
within ~12 months in favour of Doo).
