using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>drag_drop_inventory</c> showcase. Two 4×4
/// grids (Backpack + Stash) are populated at runtime with 16 slot Panels
/// each. Items are stored as an <see cref="ItemKind"/> enum; the controller
/// renders them as coloured panels with a single-letter glyph. Mouse-down
/// on an occupied slot starts a drag, a floating ghost follows the cursor,
/// and mouse-up on any slot (in either grid) swaps the source and target
/// items.
///
/// <para>Stress-tests four corners of the Designer + engine pipeline at once:</para>
/// <list type="number">
/// <item>Multiple <c>ExposeAsVariable</c> Panels (BackpackGrid, StashGrid,
/// DragGhost) all need their <c>@ref</c> captured before any runtime
/// mutation works.</item>
/// <item>Runtime-added child Panels inside a Flex-Row-Wrap parent —
/// exercises the codegen flex-direction emission fix (parent is
/// Mode=Absolute but children still flow as a wrapping grid).</item>
/// <item>Mouse position math during a drag — <c>Panel.MousePosition</c>
/// freezes once the engine claims drag, so the ghost reads
/// <c>Sandbox.Mouse.Position</c> directly and multiplies by
/// <c>ScaleFromScreen</c> to convert pixels into panel space.</item>
/// <item>Per-element <c>AddEventListener</c> on dynamically-created Panels
/// — every slot wires <c>onmousedown</c> / <c>onmouseup</c> against a
/// captured index.</item>
/// </list>
/// </summary>
public sealed class DragDropInventoryController : Component
{
	/// <summary>
	/// Generated wrapper instance. Constructed inline so the Inspector
	/// shows the (empty) Variables foldout under the Hud Property.
	/// </summary>
	[Property] public DragDropInventory Hud { get; set; } = new();

	private const int SlotCount = 16;
	private const int SlotPx = 56;
	private const int HalfSlot = SlotPx / 2;

	// ────────────────────────────────────────────────────────────────────
	// Item model
	// ────────────────────────────────────────────────────────────────────

	/// <summary>Item taxonomy. <c>None</c> means an empty slot.</summary>
	private enum ItemKind { None, Sword, Potion, Gold, Helmet, Shield, Bow, Gem, Scroll }

	private readonly ItemKind[] _backpack = new ItemKind[SlotCount];
	private readonly ItemKind[] _stash = new ItemKind[SlotCount];

	// ────────────────────────────────────────────────────────────────────
	// Drag state — one in flight at a time.
	// ────────────────────────────────────────────────────────────────────

	private bool _dragging;
	private bool _dragFromBackpack;
	private int _dragSourceIndex;
	private ItemKind _dragItem;

	// Per-grid slot panel references. Populated by RenderGrid in the order
	// the slot indices appear. We hit-test these against Sandbox.Mouse.Position
	// in OnRootMouseUp to find the drop target — Sandbox.UI silences
	// onmouseover on other panels during the drag, so we can't track hover
	// via events. Manual IsInside hit-test is the reliable path.
	private readonly List<Panel> _backpackSlotPanels = new();
	private readonly List<Panel> _stashSlotPanels = new();

	// One-shot bootstrap flags. The engine populates @ref fields AFTER the
	// first paint, so OnStart sees Hud.View.BackpackGrid as null. Each flag
	// flips the frame its dependency becomes available.
	private bool _initialRendered;
	private bool _rootMouseupWired;


	protected override void OnStart()
	{
		// SuiInputMode.All grabs cursor input so mousedown / mouseup
		// events fire on the runtime slot Panels.
		Hud.Show( GameObject, SuiInputMode.All );

		// Seed: 4 items per grid, spread out so there's space to drag into.
		_backpack[0] = ItemKind.Sword;
		_backpack[2] = ItemKind.Potion;
		_backpack[5] = ItemKind.Gold;
		_backpack[10] = ItemKind.Helmet;

		_stash[1] = ItemKind.Shield;
		_stash[4] = ItemKind.Bow;
		_stash[9] = ItemKind.Gem;
		_stash[14] = ItemKind.Scroll;
	}

