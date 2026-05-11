---
layout: default
title: Text
parent: Element reference
nav_order: 3
---

# Text

Renders text. Generates a `<label>` element in Razor.

## Properties (Text section)

| Field | Default | Notes |
|---|---|---|
| **Text** | `""` | Literal text. Use `{placeholders}` for dynamic values driven by bindings |
| **Font Family** | (project default) | Font face name (e.g. `Inter`, `Roboto`) |
| **Font Size** | 16 | px |
| **Font Weight** | Normal | Normal / Bold / Light / Medium / SemiBold / ExtraBold |
| **Color** | `#ffffff` | Text fill color |
| **Text Align** | Left | Left / Center / Right / Justify |
| **Vertical Align** | Top | Top / Center / Bottom — only meaningful in Fixed text size mode |
| **Text Size Mode** | Auto | Auto / Fixed / AutoHeightWrap |
| **Auto Wrap Text** | off | When on, lines wrap at the element's width |
| **Wrap Text At** | 0 | Max width before wrap. Used with Auto Wrap Text |
| **Letter Spacing** | 0 | px |
| **Line Height** | (auto) | px or unitless |
| **Text Overflow** | Clip | Clip / Ellipsis / None |

## Text Size Mode

The most consequential choice. Controls how W/H interact with content:

| Mode | Behavior |
|---|---|
| **Auto** | Width and Height auto-size to content. Single line, no wrap. Use for short labels |
| **Fixed** | User-specified W and H. Supports Text Align + Vertical Align for positioning inside the box |
| **AutoHeightWrap** | User-specified W, Height grows with wrapped lines. Horizontal alignment supported, vertical not (always top-aligned) |

The generator emits the right SCSS depending on the mode:

- **Auto** → `white-space: nowrap; width: auto; height: auto;`
- **Fixed** → emits the user's W/H + `display: flex; flex-direction: column; justify-content: <VerticalAlign>;`
- **AutoHeightWrap** → `white-space: normal; height: auto;`

## Generated output

```html
<label class="text sui-text">My HUD</label>
```

```scss
.sui-text {
  width: 400px;
  height: 48px;
  font-size: 32px;
  font-weight: 700;
  color: #ffffff;
  text-align: center;
  display: flex;
  flex-direction: column;
  justify-content: center;
}
```

## Gotchas

- **Background-color alpha on `<label>`** — see [ISSUE-004](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md#issue-004) — rgba alpha appears to render solid in some runtime contexts. Workaround: wrap the Text in a Panel and put the background on the panel.
- **Auto mode + Width/Height set** — pre-2026-05-08 documents had a bug where Auto with explicit W/H rendered cropped. Migration auto-converts to Fixed on load. New documents shouldn't hit this.
