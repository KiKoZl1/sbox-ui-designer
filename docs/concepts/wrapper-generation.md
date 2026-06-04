---
layout: default
title: Wrapper generation
parent: Concepts
nav_order: 5
---

# Wrapper Generation

Every `.sui` document, when compiled, produces the same set of files:

| File | What it is |
|---|---|
| `<Name>Panel.razor` | The PanelComponent renderer — this is the `<root>` Razor tree the engine actually paints |
| `<Name>Panel.razor.scss` | The stylesheet for the renderer (matches the renderer's `.razor` name) |
| `<Name>.cs` | **The user-facing wrapper class** — what your gameplay code touches |
| `<Name>Panel.User.scss` | Author-owned overrides — created once, never overwritten (imported last so your rules win the cascade) |

No per-document mode. No dropdown to configure. One shape, every time. *(V1.5-M2-K6 — DEVIATIONS D-013.)*

## The wrapper class

`<Name>.cs` extends `SuiPanel<<Name>Panel>` (a small runtime base — `Code/Runtime/SuiPanel.cs`) and gives you:

```csharp
namespace Game.UI;

public sealed class MyHud : SuiPanel<MyHudPanel>
{
    // Lifetime control (inherited from SuiPanel<TView>):
    //   Add( parent = null )    // mount + ScreenPanel + Panel, hidden
    //   Show( parent = null )   // mount-if-needed + visible
    //   Hide()                  // hide, keep mount around
    //   Remove()                // destroy mount entirely
    //   RefreshView()           // re-push wrapper field values to View
    //   IsMounted, IsShown      // state queries

    // Per-Variable mirrors (one [Property] per Manual Variable in the .sui):
    [Property, Group("Internal")] public int Health { get; set; } = 100;
    [Property, Group("Public")]   public int MaxHealth { get; set; } = 100;

    // Per-SuiReference named-instance fields (one per embed on the canvas,
    // typed as the child wrapper):
    [Property, Group("Children")] public global::Game.UI.HealthBar StaminaBar { get; set; } = new();
    [Property, Group("Children")] public List<global::Game.UI.SlotIcon> InventorySlots { get; set; } = new();

    protected override void SyncFieldsTo( MyHudPanel view )
    {
        view.Health = Health;
        view.MaxHealth = MaxHealth;
        view.StaminaBar = StaminaBar;          // shared by reference — see below
        view.InventorySlots = InventorySlots;  // ditto
    }
}
```

## How your code uses it

Declare the wrapper as a `[Property]` field on **any** Component:

```csharp
public sealed class PlayerHudController : Component
{
    [Property] public MyHud Hud { get; set; } = new();

    protected override void OnStart() => Hud.Show();

    protected override void OnUpdate()
    {
        if ( IsProxy ) return;
        Hud.Health    = Player.Hp;
        Hud.MaxHealth = Player.MaxHp;
        Hud.StaminaBar.ActualValue = Player.Stamina;  // ← direct access by name
    }
}
```

When you set `Hud.StaminaBar.ActualValue`, the wrapper's `SyncFieldsTo` passes the `StaminaBar` reference (not a copy) into the PanelComponent — so the renderer sees the same instance the gameplay code edited. Next render tick picks it up via `BuildHash` and re-renders.

## Composition: `Parent.Child.Var` at any depth

Every embedded `.sui` (drag from the **USER WIDGETS** palette section onto the canvas) becomes a named `[Property]` field on the parent wrapper, typed as the child's wrapper class. Each child likewise has its own children:

```csharp
Hud.StaminaBar.ActualValue           = 75;     // Hud → StaminaBar (1 level)
Hud.InventoryGrid.Slots[2].IconPath  = "...";  // Hud → InventoryGrid → Slots[i] (2 levels)
```

No depth limit. Each `.sui` always has a wrapper, so the chain just keeps working.

### Naming rules

- The field name on the parent wrapper is **the SuiReference's `Name` in the Hierarchy** (M2-K3 enforces uniqueness across the document — duplicates are auto-suffixed `_2`/`_3`/...).
- The child wrapper's namespace is the child doc's `Output.Namespace` (default `Game.UI`); referenced with a `global::` prefix so it resolves even when the parent's own namespace shares a prefix.
- Variables become fields by their `Name` (must be a valid C# identifier — the Variable dialog enforces this).
- Internal Variables (`IsPublic = false`) are accessible too — they live on the same wrapper, in the `"Internal"` inspector group. Mark `IsPublic = true` only to also surface them in the **parent's** Sub-UI Props editor.

## ForEach: dynamic-list children

When a SuiReference has ForEach enabled, the field becomes a `List<TChild>`. Code edits the list and the parent re-renders:

```csharp
Hud.ChatMessages.Add( new ChatLine { Text = "Hello", Color = Color.Green } );
Hud.ChatMessages[0] = new ChatLine { Text = "Updated!" };
```

Individual mounted-child instances are NOT addressable — `Hud.ChatMessages[3].View.Text = "X"` doesn't work as a render-time mutation. The list is the source of truth; children are derived views. Same semantics as React `.map()` / Vue `v-for`.

Items in the list need member names matching the child's `IsPublic` Variable names — codegen wires `<ChildPanel VarName=@(__item?.VarName ?? default) />` per iteration. No explicit mapping table; the schema's `ItemPropId` / `IndexPropId` fields are reserved + unused in V1.5 (DEVIATIONS D-007 area).

## Why this shape (vs the old 3 modes)

V1.5 originally shipped three Output Modes (Manual, Singleton, Instance, plus a deprecated PerLocalPlayer). The refactor in M2-K6 deleted them all because:

1. **The wrapper is always useful.** Even if you only ever wanted a "static `Show()` modal", `new MyModal().Show()` is one line longer than `MyModal.Show()` — but you also get the lifecycle + property-mirror API for free.
2. **Three modes was a trap.** Children authored in Manual mode never generated a wrapper class, so any parent that embedded them via SuiReference referenced a non-existent type and failed to compile. Detected during M2-K5 validation, prompted the refactor.
3. **Zero-cost when unused.** The wrapper is just a POCO `[Property]` field on your Component. The ScreenPanel + Panel are only created the moment you call `Show()` / `Add()`.

If you only need the renderer (e.g. you want to host it inside another framework's UI tree), `MyHud.Show()` and `Add()` are no-ops you ignore. The wrapper doesn't get in the way.

## Why no auto-mount?

You may wonder why the wrapper doesn't just mount itself when the Component starts — i.e. why gameplay code has to call `Hud.Show()` explicitly rather than the panel auto-attaching the moment `OnStart` fires.

M0 D-003 spike (`GameObject.MoveTo` negative result) confirmed two engine constraints that made auto-mount impossible:

1. **`GameObject.MoveTo` does not exist in s&box.** A wrapper can't relocate its mount GameObject under a different parent after the fact.
2. **Components are bound to one GameObject for life.** The PanelComponent the wrapper owns can't be lifted off its mount and re-parented either.

The original PRD-22 design assumed the wrapper could auto-mount on Component attach and then move/re-parent as needed. That design was killed by the spike. As a result, `SuiPanel<TView>` uses an explicit `Show()` / `Hide()` lifecycle — gameplay code must call `wrapper.Show(SuiInputMode.X)` at the right moment, rather than the panel auto-attaching when its host Component starts.

## Cross-references

- DEVIATIONS D-013 — the Output Mode removal decision and rationale
- DEVIATIONS D-005 — Variable.IsPublic (replaced the separate AcceptedProp surface)
- `Code/Runtime/SuiPanel.cs` — base class implementation
- `Code/Generation/SuiWrapperEmitter.cs` — the codegen that produces `<Name>.cs`
