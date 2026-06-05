using Sandbox;
using Sandbox.UI;
using SboxUiDesigner.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>inventory_grid_full</c> showcase sample.
///
/// <para>Drop this on a GameObject in any scene, press Play, and a 6×4
/// inventory card appears in the middle of the screen with four seeded
/// items (sword, health potion, bread, gold coin). Hovering a filled
/// slot reveals a tooltip with the item name + count; left-clicking
/// logs a "select" event; right-clicking removes the item from the
/// slot. The header subtitle (<c>"4 / 24 slots"</c>) updates live as
/// the inventory mutates.</para>
///
/// <para>The UI lives entirely in <c>inventory_grid_full.sui</c>; this
/// Component owns the item list, the per-slot click router, and the
/// three Variables the wrapper exposes (<c>SlotCountText</c>,
/// <c>ItemTooltip</c>, <c>TooltipVisible</c>). The <c>BackpackGrid</c>
/// element is <b>exposed as a Variable</b> so we can reach into the
/// rendered Panel and wire <c>OnMouseOver</c> / <c>OnMouseOut</c> /
/// <c>OnClick</c> on every InventorySlot child after mount.</para>
/// </summary>
public sealed class InventoryGridFullController : Component
{
	/// <summary>One inventory entry. Plain POCO so the Component inspector
	/// lets you edit the seed list straight from the scene — <c>List&lt;T&gt;</c>
	/// of records is a valid <c>[Property]</c> target on a Component (it just
	/// isn't a valid SUI Variable type, which is why this list lives in C#
	/// and not in the <c>.sui</c>).</summary>
	public sealed class ItemEntry
	{
		[Property] public string Name { get; set; } = "";
		/// <summary>Single-character glyph rendered in the centre of the slot
		/// (e.g. "⚔" / "✚" / "$"). The companion <see cref="Color"/> hex
		/// paints the slot background so each item reads at a glance without
		/// shipping PNG assets. Swap this for <c>IconPath</c> if you wire a
		/// real <c>background-image</c> on the slot — see the README.</summary>
		[Property] public string Glyph { get; set; } = "";
		/// <summary>Hex string (e.g. <c>"#7c3aed"</c>) applied to the slot's
		/// <c>BackgroundColor</c> at wire-up time. Parsed via
		/// <c>Sandbox.Color.Parse</c>; falls back to no override if parsing
		/// fails so a bad string can't crash the wire-up loop.</summary>
		[Property] public string Color { get; set; } = "";
		[Property] public int Count { get; set; } = 1;
	}

	/// <summary>Generated wrapper instance. Exposes <c>Hud.SlotCountText</c>,
	/// <c>Hud.ItemTooltip</c>, <c>Hud.TooltipVisible</c> (the three Variables)
	/// and — because we ticked "Expose as Variable" on the grid in the
	/// designer — <c>Hud.View?.BackpackGrid</c> (the renderer-side Panel ref).</summary>
	[Property] public InventoryGridFull Hud { get; set; } = new();

	/// <summary>Total slot capacity. Matches the grid's 6 columns × 4 rows
	/// in the .sui. Change both at once if you resize the grid.</summary>
	[Property] public int SlotCapacity { get; set; } = 24;

	/// <summary>Seed items written into slots 0..N-1 at <see cref="OnStart"/>.
	/// Defaults match the canvas preview so the running game looks identical
	/// to the editor on the first frame. Edit from the Inspector to reshape
	/// the demo without touching code.</summary>
	[Property]
	public List<ItemEntry> StartingItems { get; set; } = new()
	{
		new ItemEntry { Name = "Iron Sword",    Glyph = "⚔", Color = "#7c3aed", Count = 1  },
		new ItemEntry { Name = "Health Potion", Glyph = "✚", Color = "#ef4444", Count = 5  },
		new ItemEntry { Name = "Bread",         Glyph = "◉", Color = "#d97706", Count = 12 },
		new ItemEntry { Name = "Gold Coin",     Glyph = "$", Color = "#facc15", Count = 99 },
	};

	// ── Runtime state ────────────────────────────────────────────────────
	// _items has exactly SlotCapacity entries; null = empty slot. We don't
	// expose this as a SuiVariable because List<custom-POCO> isn't in the
	// V1.5 variable matrix (which only covers string/int/bool/float/Color).
	// All view updates are pushed through (a) the three Variables for the
	// header/tooltip, and (b) imperative Panel mutation through @ref for
	// per-slot icon swaps.
	private List<ItemEntry> _items;

	// True once we've walked the BackpackGrid children and assigned a click
	// router to each one. Re-running the wire-up is harmless but wasteful;
	// the flag keeps OnUpdate idle most frames.
	private bool _slotsWired;

