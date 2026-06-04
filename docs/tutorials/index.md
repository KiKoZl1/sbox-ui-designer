---
layout: default
title: Tutorials
nav_order: 9
has_children: true
permalink: /tutorials/
---

# Tutorials

End-to-end walkthroughs that go from empty document to a working UI. Each tutorial covers a real scenario; you can also load the matching sample from `Assets/SuiSamples/` in the source repo.

## V1.5 worked examples

- [Settings screen with input widgets]({% link tutorials/settings-screen.md %}) — TextEntry, Slider, Toggle, DropDown + `Apply` API for Manual commits
- [Health HUD with converters]({% link tutorials/health-hud-with-converters.md %}) — bind `Health` → ProgressBar + `Compose` label + custom `[SuiConverter]` for low-HP tint

## V1.0 (still valid)

- [Survival HUD]({% link tutorials/survival-hud.md %}) — Health, hunger, stamina bars + ammo counter + minimap frame
- [Inventory screen]({% link tutorials/inventory-screen.md %}) — Backpack grid + equipment slots + drag-and-drop scaffolding
- [Death modal]({% link tutorials/death-modal.md %}) — Full-screen overlay with respawn timer + buttons

These are written for someone who has read [Getting Started]({% link getting-started/index.md %}) but hasn't built a full UI yet.

If you'd rather learn by remix, the [Sample index]({% link reference/sample-index.md %}) catalogs every `.sui` shipped with the project + what feature each one exercises.
