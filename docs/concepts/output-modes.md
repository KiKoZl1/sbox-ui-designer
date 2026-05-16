# Output Modes

The **Output → Mode** dropdown on every `.sui` document controls *what gets generated alongside the PanelComponent class*. Three modes:

## Manual (default)

The generator emits only:

- `<Name>.razor` (the PanelComponent class + markup)
- `<Name>.razor.scss` (the per-class stylesheet)

You spawn the HUD yourself:

```csharp
var go = scene.CreateObject();
go.Components.Create<ScreenPanel>();
var hud = go.Components.Create<MyHud>();
hud.Health = 75;
```

Use Manual when you want explicit control over lifetime, or when the HUD is mounted by another system (e.g. a custom menu manager).

## Singleton

Emits the same `.razor` + `.razor.scss`, **plus** `<Name>Factory.cs` containing a static `Show()` / `Hide()` pair:

```csharp
var modal = DeathModal.Show();
modal.KilledByName = killer.Name;
// later…
modal.Hide();
```

Internally `Show()` creates a fresh `GameObject` + `ScreenPanel` + the PanelComponent. `Hide()` destroys the GameObject.

Use Singleton for **menus, modals, overlays** — UI whose lifetime is "open it once, close it later." Not for HUDs you want every player to see.

## PerLocalPlayer

Emits `.razor` + `.razor.scss` **plus** `<Name>Spawner.cs` — a wrapper `Component` you drop on the player prefab.

At runtime, on the local player only (`IsProxy` is false), the Spawner:

1. Creates a child `GameObject` named `<Name>_Screen`
2. Adds a `ScreenPanel` to it
3. Adds the `<Name>` PanelComponent to it
4. Forwards any `[Property]` values you set on the Spawner in the inspector

On `OnDisabled` the child GameObject is destroyed. Hot-reload re-creates cleanly.

The user workflow is:

1. Mark the `.sui` as `PerLocalPlayer`
2. Compile
3. Drag `<Name>Spawner` onto the player prefab — done

Use PerLocalPlayer for **in-game HUDs** — health, ammo, minimap, hotbar, etc.

## Why the Spawner wrapper?

Originally the design called for the PanelComponent to mount itself via `GameObject.MoveTo()` (PRD 22 § 4.5 first draft). The M0 spike against the running engine confirmed `GameObject.MoveTo()` **does not exist** in s&box — Components in s&box belong to exactly one GameObject for their lifetime and cannot be re-homed.

The wrapper-Spawner pattern is the locked V1.5 mechanism: two Components instead of one, but zero engine assumptions. The Spawner code is auto-generated and never edited by hand.

## Switching modes

You can change `Output.Mode` at any time. The next compile emits/removes the bootstrap aux file. Your `.partial.cs` sidecar with custom logic is preserved.

If you switch `PerLocalPlayer → Manual`, prefab references to the Spawner become broken — you'll need to remove them. If you switch `Manual → PerLocalPlayer`, drop the new Spawner Component on the prefab.

## Cross-references

- PRD 17 § 5 — Output Modes overview
- PRD 22 § 4 — PerLocalPlayer locked design (Spawner pattern, IsProxy guard, idempotency)
- PRD 19 — Composition (AcceptedProps on the child of an embedded reference become `[Property]` slots the Spawner forwards too)