	protected override void OnUpdate()
	{
		// Bootstrap the rendered grids once both @refs are live.
		if ( !_initialRendered )
		{
			if ( Hud.View?.BackpackGrid != null && Hud.View?.StashGrid != null )
			{
				RenderGrid( Hud.View.BackpackGrid, _backpack, isBackpack: true );
				RenderGrid( Hud.View.StashGrid, _stash, isBackpack: false );
				_initialRendered = true;
			}
		}

		// Wire the root-level mouseup that cancels drags ending outside any
		// slot. Children's mouseup bubbles up first, so by the time root
		// sees the event the slot may have already EndDrag'd — the _dragging
		// guard inside OnRootMouseUp handles that.
		if ( !_rootMouseupWired )
		{
			if ( Hud.View != null )
			{
				Hud.View.AddEventListener( "onmouseup", OnRootMouseUp );
				_rootMouseupWired = true;
			}
		}

		// Ghost follows the cursor every frame while a drag is in flight.
		// Panel.MousePosition is frozen during native drag — read
		// Sandbox.Mouse.Position raw and apply ScaleFromScreen to convert
		// screen pixels into the ghost's local panel coords. Subtract
		// HalfSlot so the cursor sits in the centre of the ghost.
		if ( _dragging && Hud.View?.DragGhost != null )
		{
			var ghost = Hud.View.DragGhost;
			var local = Sandbox.Mouse.Position * ghost.ScaleFromScreen;
			ghost.Style.Left = Length.Pixels( local.x - HalfSlot );
			ghost.Style.Top = Length.Pixels( local.y - HalfSlot );
			ghost.Style.Dirty();
		}

		// (Source-slot clear during drag was tried via OnUpdate deferral and
		// also blocked the engine's mouseup routing. ANY mutation to the
		// source slot mid-drag — inside or outside the event handler —
		// breaks the gesture. Source stays visually populated during drag;
		// the ghost provides the "I'm carrying this" feedback instead.)
	}

	protected override void OnDestroy()
	{
		Hud?.Remove();
	}

	// ────────────────────────────────────────────────────────────────────
	// Grid rendering
	// ────────────────────────────────────────────────────────────────────

	private void RenderGrid( Panel grid, ItemKind[] slots, bool isBackpack )
	{
		// Full rebuild — only called during the initial bootstrap when the
		// grid Panel has just become available. Subsequent state changes
		// during a drag use UpdateSlotVisual to mutate individual slots
		// without destroying their Panel instances (the engine routes the
		// pending mouseup to whichever Panel received the mousedown, so
		// destroying mid-drag breaks the gesture).
		grid.DeleteChildren( true );
		var refList = isBackpack ? _backpackSlotPanels : _stashSlotPanels;
		refList.Clear();

		for ( int i = 0; i < slots.Length; i++ )
		{
			var slotPanel = grid.AddChild<Panel>();
			slotPanel.AddClass( "drag-slot" );
			refList.Add( slotPanel );

			// Explicit size on the runtime Panel — without Width/Height the
			// slot collapses to its content (or zero) inside a flex row.
			// flex-grow/flex-shrink live in User.scss because the runtime
			// Style properties for them aren't reliably exposed in every
			// Sandbox.UI build.
			slotPanel.Style.Width = Length.Pixels( SlotPx );
			slotPanel.Style.Height = Length.Pixels( SlotPx );

			UpdateSlotVisual( slotPanel, slots[i] );

			int capturedIndex = i;
			bool capturedSide = isBackpack;
			slotPanel.AddEventListener( "onmousedown", () => OnSlotMouseDown( capturedSide, capturedIndex ) );
		}
	}

	/// <summary>
	/// Mutate a single slot's visual state without recreating the Panel.
	/// Critical during a drag because destroying the Panel that received
	/// mousedown loses the engine's pending mouseup dispatch.
	/// </summary>
	private void UpdateSlotVisual( Panel slot, ItemKind item )
	{
		slot.DeleteChildren( true );
		slot.SetClass( "occupied", item != ItemKind.None );

		if ( item != ItemKind.None )
		{
			var (color, letter) = GetItemVisual( item );
			slot.Style.BackgroundColor = Color.Parse( color ).Value;

			var label = slot.AddChild<Label>();
			label.AddClass( "drag-item-letter" );
			label.Text = letter;
		}
		else
		{
			// Empty — reset to the User.scss empty colour. Style.Background
			// is an inline override, so it stays sticky once set; we have
			// to write the empty value explicitly to "undo" a previous item.
			slot.Style.BackgroundColor = Color.Parse( "#1f2230" ).Value;
		}

		slot.Style.Dirty();
	}

	// ────────────────────────────────────────────────────────────────────
	// Drag state machine
	// ────────────────────────────────────────────────────────────────────

	private void OnSlotMouseDown( bool isBackpack, int index )
	{
		if ( _dragging ) return;

		var slots = isBackpack ? _backpack : _stash;
		var item = slots[index];
		if ( item == ItemKind.None ) return;

		_dragging = true;
		_dragFromBackpack = isBackpack;
		_dragSourceIndex = index;
		_dragItem = item;

		// Source slot intentionally NOT mutated. Confirmed via testing in
		// 2026-06-05: any visual change to the source slot (DeleteChildren,
		// Style.BackgroundColor, AddClass, etc.) — whether inside this
		// handler or deferred to OnUpdate — eats the eventual mouseup
		// event. Keeping the source untouched preserves routing; the
		// ghost at the cursor supplies the "I'm holding this item" feedback.
		ShowGhost( item );
	}