	protected override void OnStart()
	{
		// Seed the runtime list — copy StartingItems into a SlotCapacity-sized
		// slot array so unused tail positions read as null (= empty cell).
		_items = new List<ItemEntry>( SlotCapacity );
		for ( int i = 0; i < SlotCapacity; i++ )
		{
			_items.Add( i < StartingItems.Count ? StartingItems[i] : null );
		}

		// Wire the grid-level Code-mode events BEFORE Show() so SyncFieldsTo
		// carries the delegates into the renderer Panel on first mount. The
		// .sui declares OnClick + OnRightClick on the grid; we keep the
		// handlers as a fallback in case per-slot wiring (below) fails to
		// resolve a child. In V1.5 OnRightClick is wired to plain onclick
		// (no button filter — known M3 gap) so the right-click handler also
		// fires on left-clicks; we no-op it for that reason.
		Hud.OnGridClick      = OnGridClick;
		Hud.OnGridRightClick = OnGridRightClick;

		// MouseOnly so the player can click slots and hover for tooltips
		// without the inventory grabbing keyboard focus from gameplay.
		Hud.Show( GameObject, SuiInputMode.MouseOnly );

		// Push the initial subtitle string so frame zero shows the seeded
		// fill count instead of the default "0 / 24 slots".
		RefreshSubtitle();

		// Slots can't be wired here — Hud.View?.BackpackGrid is null until
		// the Razor renderer runs at least once. OnUpdate picks it up on
		// the first frame the view is alive.
	}

	protected override void OnUpdate()
	{
		// One-shot late wire-up: the BackpackGrid Panel ref is populated by
		// Razor on the first render pass, which lands after OnStart. Once we
		// see it, walk every InventorySlot child and assign hover / click
		// routers that know the slot index from its position in the list.
		if ( !_slotsWired )
		{
			var grid = Hud.View?.BackpackGrid;
			if ( grid == null || grid.Children == null ) return;

			var children = grid.Children.ToList();
			if ( children.Count == 0 ) return;

			for ( int idx = 0; idx < children.Count; idx++ )
			{
				var slotPanel = children[idx];
				if ( slotPanel == null ) continue;
				int capturedIndex = idx; // capture before the closure

				// Hover in → show tooltip with the item name + count.
				slotPanel.AddEventListener( "onmouseover", () => OnSlotHover( capturedIndex ) );
				// Hover out → clear the tooltip.
				slotPanel.AddEventListener( "onmouseout",  () => OnSlotUnhover( capturedIndex ) );
				// Click → log "select" (extend to actually do something useful).
				slotPanel.AddEventListener( "onclick",     () => OnSlotClick( capturedIndex ) );
				// Right-click → remove the item from the slot.
				slotPanel.AddEventListener( "onrightclick", () => OnSlotRightClick( capturedIndex ) );
				// Double-click → log "use" (e.g. consume a potion).
				slotPanel.AddEventListener( "ondoubleclick", () => OnSlotDoubleClick( capturedIndex ) );

				// Paint the seeded item visuals at runtime. No PNGs needed —
				// we tint the slot background with the entry's Color hex and
				// stamp a single-character Glyph in the centre via a Label.
				// Re-running this is gated by _slotsWired, but ApplySlotVisual
				// is also idempotent (it removes any prior .slot-glyph child
				// before adding a new one) so manual re-wiring is safe.
				ApplySlotVisual( slotPanel, SafeGet( capturedIndex ) );
			}

			_slotsWired = true;
			Log.Info( $"[InventoryGridFull] Wired {children.Count} slot click routers." );
		}
	}

	// ── Slot routers ─────────────────────────────────────────────────────

	private void OnSlotHover( int slotIndex )
	{
		var item = SafeGet( slotIndex );
		if ( item == null )
		{
			// Empty slot → leave the tooltip hidden.
			Hud.TooltipVisible = false;
			return;
		}

		Hud.ItemTooltip   = $"{item.Name}  x{item.Count}";
		Hud.TooltipVisible = true;
	}

	private void OnSlotUnhover( int slotIndex )
	{
		Hud.TooltipVisible = false;
	}

	private void OnSlotClick( int slotIndex )
	{
		var item = SafeGet( slotIndex );
		if ( item == null )
		{
			Log.Info( $"[InventoryGridFull] Click on empty slot #{slotIndex}." );
			return;
		}
		Log.Info( $"[InventoryGridFull] Select slot #{slotIndex}: {item.Name} (x{item.Count})." );
	}

