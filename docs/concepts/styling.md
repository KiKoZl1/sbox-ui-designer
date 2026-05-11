---
layout: default
title: Styling
parent: Concepts
nav_order: 3
---

# Styling
{: .no_toc }

Colors, borders, opacity — the Appearance section of the Details panel.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Color formats

SUI Designer accepts several CSS color formats in any color field (Background, Border, Color, Tint, Fill Color):

| Format | Example | Notes |
|---|---|---|
| 6-digit hex | `#22C55E` | Standard RGB. Most common |
| 8-digit hex | `#22C55E1F` | RGB + alpha (last 2 hex digits). For semi-transparent |
| 3-digit hex | `#2C5` | Short form (expands to `#22CC55`) |
| `rgb(r,g,b)` | `rgb(34, 197, 94)` | Decimal, 0..255 |
| `rgba(r,g,b,a)` | `rgba(34, 197, 94, 0.12)` | Alpha 0..1 |

The Color Picker popup emits `rgba()` by default when alpha < 1, `#hex` when alpha = 1.

## Background

The element's fill color. Solid by default; can be transparent (alpha < 1) or fully transparent (alpha 0, the default — element has no background).

Common patterns:

- Solid card: `#0f0f12`
- Translucent overlay (dim modal): `rgba(0, 0, 0, 0.5)`
- Glass card: `rgba(15, 18, 24, 0.95)` — almost opaque, slight blur of background

## Border

`Border` + `Border Width` + `Border Radius` together.

- **Border** (color) AND **Border Width** > 0 — both must be set for the stroke to appear.
- The generator now requires both to emit the SCSS — emitting `border-width: 1px` alone would fall back to a default color (often white) in some renderers.
- **Border Radius** is independent — round corners even without a border (it clips background-image, see Image element).

| Style | Combination |
|---|---|
| Solid blue 1px border | Border `#3b82f6`, Border Width `1` |
| Thick yellow rim (rare item) | Border `#facc15`, Border Width `2` |
| Pill button | Border Radius `9999` (or just half the height) |
| Rounded card | Border Radius `10` |

## Opacity

`0..1` — multiplies the element's rendered alpha. **Cascades** to children — `opacity: 0.5` on a parent means the parent AND all its children are 50% transparent.

For transparent backgrounds alone (without affecting children), use rgba in `Background` instead.

## Visibility

| Value | Behavior |
|---|---|
| **Visible** | Default. Rendered normally |
| **Hidden** | `opacity: 0` — invisible but still occupies layout space |
| **Collapsed** | `display: none` — removed from layout entirely |

Use Hidden for show/hide toggles where layout shouldn't shift (e.g. a "now loading" spinner that toggles visibility but you want the layout stable). Use Collapsed when the layout should adapt.

## Pointer Events

| Value | Behavior |
|---|---|
| **None** (default) | Clicks/hovers pass through to elements below |
| **All** | Catches input |

Set to **All** on interactive elements (Button auto-sets it). Leave as **None** on decorative overlays so the player can interact with elements behind them.

## Overflow

| Value | Behavior |
|---|---|
| **Visible** (default) | Children render outside this element's bounds |
| **Hidden** | Children clipped at the edges |
| **Scroll** | Children clipped AND scrollbars appear when content exceeds |

Use Hidden for masked content (rounded corners with image fill that should clip). Use Scroll for lists/feeds. See [ScrollPanel]({% link elements/scroll-panel.md %}).

## Customization beyond Appearance

For anything not in the Appearance section (`box-shadow`, `transform`, custom hover states, `transition`, …), use the `.User.scss` sidecar:

```scss
// MyHud.User.scss
.health-bar {
  box-shadow: 0 0 8px rgba(239, 68, 68, 0.6);
  transition: box-shadow 0.2s ease;
}

.health-bar:hover {
  box-shadow: 0 0 16px rgba(239, 68, 68, 0.9);
}
```

See [User SCSS customization]({% link workflows/user-scss-customization.md %}) for the full pattern.

## Allowed properties

Only s&box-supported CSS properties are emittable. SUI Designer enforces this via `SuiAllowedPropertyList` — see [Reference · Allowed CSS]({% link reference/allowed-css.md %}) for the complete whitelist.
