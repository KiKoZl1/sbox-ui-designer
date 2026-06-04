---
layout: default
title: Slider
parent: Element reference
nav_order: 17
---

# Slider
{: .no_toc }

A horizontal slider with author-controlled track / fill / thumb / tooltip. **Fully custom markup** — V1.5 M4 (DEVIATIONS D-022) rebuilds the slider from scratch instead of wrapping `Sandbox.UI.SliderControl`.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Why the slider is custom

Three engine blockers forced the rebuild (DEVIATIONS D-022):

1. **Tooltip color override** — engine `SliderControl` buries the tooltip selector behind `.slidercontrol > .inner > .track > .thumb > .value-tooltip > .label`. The Sandbox.UI CSS parser silently drops past two levels of `>` chaining, so author-side `.value-tooltip { color: ... }` overrides were ignored.
2. **Tooltip only renders with `HasActive` true** — the canvas (which has no input state) couldn't show the tooltip at all, breaking canvas-vs-Play parity.
3. **`flex-grow: 1` baked into `.slidercontrol`** — the engine slider absorbed all remaining vertical space inside a `flex-direction: column` container, shoving siblings to the floor.

V1.5 Slider is **100% custom** — hand-written track / fill / thumb / tooltip divs the canvas paints identically. Drag math mirrors the engine (`MathX.LerpInverse(Mouse.Position.x, Box.Left, Box.Right)`) but lives in generated handlers.

## Properties (Slider section)

| Field | Default | Notes |
|---|---|---|
| **Slider Min** | `0` | Bindable — lower bound |
| **Slider Max** | `100` | Bindable — upper bound |
| **Slider Step** | `1` | Snap value to multiples of step (0 = continuous) |
| **Slider Track Color** | `#22222288` | Background track |
| **Slider Fill Color** | `#4ade80` | Filled portion (left of the thumb) |
| **Slider Handle Color** | `#ffffff` | The thumb |
| **Slider Show Value** | `false` | Opt-in custom tooltip pill above the thumb |
| **Slider Tooltip Bg Color** | `#000000` | Tooltip background (only when ShowValue) |
| **Slider Tooltip Text Color** | `#ffffff` | Tooltip text (only when ShowValue) |
| **Slider Value** | `50` | Design-time preview position |
| **Slider Orientation** | `Horizontal` | Future-proof; V1.5 ships horizontal only (PRD 21 § 11 #2) |

The wrapper carries `flex-grow: 0` (intentional override of the engine pattern) so cards lay out predictably.

## Bindable properties

| Property | Mode | Target type |
|---|---|---|
| `Value` | OneTime / OneWay / **TwoWay** (default) | `float` (or `int` — the slider casts) |
| `Min` | OneTime / OneWay | `float` |
| `Max` | OneTime / OneWay | `float` |
| Style + Universal | OneWay | per matrix |

`Value` UpdateTrigger options: `OnChange` (write per drag tick) / `OnRelease` (commit on mouse-up) / `Manual` (call `wrapper.Apply.<Field>()`). See [Input & Update triggers]({% link concepts/input-and-update-triggers.md %}).

## Events surfaced

| Event | Signature |
|---|---|
| `OnValueChanged` | `Action<float>` — fires per drag tick |
| `OnDragStart` | `Action` |
| `OnDragEnd` | `Action` |

## Codegen — `OnChange` trigger

```razor
<div class="sui-slider sui-volume-slider" @ref="VolumeSliderTrackRef"
     onmousedown=@(e => OnVolumeSlider_OnTrackInput(e))
     onmousemove=@(e => OnVolumeSlider_OnTrackMove(e))>
    <div class="sui-slider-track" style="background-color: #22222288"></div>
    <div class="sui-slider-fill"  style="background-color: #4ade80; width: @($"{(Volume - 0f) / (100f - 0f) * 100f}%")"></div>
    <div class="sui-slider-thumb" style="background-color: #ffffff; left: @($"{(Volume - 0f) / (100f - 0f) * 100f}%")"></div>
    @if ( SliderShowValue && HasActive )
    {
        <div class="sui-slider-tooltip" ...>@Volume</div>
    }
</div>

@code {
    public float Volume { get; set; }

    private void OnVolumeSlider_OnTrackInput( PanelEvent e ) { /* compute Volume from mouse pos */ }
    private void OnVolumeSlider_OnTrackMove ( PanelEvent e ) { /* drag */ }
}
```

CSS chains are kept ≤ 2 deep (selector form `.sui-slider-fill` / `.sui-slider-thumb` / `.sui-slider-tooltip` — descendants of the wrapper, never `> > >` chained — required after Sandbox.UI parser drops deeper chains).

## Codegen — `OnRelease` / `Manual` trigger

```razor
@code {
    public float Volume { get; set; }
    private float _volumeVisual;   // visual buffer

    protected override void Tick()
    {
        // Detect HasActive true → false transition for OnRelease — commit _volumeVisual → Volume
        // Idle ticks resync _volumeVisual = Volume so external writes flow back to displayed position
    }
}
```

For `Manual`, the wrapper exposes `Settings.Apply.Volume()` — call it explicitly. The slider visually responds to drag in real time but the bound Variable only updates when you call Apply.

## Tutorial — drop + bind + read

1. **Drop** the Slider onto the canvas at (40, 100), size 240×24.
2. **Variable** — add `Volume: float` with `Default = 50`, `IsPublic = false`.
3. **Bind** — Bind dialog → Property: `Value` → Source: `Volume` → Mode: `TwoWay` → UpdateTrigger: `OnRelease` (avoid per-tick spam).
4. **Save + Compile**.
5. **Use from code**:

```csharp
[Property] public Game.UI.SettingsPanel Settings { get; set; } = new();

void OnVolumeReleased()
{
    // Triggered by the released slider — Settings.Volume is now the new value
    AudioSystem.MasterVolume = Settings.Volume / 100f;
}
```

(For automatic notification, also wire an `OnValueChanged` Code event slot.)

## Tooltip

Opt-in via `Slider Show Value`. The tooltip pill renders above the thumb at the current value, with author-controlled bg + text colors. The canvas mirrors the runtime markup pixel-for-pixel.

## Engine features not exposed (V1.5)

- `ShowTextEntry` — number entry inside the slider.
- `ShowRange` — min/max labels.
- `OnValueChanged` engine `Action<float>` — SUI emits its own event slots per PRD 20.
- Vertical orientation (PRD 21 § 11 #2 — kept dropped).

## See also

- [Bindings]({% link concepts/bindings.md %})
- [Input & Update triggers]({% link concepts/input-and-update-triggers.md %})
- [Settings screen tutorial]({% link tutorials/settings-screen.md %})
