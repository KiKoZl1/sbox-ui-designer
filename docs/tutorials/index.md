---
layout: default
title: Tutorials
nav_order: 9
has_children: true
permalink: /tutorials/
---

# Tutorials

End-to-end walkthroughs that go from empty document to a working UI. Each tutorial covers a real scenario; you can also load the matching sample from `Assets/SuiSamples/` in the source repo.

These are written for someone who has read [Getting Started]({% link getting-started/index.md %}) but hasn't built a full UI yet. If you'd rather learn by remix, the [Sample index]({% link reference/sample-index.md %}) catalogs every `.sui` shipped with the project + what feature each one exercises.

## Want a quick demo?

Each concept covered in the tutorials below pairs with a working showcase sample. V1.5 ships **16 showcase samples** — 5 beginner ones that isolate a single concept each (`empty_canvas`, `label_clock`, `health_bar`, `counter_button`, `toggle_pause`), 3 intermediate "wow" samples that wire multiple features together on realistic surfaces (`settings_full`, `inventory_grid_full`, `survival_hud_aaa`), and 8 advanced samples that combine the full runtime into game-flow surfaces (`death_respawn_modal`, `quest_journal`, `boss_hp_bar`, `chat_panel`, `loadout_selector`, `dialog_system`, `drag_drop_inventory`, `notification_toast_queue`). Browse them via [Showcase samples]({% link reference/sample-index.md %}) — drop the companion Component on a GameObject and you have a working UI in seconds.

<div class="section-grid" markdown="0">
  <a class="section-card" href="{% link tutorials/survival-hud.md %}">
    <span class="card-tag">TUTORIAL</span>
    <h3>Survival HUD</h3>
    <p>Classic survival HUD — health / hunger / stamina bars bottom-left, ammo bottom-right, minimap top-right. ~20 minutes.</p>
  </a>
  <a class="section-card" href="{% link tutorials/inventory-screen.md %}">
    <span class="card-tag">TUTORIAL</span>
    <h3>Inventory screen</h3>
    <p>Full-screen inventory with a 6×4 backpack grid, equipment slot column, and hotbar. ~30 minutes.</p>
  </a>
  <a class="section-card" href="{% link tutorials/death-modal.md %}">
    <span class="card-tag">TUTORIAL</span>
    <h3>Death modal</h3>
    <p>Full-screen "you died" overlay with a respawn countdown and two action buttons. ~10 minutes.</p>
  </a>
  <a class="section-card" href="{% link tutorials/settings-screen.md %}">
    <span class="card-tag">TUTORIAL</span>
    <h3>Settings screen</h3>
    <p>Exercises every V1.5 M4 input widget — <code>TextEntry</code>, <code>Slider</code>, <code>Toggle</code>, <code>DropDown</code> + the <strong>Apply API</strong>. ~20 minutes.</p>
  </a>
  <a class="section-card" href="{% link tutorials/health-hud-with-converters.md %}">
    <span class="card-tag">TUTORIAL</span>
    <h3>Health HUD with converters</h3>
    <p>Health bar + "75 / 100 HP" label, both driven by Variables through converter chains. ~15 minutes.</p>
  </a>
</div>
