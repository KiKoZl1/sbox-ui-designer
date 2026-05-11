---
layout: default
title: ProgressBar
parent: Element reference
nav_order: 11
---

# ProgressBar

A fill bar for stats (health, stamina, mana, hunger, …).

## Properties (Progress section)

| Field | Default | Notes |
|---|---|---|
| **Value** (alias for Progress Preview Value) | 0.5 | Preview value in the editor (0..1 or 0..MaxValue) |
| **Max Value** (Progress Max) | 100 | Upper bound |
| **Min Value** (Progress Min) | 0 | Lower bound |
| **Direction** | LeftToRight | LeftToRight / RightToLeft / TopToBottom / BottomToTop |
| **Fill Color** | `#4ade80` | Color of the filled portion |

## Generated output

V1 emits the bar container only — the fill is rendered by the canvas/runtime based on the value.

```html
<div class="health-bar sui-health-bar"></div>
```

```scss
.sui-health-bar {
  width: 320px;
  height: 24px;
  background-color: rgba(63, 29, 29, 0.8);
  border-color: #7f1d1d;
  border-width: 1px;
  border-radius: 4px;
}
```

## Wiring to runtime data

The `ProgressPreviewValue` is editor-only. At runtime, you bind the bar's fill from your `PanelComponent` code:

```csharp
public partial class MyHud
{
    public float Health { get; set; } = 1f;  // 0..1
}
```

And in the Razor (you'll need to edit OUTSIDE the SUI generated block for V1, V1.5 will inline):

```razor
<div class="health-bar sui-health-bar"
     style="--progress: @(Health.ToString())">
  <div class="fill" style="width: @($"{Health * 100}%")"></div>
</div>
```

Native ProgressBar binding will be first-class in V1.5.

## Tips

- For a glowing effect, add a box-shadow in `.User.scss`:

```scss
.health-bar { box-shadow: 0 0 8px rgba(239, 68, 68, 0.6); }
```

- For animated transitions, use `transition: width 0.3s ease;` on the inner fill div.
