---
layout: default
title: ProgressBar
parent: Element reference
nav_order: 11
---

# ProgressBar

A fill bar for stats (health, stamina, mana, hunger, …). Every visual property is bindable so you can drive the bar entirely from a Variable.

## Properties (Progress section)

| Field | Default | Notes |
|---|---|---|
| **Value** (alias for Progress Preview Value) | 50 | Preview value in the editor (Min..Max) — bindable |
| **Min** | 0 | Lower bound — bindable |
| **Max** | 100 | Upper bound — bindable |
| **Direction** | `LeftToRight` | `LeftToRight / RightToLeft / TopToBottom / BottomToTop` — bindable |
| **Fill Color** | `#4ade80` | Color of the filled portion — bindable |

## Bindable properties

| Property | Mode | Target type |
|---|---|---|
| `Value` | OneTime / OneWay | float |
| `Min` | OneTime / OneWay | float |
| `Max` | OneTime / OneWay | float |
| `FillColor` | OneTime / OneWay | Color |
| `Direction` | OneTime / OneWay | string (enum name) |
| Style + Universal | OneWay | per matrix |

(ProgressBar has no events.) See [Binding-mode matrix]({% link reference/binding-mode-matrix.md %}).

## Generated output

V1.5 emits the bar container + an inner `.fill` whose `width` (or `height` for vertical directions) is computed from `Value` / `Min` / `Max`:

```razor
<div class="health-bar sui-health-bar">
    <div class="fill" style="@FillStyle"></div>
</div>
```

```scss
.sui-health-bar {
  width: 320px;
  height: 24px;
  background-color: rgba(63, 29, 29, 0.8);
  border-color: #7f1d1d;
  border-width: 1px;
  border-radius: 4px;
  overflow: hidden;

  .fill {
    background-color: #ef4444;
    height: 100%;
  }
}
```

The renderer computes `FillStyle` from the current `Value` / `Min` / `Max` per render — change any of them and the bar updates next `BuildHash()` tick.

## Wiring from a Variable

Declare a Variable on your `.sui` (e.g. `Health: int`, default 100), then bind `ProgressBar.Value` to it:

1. Select the ProgressBar in the canvas.
2. Click the chain icon next to **Value** in the Details panel.
3. Pick `Health` from the Source dropdown. Mode: `OneWay` (default).
4. (Optional) Add a `Clamp(0, 100)` converter to defensive-clamp out-of-range values.
5. Save. Compile.

From gameplay code:

```csharp
[Property] public MyHud Hud { get; set; } = new();

protected override void OnUpdate()
{
    if ( IsProxy ) return;
    Hud.Health = Player.Hp;   // wrapper auto-syncs to the View; bar redraws
}
```

See [Bindings]({% link concepts/bindings.md %}) for the full mental model.

## Tips

- For a "75 / 100 HP" label, layer a `Text` element on top of the ProgressBar and bind its `Text` property using the [Compose converter]({% link concepts/converters.md %}) — `Compose("HP: ", Health, " / ", MaxHealth)`. See the [Health HUD tutorial]({% link tutorials/health-hud-with-converters.md %}).
- For a glowing effect, add a box-shadow in `.User.scss`:

```scss
.health-bar { box-shadow: 0 0 8px rgba(239, 68, 68, 0.6); }
```

- For animated transitions, the `.fill` div is hand-emitable — add `transition: width 0.3s ease` in `.User.scss` and the bar smoothly interpolates between Variable updates.

## See also

- [Bindings]({% link concepts/bindings.md %})
- [Converters]({% link concepts/converters.md %})
- [Health HUD with converters]({% link tutorials/health-hud-with-converters.md %})
