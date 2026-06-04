---
layout: default
title: Interactive states
parent: Concepts
nav_order: 11
---

# Interactive states
{: .no_toc }

Hover / Pressed / Disabled / Focused state overrides for `Button`, `InventorySlot`, and `ItemIcon` — with transitions, sounds, cursor presets, and shape presets. V1.5 M3.5 (PRD 25).
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The four states

Each interactive element has a **Normal** look (the Appearance section's standard fields) plus up to four optional overrides:

| State | CSS selector emit |
|---|---|
| **Hover** | `:hover:not(:active)` |
| **Pressed** | `:active` |
| **Disabled** | `.disabled` |
| **Focused** | `:focus` |

The `:not(:active)` selector on Hover is load-bearing — without it the engine CSS sticks on `:hover` and the click visual never wins. Validated against the M3.5 smoke test.

`.disabled` always carries `pointer-events: none` so authors don't have to write it themselves.

## Authoring

The Appearance section grows four collapsible dropdowns — one per non-Normal state. Each has:

- The same fields the Normal Appearance set has (background color, border, text color, scale, opacity).
- A **Background Image** field (with [Background Size](#background-size)) — swap per-state textures for image-based buttons.
- A **Clear State** button to blank the override.
- A `(set)` tag on the header when any field is authored.

Fields that aren't authored inherit the Normal-state value.

## The codegen

For a `FireButton` with a Hover state authored:

```scss
.sui-fire-button {
  background-color: #dc2626;
  color: #ffffff;
  transition: all 0.15s ease;
  cursor: pointer;

  &:hover:not(:active) {
    background-color: #ef4444;
  }

  &:focus {
    /* focus override */
  }

  &:active {
    background-color: #b91c1c;
    transform: scale( 0.98 );
  }

  &.disabled {
    pointer-events: none;
    opacity: 0.4;
  }
}
```

The Razor:

```razor
<div class="primary-btn sui-fire-button @(IsDisabled ? "disabled" : "")"
     tabindex="0"
     @onclick=@OnFireClick>
    <label class="label">@ButtonText</label>
</div>
```

`tabindex="0"` makes `:focus` fire for keyboard / controller navigation. `IsDisabled` is a runtime-bindable bool — toggle it from gameplay code (form validation, cooldowns).

## Transition + Sound

| Field | Default | Notes |
|---|---|---|
| **Transition Enabled** | `true` | Emits `transition: all Ns ease` on the root selector |
| **Transition Duration** | `0.15s` | Material Design "fast" baseline |
| **Hover Sound** | `""` | `SoundEvent` asset path played on `:hover` ingress |
| **Press Sound** | `""` | `SoundEvent` asset path played on `:active` ingress |

With Transition Enabled but no state overrides, the rule is visually inert. The moment you author a Hover override, the change animates.

## Cursor

`SuiCursor` enum — set the mouse cursor when the element is hovered:

| Value | CSS emit |
|---|---|
| `Default` | (no emit — inherit) |
| `Pointer` | `cursor: pointer` |
| `NotAllowed` | `cursor: not-allowed` |
| `Wait` | `cursor: wait` |
| `Text` | `cursor: text` |
| `Move` | `cursor: move` |
| `Crosshair` | `cursor: crosshair` |
| `Help` | `cursor: help` |
| `None` | `cursor: none` |

The s&box CSS subset only supports this limited set — these are the values confirmed in Facepunch public source.

## Button shape

`SuiButtonShape` overrides `BorderRadius` based on geometry:

| Value | Effect |
|---|---|
| `Rectangle` | No override — use `Style.BorderRadius` |
| `Square` | Forces `border-radius: 0` (warns if W ≠ H) |
| `Round` | Forces `border-radius: min(W,H)/2` — perfect circle when square |
| `Pill` | Forces `border-radius: 9999px` |
| `Custom` | Same as Rectangle but the inspector exposes `BorderRadius` prominently |

## Background size

When the element has a background image (Normal or per-state):

| Value | CSS emit |
|---|---|
| `Cover` | `background-size: cover` |
| `Contain` | `background-size: contain` |
| `Stretch` | `background-size: 100% 100%` |
| `Custom` | `background-size: <BackgroundWidth>px <BackgroundHeight>px` |

The Details panel also offers a **Snap to image aspect** helper that resizes the element to match the image's aspect ratio.

## What the canvas shows

The canvas **always renders Normal**. Hover / Pressed / Disabled / Focused overrides are **not** painted on the canvas — designers see them via **Test in Play**. This is intentional (the canvas has no input state) — DEVIATIONS D-021 P5 cancelled the preview-state dropdown after user feedback that it was a duplicate of Test in Play.

## Schema (V3)

The M3.5 fields land on `SuiElementProps`:

```jsonc
{
  "ButtonShape": "Rectangle",
  "HoverStyle":    { /* SuiInteractiveStateStyle */ },
  "PressedStyle":  null,
  "DisabledStyle": null,
  "FocusedStyle":  null,
  "IsDisabled": false,
  "TransitionEnabled": true,
  "TransitionDuration": 0.15,
  "HoverSound": "ui/hover.sound",
  "PressSound": "",
  "Cursor": "Pointer"
}
```

`SuiInteractiveStateStyle` is per-state — each field is nullable:

```jsonc
{
  "BackgroundColor": "#ef4444",
  "BorderColor":     null,
  "BorderWidth":     null,
  "BorderRadius":    null,
  "Color":           null,
  "Scale":           1.0,
  "Opacity":         null,
  "BackgroundImage": "ui/buttons/red_hover.png",
  "BackgroundSize":  "Cover"
}
```

Null fields inherit Normal. See [SUI JSON schema]({% link reference/sui-json-schema.md %}#v15-m35--interactive-state--button-polish-fields-on-suielementprops).

## Known gaps

- **Per-state preview on the canvas** — cancelled (M3.5 P5). Test in Play covers it.
- **`IsDisabled` Variable binding** — the renderer field exists, but the wrapper does not auto-expose `[Property] IsDisabled` per element yet. Workaround: bind it through the Universal `Enabled` matrix entry. Deferred to V1.6.
- **Alpha-aware hit testing** — engine CSS subset doesn't support complex `clip-path` shapes. Image transparency at the edges of a Button is decorative, not interactive — clicks land anywhere in the bounding rectangle.

## See also

- [Button]({% link elements/button.md %}) — per-element details
- [InventorySlot]({% link elements/inventory-slot.md %})
- [ItemIcon]({% link elements/item-icon.md %})
- [Styling]({% link concepts/styling.md %}) — BackgroundImage / BackgroundSize / Cursor / Transition
