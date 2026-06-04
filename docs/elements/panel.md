---
layout: default
title: Panel
parent: Element reference
nav_order: 2
---

# Panel

The simplest container. A generic `<div>` with Background, Border, Border Radius, and the standard Transform + Appearance fields.

## When to use

- As a styled background block (card, header bar, divider)
- As a parent for other elements (its size constrains children when Mode = Absolute)
- As a hit-test region (with `Pointer Events: All`)

## Properties

No type-specific section. Use the standard Transform + Appearance.

## Generated output

```html
<div class="panel sui-panel"></div>
```

If it has children:

```html
<div class="panel sui-panel">
  <div class="child-1 sui-child-1"></div>
  <div class="child-2 sui-child-2"></div>
</div>
```

SCSS:

```scss
.sui-panel {
  position: absolute;
  left: ...;
  top: ...;
  width: ...;
  height: ...;
  background-color: ...;
  border-color: ...;
  border-width: ...;
  border-radius: ...;
}
```

## Tips

- For a translucent overlay use `rgba(0,0,0,0.5)` Background.
- For a card style: Background `rgba(15,18,24,0.95)`, Border `#475569`, Border Width `2`, Border Radius `10`.
- A Panel inside a Flex parent becomes a flex item — its X/Y are ignored, but its **Size** is honored (or stretched via `align-items: stretch` on the parent's cross axis).
- For maximum CSS flexibility, leave `Style.ClassName` set to something descriptive (e.g. `"card"`) — you can target `.card` in `.User.scss` for shared styling across documents.

## Events surfaced

Panel is an interactive type (`panelLike` row in the event matrix). Set `Pointer Events: All` in Appearance for hit-testing to fire.

| Event | Razor attribute |
|---|---|
| `OnClick` | `onclick` |
| `OnRightClick` | `onclick` (known gap: fires on any button until M4 surfaces `MousePanelEvent`) |
| `OnHover` | `onmouseover` |
| `OnUnhover` | `onmouseout` |

The same `panelLike` row is shared by `Overlay`, `HorizontalBox`, `VerticalBox`, `Grid`, `ScrollPanel`, `Hotbar`, `InventoryGrid`, `ItemIcon`, and `Tooltip`.
