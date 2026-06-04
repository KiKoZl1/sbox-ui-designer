---
layout: default
title: Known issues
parent: Reference
nav_order: 5
---

# Known issues
{: .no_toc }

Open bugs and limitations in V1.0, with workarounds where available. The authoritative list is [ISSUES.md](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md) in the source repo.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## ISSUE-004 — `<label>` background-color rgba alpha ignored in runtime {#issue-004}

**Severity:** major · **Status:** investigating

### Symptom

Text elements (which the generator emits as `<label>`) with `background-color: rgba(r,g,b,a)` where `a < 1` render the background as **fully opaque** in Test in Play and the runtime preview. The alpha is ignored.

Canvas (paint-based) renders correctly — `rgba(34, 197, 94, 0.12)` appears as a very faint green over a dark sidebar.

In runtime: the same element appears as solid/saturated green, as if `alpha = 1.0`.

**Confirmed reproduction:** `quest_log.sui` samples (Drink from the River / Light a Campfire) and `loot_pickup.sui` — every `<label>` with `rgba` alpha below ~0.5 renders solid in runtime.

### Workaround

Wrap the Text in a Panel and put the background on the Panel:

**Before** (broken in runtime):
```
Text (Background: rgba(34, 197, 94, 0.12), Text: "Quest complete")
```

**After** (works):
```
Panel (Background: rgba(34, 197, 94, 0.12))
└── Text (Text: "Quest complete")
```

The Panel emits as `<div>` which respects rgba alpha correctly. The Text inside has a transparent background.

### Status

Filed under investigation. The root cause is likely a special handling of `<label>` background in the Sandbox.UI parser. We're choosing between two fixes:

1. Make the generator emit `<div>` when a Text element has a `background-color` set.
2. Wrap label in a div automatically: `<div class="..."><label>text</label></div>`.

Either way: a future SUI version will handle this transparently.

---

## ISSUE-005 — PreviewCount badges not emitted in runtime {#issue-005}

**Severity:** minor · **Status:** open

### Symptom

`InventorySlot` and `ItemIcon` elements have a `PreviewCount` prop (e.g. `"20"`, `"3"`). The canvas draws this count as an overlay in the slot's bottom-right corner.

**In runtime Test in Play, the count doesn't appear.** The Razor generator doesn't emit the `<label>` child with the count text.

Result: canvas shows "20" on top of a berry stack; runtime shows only the icon.

### Workaround

For previews / mockups, this is cosmetic only. For real gameplay UIs, you'll be driving the count from your C# code anyway (binding the count text to `ItemStack.Count` or similar), so the static `PreviewCount` isn't load-bearing.

If you need the badge visible in Test in Play for screenshot purposes, add a manual Text child:

```
InventorySlot
└── ItemIcon
└── Text (Anchor: BottomRight, Text: "20", FontWeight: Bold, Color: white)
```

### Status

Tracked. Future fix: generator adds an automatic count `<label>` when `PreviewCount > 0`, plus matching SCSS for absolute positioning.

---

## Limitations (not bugs)

These are deliberate V1 scope limits, not bugs to be fixed:

### `.User.scss` not visible in Test in Play

Test in Play runs the generator in `Preview` mode which skips the `@import "<Name>.User.scss"`. The sidecar doesn't exist at the preview cache path.

**Impact:** hovers, animations, shadows in `.User.scss` only appear after a real Compile + Play.

**Why:** importing a non-existent file silently breaks SCSS compilation at runtime. The fix is to emit the import only in Final mode.

### Test in Play has no scene auto-restore

After clicking Stop, you remain on `preview_stage.scene`. There's no API in s&box for the addon to safely restore your previous scene.

**Workaround:** open your original scene manually from the Asset Browser.

### One Play session at a time

You can't run Test in Play on a `.sui` while another play session is active. Stop the existing one first.

**Why:** s&box doesn't support nested play sessions.

### `flex-grow` / `flex-shrink` / `flex-basis` not honored by canvas

The canvas's flex solver doesn't implement flex-grow. Children always use their intrinsic size. The runtime CSS engine DOES honor flex-grow.

**Impact:** if you set `flex-grow: 1` via `.User.scss`, canvas and runtime will diverge.

**V2:** full flex algorithm.

### `align-self` not honored

Children in a flex container inherit the parent's `align-items`. Per-child override isn't exposed in the inspector.

**Workaround:** override in `.User.scss`:

```scss
MyHud {
  .important-item { align-self: flex-end; }
}
```

### `display: grid` is forbidden

s&box's Yoga engine doesn't support CSS Grid. SUI's Grid element uses wrapped flex.

**Workaround:** use the `Grid` element type — it does what you want.

### Multi-document workspace doesn't exist

One `.sui` open per window. To work on two HUDs at once, open two SUI Designer windows.

---

## Closed issues (historical)

The full ISSUES.md in the source repo tracks resolved bugs with their fix history:

- **ISSUE-001** — ColorPicker SV box stale on hue change — **resolved** by replacing the editor's color picker with a custom SUI implementation (`SuiColorPickerPopup`).
- **ISSUE-002** — Text vertical alignment divergence between canvas and runtime — **resolved** by introducing `SuiTextSizeMode { Auto, Fixed, AutoHeightWrap }` and per-mode rendering.
- **ISSUE-003** — Editor color picker instability (5 related symptoms) — **resolved** alongside ISSUE-001 via the custom picker.

See [ISSUES.md](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md) for the full repro + design discussion that led to each fix.

## Reporting new issues

Found a bug not on this list? Open an issue on [GitHub](https://github.com/KiKoZl1/sbox-ui-designer/issues) with:

1. A repro `.sui` file (attach or paste contents).
2. Expected vs actual behavior.
3. Whether it shows in canvas, Test in Play, or both.
4. Engine console output if compile-related.

## See also

- [Test in Play limitations]({% link workflows/test-in-play.md %}#limitations)
- [User SCSS customization]({% link workflows/user-scss-customization.md %}) — for things `.User.scss` lets you sidestep