	private void OnSlotRightClick( int slotIndex )
	{
		var item = SafeGet( slotIndex );
		if ( item == null ) return;

		Log.Info( $"[InventoryGridFull] Drop slot #{slotIndex}: {item.Name} (x{item.Count})." );
		_items[slotIndex] = null;

		// Strip the slot's runtime visuals. We applied a BackgroundColor +
		// glyph Label in the wire-up loop instead of authoring a PNG icon
		// path; clearing the entry means clearing both. Style.BackgroundColor
		// is reset to null so the slot falls back to its authored .sui colour,
		// and any .slot-glyph child Label is removed via the helper below.
		var slotPanel = GetSlotPanel( slotIndex );
		if ( slotPanel != null )
		{
			ApplySlotVisual( slotPanel, null );
		}

		Hud.TooltipVisible = false;
		RefreshSubtitle();
	}

	private void OnSlotDoubleClick( int slotIndex )
	{
		var item = SafeGet( slotIndex );
		if ( item == null ) return;
		Log.Info( $"[InventoryGridFull] Use slot #{slotIndex}: {item.Name}." );
		// Hook gameplay here (drink potion, equip weapon, etc.).
	}

	// ── Grid-level fallback handlers (Code-mode events declared in .sui) ──
	// These fire when no child handles the click first. With per-slot
	// listeners wired in OnUpdate, this is rare — mostly clicks on grid
	// padding/gaps.

	private void OnGridClick()      { /* no-op — children handle their own clicks */ }
	private void OnGridRightClick() { /* no-op — see ISSUE-D018 (no button filter) */ }

	// ── Helpers ──────────────────────────────────────────────────────────

	private ItemEntry SafeGet( int slotIndex )
	{
		if ( _items == null ) return null;
		if ( slotIndex < 0 || slotIndex >= _items.Count ) return null;
		return _items[slotIndex];
	}

	/// <summary>Idempotent painter that syncs a slot Panel to an
	/// <see cref="ItemEntry"/> (or <c>null</c> for empty). Strips any
	/// previously-attached <c>.slot-glyph</c> Label child first, then —
	/// if <paramref name="item"/> is non-null — parses
	/// <see cref="ItemEntry.Color"/> via <c>Sandbox.Color.Parse</c> and
	/// applies it to <c>slotPanel.Style.BackgroundColor</c>, then adds
	/// a fresh <c>Sandbox.UI.Label</c> with the glyph centred via the
	/// <c>slot-glyph</c> class (styling lives in the generated .scss /
	/// user .scss; absent class still renders, just unstyled). Safe to
	/// call repeatedly — the strip-then-paint pattern keeps the slot
	/// in sync with the data without leaking ghost Labels.</summary>
	private void ApplySlotVisual( Panel slotPanel, ItemEntry item )
	{
		if ( slotPanel == null ) return;

		// 1) Strip any prior glyph Label so we don't stack them on re-paint.
		//    ToList() materialises so Delete() doesn't mutate during iter.
		var existingGlyphs = slotPanel.Children
			.Where( c => c != null && c.HasClass( "slot-glyph" ) )
			.ToList();
		foreach ( var g in existingGlyphs )
		{
			g.Delete();
		}

		// 2) Empty slot → clear inline background-color override so the slot
		//    falls back to its .sui-authored .inv-slot colour.
		if ( item == null )
		{
			slotPanel.Style.BackgroundColor = null;
			slotPanel.Style.Dirty();
			return;
		}

		// 3) Filled slot → tint background + stamp glyph Label.
		if ( !string.IsNullOrWhiteSpace( item.Color ) )
		{
			// Color.Parse returns Color? — null on a malformed hex string.
			// Both the null and the (extremely unlikely) throw paths are
			// swallowed so a bad seed entry can't blow up the wire-up loop.
			try
			{
				var parsed = Color.Parse( item.Color );
				if ( parsed.HasValue )
				{
					slotPanel.Style.BackgroundColor = parsed.Value;
					slotPanel.Style.Dirty();
				}
			}
			catch
			{
				// Bad hex → leave the authored background colour alone.
			}
		}

		if ( !string.IsNullOrEmpty( item.Glyph ) )
		{
			var label = slotPanel.AddChild<Label>();
			if ( label != null )
			{
				label.AddClass( "slot-glyph" );
				label.Text = item.Glyph;
			}
		}
	}

	private Panel GetSlotPanel( int slotIndex )
	{
		var grid = Hud.View?.BackpackGrid;
		if ( grid == null ) return null;
		var children = grid.Children.ToList();
		if ( slotIndex < 0 || slotIndex >= children.Count ) return null;
		return children[slotIndex];
	}

	private void RefreshSubtitle()
	{
		int filled = 0;
		if ( _items != null )
		{
			for ( int i = 0; i < _items.Count; i++ )
				if ( _items[i] != null ) filled++;
		}
		Hud.SlotCountText = $"{filled} / {SlotCapacity} slots";
	}
}
