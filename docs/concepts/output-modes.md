# Output Modes

The **Output → Mode** dropdown on every `.sui` document controls *what gets generated alongside the PanelComponent class*. Three modes — Manual, Singleton, Instance.

## Manual (default)

The generator emits only:

- `<Name>.razor` — the PanelComponent class + markup
- `<Name>.razor.scss` — the per-class stylesheet

You spawn and tear the HUD down yourself:

```csharp
var go = scene.CreateObject();
go.Components.Create<ScreenPanel>();
var hud = go.Components.Create<MyHud>();
hud.Health = 75;
// later:
go.Destroy();
```

Use Manual when you want explicit control over lifetime and don't mind the boilerplate.

## Singleton

Emits `.razor` + `.razor.scss` **plus** `<Name>Factory.cs` containing a static `Show()` / `Hide()` pair:

```csharp
var modal = DeathModal.Show();
modal.KilledByName = killer.Name;
// later:
modal.Hide();
```

Internally `Show()` creates a fresh `GameObject` + `ScreenPanel` + the PanelComponent. `Hide()` destroys the GameObject.

Use Singleton for **modals and one-off overlays** — UI whose lifetime is "open once, close later." Not great when you want multiple distinct instances or per-player control.

## Instance ⭐ (V1.5 — recommended for in-game HUDs)

The Mode that matches the UMG / UEFN "I declare a widget instance and drive it from code" pattern.

Generator emits **two** classes:

- `<Name>Panel.razor` (+ `.razor.scss`) — the PanelComponent that renders the markup. Internal-ish, you rarely name it directly.
- `<Name>.cs` — the **user-facing wrapper class** that extends `SuiPanel<<Name>Panel>` and mirrors every Variable / AcceptedProp as a `[Property]`.

You declare it as a `[Property]` field on your own Component:

```csharp
public sealed class JobSelectController : Component
{
    [Property] public WdgSelectJob Widget { get; set; } = new();

    void OnPlayerJoined()
    {
        Widget.Add();      // mount + ScreenPanel + WdgSelectJobPanel (hidden)
        Widget.Show();     // make visible
    }

    void OnJobChosen()
    {
        Widget.Hide();     // hide but keep mounted (cheap re-show)
        // or Widget.Remove(); // tear the mount down entirely
    }

    void Update()
    {
        Widget.Health = 75;  // edits the wrapper; auto-pushes to the live View
    }
}
```

### API surface (inherited from `SuiPanel<TView>`)

| Method | What it does |
|---|---|
| `Add( parent = null )` | Create child GameObject + ScreenPanel + Panel. **Hidden** by default. Idempotent. |
| `Show( parent = null )` | Mount-if-needed + `Enabled = true`. The common one-call entry point. |
| `Hide()` | `Enabled = false` on the mount. Cheap re-show via `Show()`. |
| `Remove()` | `Destroy()` the mount entirely. Next `Show()` spawns a fresh one. |
| `RefreshView()` | Re-push every wrapper `[Property]` value to the live View. Call after batch edits. |
| `IsMounted` / `IsShown` | Read-only state queries. |

### Why two classes?

The `.razor` needs `@inherits PanelComponent` — that contract is a Sandbox.Component on a GameObject. The wrapper, by contrast, lives as a plain `[Property]` field on the user's Component (UMG-style "I have a Widget reference"). Splitting the names (`<Name>` vs `<Name>Panel`) keeps both natural:

- User types `WdgSelectJob` — matches the `.sui` name exactly
- Renderer is `WdgSelectJobPanel` — internal to the framework

The Add/Show/Hide/Remove API + per-View field sync are inherited from `SboxUiDesigner.Runtime.SuiPanel<TView>` — one runtime base class, generated subclasses get the API for free.

### Per-player control (V1.6)

V1.5 ships **single-mount** semantics: one Add() per instance. If you need per-Connection panels (UMG `UIMap[player]` pattern), declare multiple fields, or wait for the V1.6 `SuiPanelManager<T>` wrapper that adds per-Connection tracking on top of `SuiPanel<TView>`.

## Switching modes

Change `Output.Mode` at any time. The next compile emits/removes the bootstrap aux file. Your `.partial.cs` sidecar with custom logic is preserved.

If you switch `Instance → Manual`, downstream code referencing `WdgSelectJob.Show()` will break — but that breakage is exactly what you want to discover. If `Manual → Instance`, drop the new wrapper into a field and migrate code from `Components.Create<>` calls to `Widget.Show()`.

## Legacy: PerLocalPlayer (deprecated)

V1.5 originally shipped a `PerLocalPlayer` mode that auto-mounted via a Spawner Component dropped on the player prefab. **This is deprecated** because it forced "HUD always appears from spawn" — the user fed back that they want explicit `Show/Hide/Add/Remove` control from any code, not from a prefab Component lifecycle.

Loading an old `.sui` with `Mode = PerLocalPlayer` auto-migrates to `Instance` on the next compile. No data loss. Re-wire your gameplay code to call `Widget.Show()` where you previously dropped the Spawner.

## Cross-references

- PRD 17 § 5 — Output Modes overview
- PRD 22 V1.5 revised — Instance-mode locked design (post-spike: GameObject.MoveTo doesn't exist; Spawner pattern abandoned)
- PRD 19 — Composition (AcceptedProps on the child of an embedded reference become `[Property]` slots the wrapper forwards too)
