---
layout: default
title: Visibility, Overflow, Pointer Events
parent: Concepts
nav_order: 4
---

# Visibility, Overflow, Pointer Events

Three independent attributes that control how an element renders and receives input.

## Visibility

| Value | Layout space? | Visually visible? | Use case |
|---|---|---|---|
| **Visible** | ✓ | ✓ | Default |
| **Hidden** | ✓ | ✗ | Pre-load a panel that pops in later. Toggling Hidden ↔ Visible doesn't cause layout reflow |
| **Collapsed** | ✗ | ✗ | Truly remove from layout. Siblings shift to fill the space |

In code:

```csharp
HealthBar.Style.Visibility = SuiVisibility.Hidden;   // hide but keep space
HealthBar.Style.Visibility = SuiVisibility.Collapsed; // remove from layout
```

Generated SCSS mapping:

- Visible → no rule emitted
- Hidden → `opacity: 0;`
- Collapsed → `display: none;`

## Overflow

Controls what happens to children that draw outside this element's bounds.

| Value | Behavior |
|---|---|
| **Visible** (default) | Children render outside the box — no clipping |
| **Hidden** | Children clipped at the element's edges. No scrollbars |
| **Scroll** | Children clipped at the edges. Scrollbars appear when content exceeds |

Common patterns:

- **Hidden** on an Image-as-thumbnail with rounded corners — combined with Border Radius, clips the image to the rounded shape.
- **Hidden** on a Panel containing oversized text — text gets clipped instead of overflowing.
- **Scroll** on a quest log / chat / inventory list — content scrollable when long.

## Pointer Events

Controls whether the element catches mouse/touch input.

| Value | Behavior |
|---|---|
| **None** (default) | Clicks/hovers pass through to elements below |
| **All** | Element catches input |

Generally:

- **All** on Button (auto-set), interactive Panel (custom controls), input fields, drag-handles
- **None** on decorative overlays (vignettes, watermarks, semi-transparent backdrops that shouldn't block clicks)

Generated SCSS:

```scss
.button { pointer-events: all; }
.decoration { pointer-events: none; }
```

## Interaction with parent

`Pointer Events: None` does NOT cascade. A parent with None doesn't disable input on children — each child decides independently.

`Visibility: Collapsed` DOES cascade — collapsing the parent removes all descendants from layout.

`Opacity` cascades (CSS rule) — parent opacity 0.5 multiplies child opacity. So if parent is opaque (1.0) and child is 0.5, child renders 0.5. If parent is 0.5 and child is 1.0, child renders 0.5 too.

## Practical examples

### Damage flash overlay

A red flash that fades in/out on hit:

```
FlashOverlay (Panel, Anchor: Stretch, Background: rgba(255, 0, 0, 0.4), Pointer Events: None)
```

Set `Pointer Events: None` so the flash doesn't block clicks to underlying UI. Toggle Visibility from None to Hidden (or animate opacity) when no damage.

### Modal with backdrop

```
ModalBackdrop (Panel, Anchor: Stretch, Background: rgba(0,0,0,0.7), Pointer Events: All)
└── ModalContent (Panel, MiddleCenter)
```

`Pointer Events: All` on the backdrop catches stray clicks (closes modal). Without it, clicks would pass through to the game world behind.

### Hidden until loaded

```
LoadingSpinner (Panel, Visibility: Hidden by default)
```

Code:

```csharp
LoadingSpinner.Style.Visibility = isLoading
    ? SuiVisibility.Visible
    : SuiVisibility.Hidden;
```

Layout doesn't shift when toggling — only the rendering changes.
