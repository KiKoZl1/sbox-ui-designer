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
		[Property] public string IconPath { get; set; } = "";
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
		new ItemEntry { Name = "Iron Sword",    IconPath = "ui/icons/sword.png",         Count = 1  },
		new ItemEntry { Name = "Health Potion", IconPath = "ui/icons/health_potion.png", Count = 5  },
		new ItemEntry { Name = "Bread",         IconPath = "ui/icons/bread.png",         Count = 12 },
		new ItemEntry { Name = "Gold Coin",     IconPath = "ui/icons/gold_coin.png",     Count = 99 },
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

		// Strip the slot's icon at runtime. The .sui authored a
		// background-image via PreviewIconPath which the generator baked
		// into SCSS; we override it inline so the empty slot looks empty
		// without needing a class-swap. (Re-seeding would be the inverse:
		// also set background-image to the new icon's URL.)
		var slotPanel = GetSlotPanel( slotIndex );
		if ( slotPanel != null )
		{
			slotPanel.Style.BackgroundImage = null;
			slotPanel.Style.Dirty();
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
