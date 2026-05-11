---
layout: default
title: ScrollPanel
parent: Element reference
nav_order: 10
---

# ScrollPanel

A flex container with `overflow: scroll` so its children become scrollable when they exceed the container's bounds.

## When to use

- Long lists (chat log, quest list, inventory in a fixed-height window)
- Scrollable subpanels of a HUD/menu

## Properties

Standard Transform (typically Mode = Flex) + Overflow = Scroll (set automatically when you set the type, but visible in Appearance section if you want to override).

## Generated output

```html
<div class="quest-list sui-quest-list">
  <label class="quest-item sui-q1">▸ Find the Lost Camp</label>
  <label class="quest-item sui-q2">Hunt 5 Deer</label>
  <label class="quest-item sui-q3">Build a Shelter</label>
  ...
</div>
```

```scss
.sui-quest-list {
  position: absolute;
  width: 260px;
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 8px;
  overflow: scroll;
}
```

## Scrollbar styling

s&box uses Qt scrollbars by default. SUI Designer doesn't currently auto-emit scrollbar styling. To customize, add to `.User.scss`:

```scss
.quest-list {
  // scrollbar handled by Qt — limited styling in s&box runtime CSS
}
```

For thin scrollbar styling in the editor itself (Palette / Hierarchy / Details), see `SuiScrollStyle.cs`.

## Tips

- Children must have a `Height` set or `min-content` — otherwise the parent's `height` collapses and there's nothing to scroll.
- Setting `Overflow: Hidden` on the ScrollPanel just hides the overflow (clips, no scrollbars). Use `Scroll` for actual scrolling.
- A ScrollPanel is just a Flex container with `overflow: scroll` — semantically the same as a `<div>` panel with that overflow value. The element type exists for clarity in the Hierarchy.
