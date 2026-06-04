---
layout: default
title: Health HUD with converters
parent: Tutorials
nav_order: 5
---

# Tutorial — Health HUD with converters
{: .no_toc }

Build a HUD with a Health bar and a "75 / 100 HP" label, both driven by Variables through converter chains. ~15 minutes.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What we'll build

A 360×80 panel anchored top-left:

- **ProgressBar** — fill scales 0..1 from `Health: int (0..MaxHealth)`. Chain: `IntToFloat → Clamp(0, MaxHealth) → Divide(MaxHealth)`.
- **Text label** — "HP: 75 / 100" — bound to a Compose chain over `Health` + `MaxHealth`.
- **Background tint** — turns red when Health < 30 via a custom converter `HpTintColor`.

Driven from gameplay code:

```csharp
Hud.Health = 50;   // bar shrinks, label updates, bg tints if low
```

## Prerequisites

- [SUI Designer installed]({% link getting-started/install.md %}).
- Comfortable with [Variables]({% link concepts/variables.md %}) + [Bindings]({% link concepts/bindings.md %}) + [Converters]({% link concepts/converters.md %}).

## 1. Create the `.sui`

Asset Browser → right-click → **New** → **Sbox UI Document**. Name `Hud`. Open in Designer.

Output: ClassName `Hud`, Namespace `Game.UI`.

## 2. Declare the Variables

Variables tab → **+ Add Variable** twice:

| Name | Type | Default | IsPublic | Group |
|---|---|---|---|---|
| `Health` | int | `100` | false | Stats |
| `MaxHealth` | int | `100` | false | Stats |

## 3. Build the layout

Add a root **Panel** sized 360×80 at TopLeft anchor (40, 40). Background `rgba(15, 15, 18, 0.95)`, BorderRadius 6, padding 12px.

Inside, a **VerticalBox** filling the panel — gap 6.

Inside:

### Label

**Text** — Name `HpLabel`. Text "HP: 100 / 100" (placeholder). FontSize 14, FontWeight Bold, Color #ffffff, TextSizeMode Fixed, Height 20.

### Bar

