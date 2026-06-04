---
layout: default
title: Allowed CSS
parent: Reference
nav_order: 2
---

# Allowed CSS
{: .no_toc }

The exact list of CSS properties the SUI generator will emit. Anything outside this list is a hard error.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Why a whitelist?

The s&box runtime UI layer (Yoga + a CSS subset) only parses a subset of CSS. Properties outside that subset are **silently ignored** at runtime — which means you can write web-style CSS that previews fine in browser tooling but does nothing in s&box.

SUI Designer's whitelist makes this explicit. The generator validates every property before emitting it; if it's not on the list, you get a build-time error. No silent failures.

Source of truth: [Code/Generation/SuiAllowedPropertyList.cs](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Generation/SuiAllowedPropertyList.cs).

## Allowed properties

### Display & layout

```
display
flex-direction, flex-wrap, flex-grow, flex-shrink, flex-basis, flex
gap, row-gap, column-gap
justify-content, align-items, align-self
position
left, top, right, bottom
width, height, min-width, min-height, max-width, max-height
margin, margin-left, margin-top, margin-right, margin-bottom
padding, padding-left, padding-top, padding-right, padding-bottom
overflow, z-index
```

### Visual

```
background-color
background-image, background-size, background-position, background-repeat
background-image-tint
border, border-color, border-width, border-radius
box-shadow, text-shadow
opacity
```

### Text

```
color
font-family, font-size, font-weight, font-style
text-align, text-transform, text-decoration
letter-spacing, line-height, white-space
text-overflow
```

### Motion

```
transform
transition, transition-duration, transition-property, transition-timing-function, transition-delay
animation, animation-name, animation-duration, animation-iteration-count, animation-timing-function
```

### s&box-specific

```
pointer-events
sound-in, sound-out
cursor
```

`sound-in` / `sound-out` are engine extensions — play a sound when the element appears or disappears.

`cursor` is in the engine CSS subset (verified against `game/addons/base/code/Styles/base.scss`); common values like `pointer` work directly, project-specific named cursors should be quoted (`cursor: "bug-hover";`). V1.5 M3.5 (D-021) added cursor support to the Button polish pass.

## Hard-blocked properties

These are properties web tutorials commonly suggest but the engine does NOT support:

```
grid-template-columns, grid-template-rows, grid-template-areas, grid-area
grid-row, grid-column, grid-gap, grid-row-gap, grid-column-gap
float, clear
```

If you write any of these in `.User.scss`, the engine silently ignores them. The generator's validator catches them BEFORE write — but `.User.scss` isn't validated, so you're on your honor there.

## Disallowed values

Some properties exist but only accept a subset of values:

### `display`

| Value | Status |
|---|---|
| `flex` | Allowed |
| `none` | Allowed |
| `grid` | **Forbidden** |
| `block` | **Forbidden** |
| `inline`, `inline-block`, `inline-flex` | **Forbidden** |
| `table`, `table-cell`, `contents` | **Forbidden** |

The s&box runtime treats every element as flex or none. `block` doesn't exist; there's no "regular" non-flex layout.

### `position`

| Value | Status |
|---|---|
| `static` | Allowed |
| `relative` | Allowed |
| `absolute` | Allowed |
| `fixed` | **Forbidden** |
| `sticky` | **Forbidden** |

Use `position: absolute` with appropriate anchoring to achieve "fixed"-like effects.

## Notable absences

Web CSS that doesn't exist here:

- **`@media` queries** — runtime UI is screen-resolution driven via `ScreenPanel.AutoScreenScale`, not media queries.
- **`@supports`** — there's no feature-query mechanism.
- **Pseudo-elements** like `::before`, `::after` — not supported.
- **`content` property** — pseudo-element only; not supported.
- **`filter`** — no backdrop blur, no drop-shadow filter (use `box-shadow` instead).
- **`backdrop-filter`** — not supported.
- **CSS variables (`--var`)** — not supported. Use SCSS `$variables` instead (compiled away at SCSS time).

If a property you need isn't on the list, it's almost certainly not parsed by the engine. If you're certain the engine accepts it (e.g. you saw it in a Facepunch first-party UI), open an [issue](https://github.com/KiKoZl1/sbox-ui-designer/issues) asking for it to be added to the whitelist.

## Pseudo-classes (allowed in `.User.scss`)

The generator doesn't emit pseudo-classes directly — they're not exposed in the Details panel. But in `.User.scss` you can use:

```scss
.my-button:hover { ... }
.my-button:active { ... }
.my-input:focus { ... }
.toggle.active { ... }      // not a pseudo, but commonly used with SetClass
```

s&box supports `:hover`, `:active`, `:focus`, `:disabled` — verified at runtime.

## The `Validate(property, value)` API

For tooling, the validator exposes:

```csharp
public static string Validate( string property, string value );
// returns null if accepted, or an error message string
```

Used by `SuiScssGenerator` before every emission. Failures surface as Errors in the [Compile Results panel]({% link user-guide/compile-results.md %}).

## See also

- [Styling concept]({% link concepts/styling.md %}) — what's exposed in the Details panel
- [User SCSS customization]({% link workflows/user-scss-customization.md %}) — adding properties not in the panel
- [Generator pipeline]({% link architecture/generator.md %}) — where validation runs
