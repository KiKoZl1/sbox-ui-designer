---
layout: default
title: Sandbox.UI CSS limitations
parent: Concepts
nav_order: 13
---

# Sandbox.UI CSS limitations
{: .no_toc }

The s&box runtime UI uses a CSS subset — what works in your browser DevTools doesn't always work in `.razor.scss`. This page catalogs the silent gotchas so you don't lose half a day chasing styles that the parser drops without warning.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Why this exists

`Sandbox.UI` parses a CSS subset roughly comparable to "Yoga flex + a hand-picked visual prop set". Web CSS that runs your browser through layout (`display: grid`, `position: fixed`, `::before`) does nothing here — silently. The SUI Designer's [allowed-css whitelist]({% link reference/allowed-css.md %}) catches this on the generator side, but `.User.scss` files you author by hand are not validated. The gotchas below are the rough edges you hit even when every property is on the whitelist.

These are documented because at least one bug per V1.5 milestone traced back to one of them. PRDs 21 and 25 reference the Slider rebuild that was triggered specifically by gotcha #1 below.

---

## Gotcha 1 — `> > >` selectors silently dropped past two levels

The most expensive gotcha of V1.5 development. PRD 21 § 11 #1 / DEVIATIONS D-022.

The Sandbox.UI CSS parser **silently drops** selector chains deeper than two `>` combinators. The bytes parse, the file loads, but the deeper rule never applies. You see a rule in DevTools (mentally), you don't see it in the engine.

```scss
// Works — two levels deep
.card > .header > .title { color: red; }

// Silently broken — three+ levels deep, the rule is dropped
.card > .header > .row > .label { color: red; }
.card > .header > .row > .label > .icon { color: red; }
```

**Workaround:** use descendant selectors (space, no `>`) or a single deep class:

```scss
.card .icon { color: red; }                  // descendant — fine
.card-row-label-icon { color: red; }         // explicit class — always fine
```

V1.5's Slider widget was rebuilt from scratch (D-022) because the engine `SliderControl`'s tooltip selector chain hit this limit and the author-side color override was silently dropped. The SCSS generator now caps emitted chains at two levels.

---

## Gotcha 2 — `!important` does not exist

The CSS `!important` flag is **not parsed**. The rule is treated as if `!important` were absent. The whitelist explicitly excludes the syntax.

```scss
// Has no special effect — the engine sees it as ".x { width: 100px; }"
.x { width: 100px !important; }
```

**Workaround:** use a higher-specificity selector, or restructure your classes so the override rule wins naturally.

---

## Gotcha 3 — `calc()` subtraction is fragile

The s&box parser accepts `calc()` but **subtraction with a literal on the right-hand side** is unreliable across versions. Multiplication and addition tend to work; mixed subtraction has surfaced edge cases (PRD 21 spike, M2-K7-bugfix).

```scss
.x { width: calc(100% - 24px); }    // sometimes works
.x { width: calc(100% + 24px); }    // works
```

**Workaround:** prefer flex layouts that compute remaining space (`flex-grow: 1`) instead of fixed-pixel subtraction. The SUI Designer emits flex-based sizing for this reason — it's parser-stable.

---

## Gotcha 4 — Implicit class on tag selectors not honored

Web browsers let you write `Button { color: red }` and target every `<Button>` tag. The s&box parser **does not match this for SUI's generated Panel subclasses**. Every emit attaches an explicit class (`sui-el-<id>` plus the user's `Style.ClassName`), and the SCSS rules target the class, not the tag.

```scss
// Does NOT cascade to SUI-generated <CheckboxPanel> tags
Checkbox { background-color: red; }

// Works — targets the explicit class the SCSS emit attaches
.sui-el-mute-toggle { background-color: red; }
.sui-toggle-A1 .checkbox { background-color: red; }
```

This is why every element gets a unique `sui-el-<id>` class — to provide a stable, parser-honored handle for `.User.scss` overrides.

---

## Gotcha 5 — `display` accepts only `flex` and `none`

