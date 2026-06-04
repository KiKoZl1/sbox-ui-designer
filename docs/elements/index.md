---
layout: default
title: Element reference
nav_order: 4
has_children: true
permalink: /elements/
---

# Element reference

Every element type in the palette has its own page covering:

- **What it is** — concept + when to use
- **Properties** — type-specific fields (beyond Common / Transform / Appearance)
- **Generated output** — what Razor / SCSS comes out
- **Tips & gotchas**

Every element shares the [Common, Transform, Appearance]({% link user-guide/details-panel.md %}#sections) sections. See [Concepts]({% link concepts/index.md %}) for the meaning of Layout modes, Anchors, Variables, Bindings, Events, Interactive States, etc.

<div class="section-grid" markdown="0">
  <a class="section-card" href="{% link elements/canvas.md %}">
    <span class="card-tag">LAYOUT ROOT</span>
    <h3>Canvas</h3>
    <p>Root element — every <code>.sui</code> document has exactly one. Always fills <code>BaseWidth × BaseHeight</code>.</p>
  </a>
  <a class="section-card" href="{% link elements/panel.md %}">
    <span class="card-tag">CONTAINER</span>
    <h3>Panel</h3>
    <p>Generic <code>&lt;div&gt;</code> container with Background, Border, Border Radius, and the standard Transform + Appearance fields.</p>
  </a>
  <a class="section-card" href="{% link elements/horizontal-box.md %}">
    <span class="card-tag">CONTAINER</span>
    <h3>HorizontalBox</h3>
    <p>A flex container — children pack horizontally in a row.</p>
  </a>
  <a class="section-card" href="{% link elements/vertical-box.md %}">
    <span class="card-tag">CONTAINER</span>
    <h3>VerticalBox</h3>
    <p>A flex container — children stack vertically in a column.</p>
  </a>
  <a class="section-card" href="{% link elements/overlay.md %}">
    <span class="card-tag">CONTAINER</span>
    <h3>Overlay</h3>
    <p>A flex container that creates a <code>position: relative</code> context so absolutely-positioned children anchor to it.</p>
  </a>
  <a class="section-card" href="{% link elements/grid.md %}">
    <span class="card-tag">CONTAINER</span>
    <h3>Grid</h3>
    <p>A wrapped-flex grid for laying out children in <code>Columns × Rows</code>. Uses <code>display: flex; flex-wrap: wrap</code>.</p>
  </a>
  <a class="section-card" href="{% link elements/scroll-panel.md %}">
    <span class="card-tag">CONTAINER</span>
    <h3>ScrollPanel</h3>
    <p>A flex container with <code>overflow: scroll</code> so its children become scrollable when they exceed the bounds.</p>
  </a>
  <a class="section-card" href="{% link elements/image.md %}">
    <span class="card-tag">VISUAL</span>
    <h3>Image</h3>
    <p>Renders an image asset. Uses <code>background-image</code> internally so it can clip with <code>border-radius</code> and tint.</p>
  </a>
  <a class="section-card" href="{% link elements/text.md %}">
    <span class="card-tag">VISUAL</span>
    <h3>Text</h3>
    <p>Renders text. Generates a <code>&lt;label&gt;</code> element in Razor.</p>
  </a>
  <a class="section-card" href="{% link elements/button.md %}">
    <span class="card-tag">VISUAL</span>
    <h3>Button</h3>
    <p>A clickable region with a centered text label and full interactive-state support — hover, pressed, disabled, focused, transitions, sounds.</p>
  </a>
  <a class="section-card" href="{% link elements/progress-bar.md %}">
    <span class="card-tag">VISUAL</span>
    <h3>ProgressBar</h3>
    <p>A fill bar for stats (health, stamina, mana, hunger). Every visual property is bindable so you can drive the bar from a Variable.</p>
  </a>
  <a class="section-card" href="{% link elements/text-entry.md %}">
    <span class="card-tag">INPUT WIDGET</span>
    <h3>TextEntry</h3>
    <p>Single-line text input backed by <code>Sandbox.UI.TextEntry</code>. The first SUI widget that <strong>reads</strong> user input back into a Variable via TwoWay.</p>
  </a>
  <a class="section-card" href="{% link elements/slider.md %}">
    <span class="card-tag">INPUT WIDGET</span>
    <h3>Slider</h3>
    <p>Horizontal slider with author-controlled track / fill / thumb / tooltip. Fully custom markup per D-022.</p>
  </a>
  <a class="section-card" href="{% link elements/toggle.md %}">
    <span class="card-tag">INPUT WIDGET</span>
    <h3>Toggle</h3>
    <p>A boolean checkbox backed by <code>Sandbox.UI.Checkbox</code>. V1.5 ships only the default visual (pill / switch variants deferred).</p>
  </a>
  <a class="section-card" href="{% link elements/dropdown.md %}">
    <span class="card-tag">INPUT WIDGET</span>
    <h3>DropDown</h3>
    <p>A selection dropdown backed by <code>Sandbox.UI.DropDown</code>. TwoWay binds against <code>Value</code> (int via <code>Option.Value</code> index).</p>
  </a>
  <a class="section-card" href="{% link elements/sui-reference.md %}">
    <span class="card-tag">COMPOSITION</span>
    <h3>SuiReference</h3>
    <p>Embeds another <code>.sui</code> document by GUID. The composition element — paints the child's tree inside its rect, ForEach iterates a List Variable.</p>
  </a>
  <a class="section-card" href="{% link elements/inventory-grid.md %}">
    <span class="card-tag">INVENTORY</span>
    <h3>InventoryGrid</h3>
    <p>Slot grid for inventory UIs — same mechanics as Grid but semantically intended to hold <code>InventorySlot</code> children.</p>
  </a>
  <a class="section-card" href="{% link elements/inventory-slot.md %}">
    <span class="card-tag">INVENTORY</span>
    <h3>InventorySlot</h3>
    <p>Single inventory slot — frame, optional preview icon + stack count. Interactive (hover / pressed / disabled / focused).</p>
  </a>
  <a class="section-card" href="{% link elements/item-icon.md %}">
    <span class="card-tag">INVENTORY</span>
    <h3>ItemIcon</h3>
    <p>Standalone item icon — same rendering as InventorySlot but without the slot frame. For floating rewards, tooltip previews, drag-ghosts.</p>
  </a>
  <a class="section-card" href="{% link elements/hotbar.md %}">
    <span class="card-tag">INVENTORY</span>
    <h3>Hotbar</h3>
    <p>A single-row inventory bar — semantically a slot container that doesn't wrap to a second row.</p>
  </a>
</div>