**ProgressBar** — Name `HealthBar`. Size 336×24. Background `rgba(63, 29, 29, 0.8)`. BorderColor `#7f1d1d`. BorderWidth 1. BorderRadius 4. ProgressMin 0, ProgressMax 1 (we'll bind a 0..1 fraction). ProgressFillColor `#ef4444`.

## 4. Bind the ProgressBar

Select `HealthBar`. Click chain icon next to **Value**. Bind popup:

- Source: `Health` (yellow tint — int doesn't match the expected float)
- Mode: OneWay (default)
- Converter chain (the chain feed — the value flowing in from the previous step — lands in `Args[0]` by default, so you only need to fill the remaining slots):
  1. Add step `builtin.IntToFloat` — no extra args.
  2. Add step `builtin.Clamp` — Args[1] = literal `0`, Args[2] = pick the **Variable** `MaxHealth` (typed picker accepts cross-Variable args).
  3. Add step `builtin.Divide` — Args[1] = pick **Variable** `MaxHealth` (the convertor accepts int via implicit cast).

Resulting chain: `Health → IntToFloat → Clamp(0, MaxHealth) → Divide(MaxHealth)`.

The Bind popup's "Expects: float" hint turns green. OK.

Save (`Ctrl+S`). Compile (`Ctrl+B`). Test in Play — the bar fills proportionally to `Health / MaxHealth`.

## 5. Bind the label with Compose

Select `HpLabel`. Click chain icon next to **Text**. Bind popup:

- Source: `Health` (yellow — int doesn't match expected string)
- Converter chain:
  1. Add step `builtin.Compose`.

The popup shows a single **+** button for Compose (D-027 polish). Click it:

- Pick **Text** → literal editor opens → type `"HP: "` → OK.
- Pick **+ → Variable** → `Health`.
- Pick **+ → Text** → type `" / "` → OK.
- Pick **+ → Variable** → `MaxHealth`.

> Compose is a special-case converter that does not auto-receive the chain feed — every part you want has to be picked explicitly through the **+** menu.

Resulting Compose call: `Compose("HP: ", Health, " / ", MaxHealth)` → `"HP: 75 / 100"`. OK.

Save. Compile. Test in Play — label reflects the current values.

## 6. Custom converter — low-HP tint

We want the panel's background to flash red when health drops below 30. Two ways: a chain of `LessThan → If` builtins (verbose), or a one-line custom converter.

### Option A — chain

Bind the **root Panel's BackgroundColor**:

- Source: `Health`
- Chain:
  1. `builtin.LessThan` — Args[1] = literal `30`.
  2. `builtin.If` — Args[0] = chain (bool), Args[1] = literal Color `#3f1d1d` (low), Args[2] = literal Color `#0f0f12` (normal).

### Option B — custom converter (recommended)

Open `Code/GameConverters.cs` (create if missing). Add:

```csharp
using Sandbox;
using SboxUiDesigner.Runtime;

public static class GameConverters
{
    [SuiConverter( "HpTintColor", Category = "Game",
        Description = "Background tint that flashes red when HP is low" )]
    public static Color HpTintColor( int hp )
        => hp < 30
            ? new Color( 0.25f, 0.11f, 0.11f, 0.95f )  // dim red
            : new Color( 0.06f, 0.06f, 0.07f, 0.95f ); // dark grey (normal)
}
```

The Designer picks it up immediately. Reopen the Bind popup on the root Panel's `BackgroundColor`:

- Source: `Health`
- Chain: `user.HpTintColor` — one step, no args needed.

Cleaner.

## 7. Use it from gameplay code

In a real project the HP value comes from your player class. The snippet below uses a local `[Property] int CurrentHp` so it copy-pastes into an empty project — replace it with whatever exposes your player's current HP (typically a `[Property] public PlayerHealth Player { get; set; }` reference exposing an int).

```csharp
using Sandbox;
using Game.UI;

public sealed class HudController : Component
{
    [Property] public Hud View { get; set; } = new();

    // Stand-in for your real player HP source.
    [Property] public int CurrentHp { get; set; } = 100;

    protected override void OnStart()
    {
        View.MaxHealth = 100;
        View.Health    = CurrentHp;
        View.Show();
    }

    protected override void OnUpdate()
    {
        // On a networked player, only run the HUD locally —
        // comment out if HudController lives on a non-networked GameObject.
        if ( IsProxy ) return;

        View.Health = CurrentHp;
    }
}
```

Drop `HudController` on any GameObject. Play. Watch the bar shrink + label tick down + bg flash red as you damage the player.

## Variants to try

- **Critical pulse** — bind `Opacity` to a chain `LessThan(20) → If(0.5, 1.0)` so the panel half-fades when very low.
- **Smooth fill animation** — add `transition: width 0.3s ease` to `.User.scss` so the bar interpolates between Variable updates.
- **Glow effect** — bind `BorderColor` via `HpTintColor` so the rim matches the bg tint.

## What you just learned

- **Converter chains** transform a Variable as it flows to a property — multiple steps in series.
- **Chain feed** plugs into a specific arg position (the `🔗 (chain feed)` button) — default 0, repositionable.
- **`Compose`** is the easy-mode string builder — no `{N}` placeholders.
- **Custom `[SuiConverter]`** drops into `Code/GameConverters.cs` — pure functions, autodetected by reflection.
- **Universal bindings** (like `BackgroundColor`) work on any element type.

## Next

- [Converters concept]({% link concepts/converters.md %})
- [Working with converters workflow]({% link workflows/working-with-converters.md %})
- [Converters catalog]({% link reference/converters-catalog.md %}) — every builtin signature
- [Settings screen tutorial]({% link tutorials/settings-screen.md %}) — different focus: input widgets + Apply API