`display: grid` / `display: block` / `display: inline-flex` are **forbidden values** (the Designer's whitelist rejects them at generation time). The runtime UI is flex-only.

```scss
// Forbidden
.x { display: grid; }
.x { display: block; }

// Allowed
.x { display: flex; }
.x { display: none; }
```

For grid-style layouts use the `Grid` SUI element (which is flex-based internally) or nested HorizontalBox / VerticalBox.

---

## Gotcha 6 — `position` accepts only `static`, `relative`, `absolute`

`position: fixed` and `position: sticky` are **forbidden**. There's no fixed-relative-to-viewport mechanism — use `position: absolute` with explicit anchoring to achieve the same effect.

```scss
// Forbidden
.x { position: fixed; }

// Allowed
.x { position: absolute; left: 0; top: 0; }
```

---

## Gotcha 7 — Pseudo-elements `::before` / `::after` don't exist

The engine has no `content` property and no `::before` / `::after`. Decorations that web CSS handles via pseudo-elements must be authored as real elements in the SUI tree.

```scss
// Does nothing — pseudo-elements are not supported
.button::before { content: "→"; }
```

**Workaround:** drop a `Text` element next to your Button in the Hierarchy, or use a `background-image` if it's a static decoration.

---

## Gotcha 8 — Pseudo-classes work in `.User.scss` but the Designer doesn't expose them

`:hover`, `:active`, `:focus`, `:disabled` are honored at runtime, but the SUI Designer's Details panel never emits them. They're only available via hand-written `.User.scss`. The V1.5 M3.5 Interactive States system (D-021) takes the place of `:hover` for the polished-button case — under the hood it emits class swaps the canvas can preview, but `.User.scss` can still drop `:hover` for one-off styling.

```scss
// MyButton.User.scss — pseudo-classes in the auto-loaded sidecar
.sui-el-my-button:hover { background-color: lighter; }
.sui-el-my-button:active { transform: scale(0.98); }
```

See [User SCSS customization]({% link workflows/user-scss-customization.md %}).

---

## Gotcha 9 — CSS variables (`--var`) not supported

`var(--my-color)` lookups are not parsed. Use SCSS `$variables` instead — these are compiled away at SCSS time (before the engine sees the CSS).

```scss
// Not parsed — the engine sees "color: var(--accent)" and ignores it
.x { color: var(--accent); }

// Use SCSS $vars — compiled away before runtime
$accent: #4ade80;
.x { color: $accent; }
```

---

## Gotcha 10 — `@media`, `@supports`, `@import` of regular CSS are absent

There's no media-query mechanism. Resolution scaling is driven by `ScreenPanel.AutoScreenScale` at the engine level, not by CSS queries. `@supports` doesn't exist. The SCSS `@import` works at the SCSS compilation stage — it's NOT a runtime CSS import.

---

## What IS reliably supported

The conservative subset that works everywhere:

- **Flex layout** — `display: flex`, `flex-direction`, `flex-grow`, `flex-shrink`, `justify-content`, `align-items`, `gap`, `flex-wrap`. Solid.
- **Box model** — `width`, `height`, `min-*`, `max-*`, `margin`, `padding`, `border`, `border-radius`. Solid.
- **Visual** — `background-color`, `background-image`, `background-size`, `opacity`, `box-shadow`. Solid.
- **Text** — `color`, `font-size`, `font-weight`, `font-family`, `text-align`, `letter-spacing`, `line-height`. Solid.
- **Motion** — `transition` (property / duration / timing-function), `transform`. Solid.
- **s&box extensions** — `sound-in`, `sound-out`, `pointer-events`, `cursor`. Solid.

The [allowed-css reference]({% link reference/allowed-css.md %}) is the authoritative whitelist.

---

## Debugging tip — the silent-drop trick

If a `.User.scss` rule is "doing nothing", the parser probably silently dropped it. Quick checks:

1. **Selector depth** — count the `>` combinators. More than two? Gotcha 1.
2. **Property allowed?** — open [allowed-css]({% link reference/allowed-css.md %}). If it's not there, the runtime ignores it.
3. **Pseudo-element?** — `::before` / `::after` aren't real. Refactor to a real SUI element.
4. **`!important`?** — drop it; bump specificity instead.
5. **`calc()` subtraction?** — try flex-grow.
6. **CSS var?** — switch to SCSS `$var`.

When in doubt, copy the rule into the generated SCSS (not `.User.scss`) and recompile — if the canvas / runtime now responds, the issue was the parser tripping on something `.User.scss` didn't normalise.

---

## See also

- [Allowed CSS reference]({% link reference/allowed-css.md %}) — the authoritative whitelist
- [Styling concept]({% link concepts/styling.md %}) — what the Designer exposes
- [User SCSS customization workflow]({% link workflows/user-scss-customization.md %}) — adding rules outside the whitelist
- [`Code/Generation/SuiAllowedPropertyList.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Generation/SuiAllowedPropertyList.cs) — source of truth for the whitelist