	private (bool isBackpack, int index)? HitTestSlots( Vector2 mousePos )
	{
		// Iterate live slot Panels and ask each whether the cursor is
		// inside its rendered rect. Backpack first, then Stash — either
		// grid can be the target. Returns null if cursor is over no slot
		// (drop outside both grids).
		for ( int i = 0; i < _backpackSlotPanels.Count; i++ )
		{
			if ( _backpackSlotPanels[i].IsInside( mousePos ) )
				return (true, i);
		}
		for ( int i = 0; i < _stashSlotPanels.Count; i++ )
		{
			if ( _stashSlotPanels[i].IsInside( mousePos ) )
				return (false, i);
		}
		return null;
	}

	private void OnRootMouseUp()
	{
		if ( !_dragging ) return;

		// Sandbox.UI silences onmouseover on other slots during the drag,
		// so we don't know the target from event tracking — hit-test the
		// live panels against the current Mouse.Position instead.
		var hit = HitTestSlots( Sandbox.Mouse.Position );

		var sourceSlotsArr = _dragFromBackpack ? _backpack : _stash;
		var sourcePanelsList = _dragFromBackpack ? _backpackSlotPanels : _stashSlotPanels;

		if ( hit.HasValue )
		{
			var targetSlotsArr = hit.Value.isBackpack ? _backpack : _stash;
			var targetPanelsList = hit.Value.isBackpack ? _backpackSlotPanels : _stashSlotPanels;

			// Same-slot drop guard. When source and target reference the
			// SAME backing array slot, the swap becomes two writes to the
			// same cell — second write wins. Without this guard, dropping
			// an item back onto its own slot vanishes it (write _dragItem,
			// then overwrite with targetItem which is _dragItem itself —
			// but since the source array also points there, the slot reads
			// as None after the first write and the second write commits
			// None permanently).
			bool sameSlot = sourceSlotsArr == targetSlotsArr && _dragSourceIndex == hit.Value.index;

			if ( sameSlot )
			{
				// No-op move — item returns to where it came from.
				sourceSlotsArr[_dragSourceIndex] = _dragItem;
				UpdateSlotVisual( sourcePanelsList[_dragSourceIndex], _dragItem );
			}
			else
			{
				// Real swap — target takes _dragItem, source takes
				// whatever target had (may be ItemKind.None).
				var targetItem = targetSlotsArr[hit.Value.index];
				targetSlotsArr[hit.Value.index] = _dragItem;
				sourceSlotsArr[_dragSourceIndex] = targetItem;

				UpdateSlotVisual( targetPanelsList[hit.Value.index], _dragItem );
				UpdateSlotVisual( sourcePanelsList[_dragSourceIndex], targetItem );
			}
		}
		else
		{
			// Drop outside any slot — no-op since source was never cleared
			// during the drag. Item remains where it started.
		}

		EndDrag();
	}

	private void ShowGhost( ItemKind item )
	{
		var ghost = Hud.View?.DragGhost;
		if ( ghost == null ) return;

		var (color, letter) = GetItemVisual( item );
		ghost.Style.BackgroundColor = Color.Parse( color ).Value;

		// Visibility is gated by Opacity alone (the .sui starts the ghost at
		// opacity 0). PointerEvents:None in the .sui keeps the ghost out of
		// hit-testing whether visible or not, so we don't need a separate
		// display/visibility toggle.
		ghost.Style.Opacity = 0.85f;

		ghost.DeleteChildren( true );
		var label = ghost.AddChild<Label>();
		label.AddClass( "drag-item-letter" );
		label.Text = letter;

		ghost.Style.Dirty();
	}

	private void EndDrag()
	{
		_dragging = false;

		var ghost = Hud.View?.DragGhost;
		if ( ghost != null )
		{
			ghost.Style.Opacity = 0;
			ghost.DeleteChildren( true );
			ghost.Style.Dirty();
		}
	}

	// ────────────────────────────────────────────────────────────────────
	// Item visual lookup — colour + letter glyph per kind.
	// ────────────────────────────────────────────────────────────────────

	private static (string color, string letter) GetItemVisual( ItemKind kind ) => kind switch
	{
		ItemKind.Sword  => ( "#ef4444", "S"  ),
		ItemKind.Potion => ( "#10b981", "P"  ),
		ItemKind.Gold   => ( "#fbbf24", "G"  ),
		ItemKind.Helmet => ( "#3b82f6", "H"  ),
		ItemKind.Shield => ( "#a78bfa", "Sh" ),
		ItemKind.Bow    => ( "#84cc16", "B"  ),
		ItemKind.Gem    => ( "#ec4899", "Gm" ),
		ItemKind.Scroll => ( "#e5e7eb", "Sc" ),
		_               => ( "#000000", ""   ),
	};
}
