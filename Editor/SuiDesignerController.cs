using System;
using System.Collections.Generic;
using System.Linq;
using SboxUiDesigner.EditorUi.Commands;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Single source of truth for an open .sui document inside the editor window.
/// Owns the document, the selection state, the dirty state, and the command
/// stack. Region widgets read state through the controller and mutate state
/// only via commands — direct widget-to-widget event wiring is avoided so undo,
/// dirty-tracking, and persistence stay coherent.
///
/// All edits go through <see cref="Execute(ISuiCommand)"/>. Cosmetic-only state
/// (selection) goes through <see cref="SetSelected"/> and does NOT enter the
/// command stack — selection itself isn't undoable, only document mutations are.
/// </summary>
public sealed class SuiDesignerController
{
	public SuiDocument Document { get; private set; }

	/// <summary>
	/// Multi-selection state. <see cref="Selected"/> is the "primary" — the most
	/// recently focused element used by the Details panel and chrome handles.
	/// <see cref="SelectedSet"/> is everyone in the current selection (always
	/// contains <see cref="Selected"/> when non-null).
	/// </summary>
	public SuiElement Selected { get; private set; }
	public IReadOnlyCollection<SuiElement> SelectedSet => _selectedSet;
	public int SelectedCount => _selectedSet.Count;

	private readonly HashSet<SuiElement> _selectedSet = new();

	public bool IsDirty { get; private set; }
	public SuiCommandStack Commands { get; }

	public event Action DocumentChanged;
	public event Action SelectionChanged;
	public event Action DirtyChanged;
	public event Action CommandsChanged;

	public SuiDesignerController()
	{
		Commands = new SuiCommandStack();
		Commands.Changed += () => CommandsChanged?.Invoke();
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Document lifecycle
	// ─────────────────────────────────────────────────────────────────────

	public void SetDocument( SuiDocument doc )
	{
		Document = doc;
		_selectedSet.Clear();
		Selected = doc?.GetRoot();
		if ( Selected != null ) _selectedSet.Add( Selected );
		IsDirty = false;
		Commands.Clear();
		DocumentChanged?.Invoke();
		SelectionChanged?.Invoke();
		DirtyChanged?.Invoke();
	}

	public void MarkSaved()
	{
		if ( !IsDirty ) return;
		IsDirty = false;
		DirtyChanged?.Invoke();
	}

	private void SetDirty()
	{
		if ( IsDirty ) return;
		IsDirty = true;
		DirtyChanged?.Invoke();
	}

	/// <summary>
	/// Mark the document dirty when a non-command mutation happens — e.g. user
	/// picks an output folder via Window's PromptOutputFolder. These mutations
	/// don't go through the command stack (they're settings, not undoable
	/// edits) but still need to trigger a Save prompt on close.
	/// </summary>
	public void MarkDirtyExternally()
	{
		SetDirty();
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Selection
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Replace the entire selection with this single element (or clear when null).
	/// </summary>
	public void SetSelected( SuiElement element )
	{
		var same = _selectedSet.Count == 1 && Selected == element;
		if ( same && _selectedSet.Contains( element ) ) return;

		_selectedSet.Clear();
		Selected = element;
		if ( element != null ) _selectedSet.Add( element );
		SelectionChanged?.Invoke();
	}

	public void SetSelectedById( string elementId )
	{
		if ( Document == null ) return;
		var el = Document.GetElement( elementId );
		SetSelected( el );
	}

	public void ClearSelection()
	{
		if ( _selectedSet.Count == 0 && Selected == null ) return;
		_selectedSet.Clear();
		Selected = null;
		SelectionChanged?.Invoke();
	}

	/// <summary>Replace the selection with the given set. Primary becomes the
	/// last element in the enumeration.</summary>
	public void SetSelection( IEnumerable<SuiElement> elements )
	{
		_selectedSet.Clear();
		Selected = null;
		if ( elements != null )
		{
			foreach ( var el in elements )
			{
				if ( el == null ) continue;
				_selectedSet.Add( el );
				Selected = el;
			}
		}
		SelectionChanged?.Invoke();
	}

	/// <summary>Add an element to the selection (Shift+click). Becomes primary.</summary>
	public void AddSelected( SuiElement element )
	{
		if ( element == null ) return;
		_selectedSet.Add( element );
		Selected = element;
		SelectionChanged?.Invoke();
	}

	/// <summary>Remove an element from the selection. Primary may shift.</summary>
	public void RemoveSelected( SuiElement element )
	{
		if ( element == null ) return;
		if ( !_selectedSet.Remove( element ) ) return;
		if ( Selected == element )
			Selected = _selectedSet.LastOrDefault();
		SelectionChanged?.Invoke();
	}

	/// <summary>Toggle an element in/out of the selection (Shift+click on element).</summary>
	public void ToggleSelected( SuiElement element )
	{
		if ( element == null ) return;
		if ( _selectedSet.Contains( element ) ) RemoveSelected( element );
		else AddSelected( element );
	}

	/// <summary>Is this element currently selected?</summary>
	public bool IsSelected( SuiElement element )
		=> element != null && _selectedSet.Contains( element );

	// ─────────────────────────────────────────────────────────────────────
	//  Commands
	// ─────────────────────────────────────────────────────────────────────

	public void Execute( ISuiCommand cmd )
	{
		if ( Document == null || cmd == null ) return;
		Commands.Push( cmd, Document );
		SetDirty();
		DocumentChanged?.Invoke();
	}

	public void Undo()
	{
		if ( Document == null || !Commands.CanUndo ) return;
		Commands.Undo( Document );
		SetDirty();
		// If the selected element was deleted by the command we undid, the
		// selection might now point at a stale instance. Validate and clear.
		ValidateSelection();
		DocumentChanged?.Invoke();
		SelectionChanged?.Invoke();
	}

	public void Redo()
	{
		if ( Document == null || !Commands.CanRedo ) return;
		Commands.Redo( Document );
		SetDirty();
		ValidateSelection();
		DocumentChanged?.Invoke();
		SelectionChanged?.Invoke();
	}

	private void ValidateSelection()
	{
		// Drop any selected elements that no longer exist in the document.
		// (Undo/redo can resurrect or remove elements, leaving stale references.)
		_selectedSet.RemoveWhere( el => Document?.GetElement( el.Id ) == null );
		if ( Selected != null && Document?.GetElement( Selected.Id ) == null )
			Selected = _selectedSet.LastOrDefault() ?? Document?.GetRoot();
		if ( Selected == null ) return;
		if ( Document.GetElement( Selected.Id ) == null )
		{
			Selected = Document.GetRoot();
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  High-level operations
	//  These wrap a command + an optional selection update so callers don't
	//  have to construct command objects manually.
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Add a new element of <paramref name="type"/> as a child of
	/// <paramref name="parent"/> (or the current selection's container, or root).
	/// Selects the new element automatically.
	/// </summary>
	public SuiElement AddElement( SuiElementType type, SuiElement parent = null )
	{
		if ( Document == null ) return null;

		parent ??= ResolveAddTarget();
		if ( parent == null ) return null;

		var element = new SuiElement
		{
			Id = SuiDocument.NewElementId(),
			Name = SuggestUniqueName( type ),
			Type = type,
			ParentId = parent.Id,
		};
		element.ApplyTypeDefaults();
		element.Style.ClassName = SuiDocumentValidator.SanitizeClassName( element.Name );

		Execute( new SuiAddElementCommand( element, parent.Id ) );
		SetSelected( element );
		return element;
	}

	/// <summary>
	/// Pick the parent for a click-to-add operation. M5 default is "always Root"
	/// — auto-nesting based on the current selection caused surprising behaviour
	/// (click Panel → click Image → Image becomes child of Panel even though
	/// the user clicked the palette without dragging). Drag-and-drop in M6 will
	/// reintroduce nesting via explicit drop targets.
	///
	/// Callers that already know the parent (e.g. a future drag handler) should
	/// pass the parent explicitly to <see cref="AddElement(SuiElementType, SuiElement)"/>
	/// rather than rely on this resolver.
	/// </summary>
	private SuiElement ResolveAddTarget()
	{
		return Document.GetRoot();
	}

	private static bool IsContainer( SuiElementType type ) => type switch
	{
		SuiElementType.Canvas
			or SuiElementType.Panel
			or SuiElementType.Overlay
			or SuiElementType.HorizontalBox
			or SuiElementType.VerticalBox
			or SuiElementType.Grid
			or SuiElementType.ScrollPanel
			or SuiElementType.InventoryGrid
			or SuiElementType.Hotbar => true,
		_ => false,
	};

	private string SuggestUniqueName( SuiElementType type )
	{
		var baseName = type.ToString();
		if ( Document == null ) return baseName;

		// Try the bare type name first; otherwise append _2, _3, ...
		if ( !NameExists( baseName ) ) return baseName;
		for ( int i = 2; i < 1000; i++ )
		{
			var candidate = $"{baseName}_{i}";
			if ( !NameExists( candidate ) ) return candidate;
		}
		return $"{baseName}_{System.Guid.NewGuid().ToString( "N" ).Substring( 0, 4 )}";
	}

	private bool NameExists( string name )
	{
		foreach ( var el in Document.Elements )
			if ( string.Equals( el.Name, name, StringComparison.OrdinalIgnoreCase ) ) return true;
		return false;
	}

	/// <summary>
	/// Delete the selected element (or the explicitly given one). Root cannot be deleted.
	/// </summary>
	public void DeleteElement( SuiElement element = null )
	{
		element ??= Selected;
		if ( element == null || string.IsNullOrEmpty( element.ParentId ) ) return; // root or unset

		var newSelection = Document.GetElement( element.ParentId ) ?? Document.GetRoot();
		Execute( new SuiDeleteElementCommand( element.Id ) );
		SetSelected( newSelection );
	}

	public void RenameElement( SuiElement element, string newName )
	{
		if ( element == null || string.IsNullOrEmpty( newName ) ) return;
		if ( element.Name == newName ) return; // no-op
		Execute( new SuiRenameElementCommand( element.Id, newName ) );
	}

	/// <summary>
	/// Duplicate an element together with its descendants. New ids throughout
	/// the cloned subtree, inserted as a sibling immediately after the source.
	/// Selects the duplicate after the operation.
	/// </summary>
	public SuiElement DuplicateElement( SuiElement element = null )
	{
		element ??= Selected;
		if ( element == null || string.IsNullOrEmpty( element.ParentId ) ) return null; // refuse root

		var cmd = new SuiDuplicateElementCommand( element.Id );
		Execute( cmd );

		// Select the new clone if Apply succeeded.
		if ( !string.IsNullOrEmpty( cmd.ResultingElementId ) && Document != null )
		{
			var newElement = Document.GetElement( cmd.ResultingElementId );
			if ( newElement != null ) SetSelected( newElement );
		}
		return Selected;
	}

	/// <summary>Move the element up among its siblings (or selection if null).</summary>
	public void MoveElementUp( SuiElement element = null )
	{
		element ??= Selected;
		if ( element == null || string.IsNullOrEmpty( element.ParentId ) ) return;
		Execute( new SuiReorderElementCommand( element.Id, -1 ) );
	}

	/// <summary>Move the element down among its siblings (or selection if null).</summary>
	public void MoveElementDown( SuiElement element = null )
	{
		element ??= Selected;
		if ( element == null || string.IsNullOrEmpty( element.ParentId ) ) return;
		Execute( new SuiReorderElementCommand( element.Id, +1 ) );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Clipboard — Cut / Copy / Paste
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Copy the element (or current selection) and its descendants to the
	/// process-local clipboard. Doesn't mutate the document — clipboard holds
	/// deep clones, not references.
	/// </summary>
	public void CopyElement( SuiElement element = null )
	{
		element ??= Selected;
		if ( element == null || Document == null ) return;
		if ( string.IsNullOrEmpty( element.ParentId ) ) return; // refuse copying root

		var byId = new Dictionary<string, SuiElement>();
		foreach ( var el in Document.Elements )
			if ( !string.IsNullOrEmpty( el.Id ) ) byId[el.Id] = el;

		var payload = new SuiClipboardPayload();
		payload.Root = CloneSubtreeForClipboard( element, byId, payload.All );
		SuiClipboard.Set( payload );
	}

	/// <summary>Copy + delete (Cut). Refuses to cut root.</summary>
	public void CutElement( SuiElement element = null )
	{
		element ??= Selected;
		if ( element == null || Document == null ) return;
		if ( string.IsNullOrEmpty( element.ParentId ) ) return;

		CopyElement( element );
		DeleteElement( element );
	}

	/// <summary>
	/// Paste the clipboard subtree under <paramref name="parent"/> (defaults to
	/// the current selection if it's a container, otherwise the selection's parent,
	/// otherwise root). Selects the pasted root after the operation.
	/// </summary>
	public SuiElement PasteElement( SuiElement parent = null )
	{
		if ( Document == null || !SuiClipboard.HasContent ) return null;

		parent ??= ResolvePasteParent();
		if ( parent == null ) return null;

		var cmd = new SuiPasteElementCommand( SuiClipboard.Get(), parent.Id );
		Execute( cmd );

		if ( !string.IsNullOrEmpty( cmd.ResultingElementId ) )
		{
			var pasted = Document.GetElement( cmd.ResultingElementId );
			if ( pasted != null ) SetSelected( pasted );
			return pasted;
		}
		return null;
	}

	/// <summary>True if there's anything to paste — used by menus to enable/disable.</summary>
	public bool CanPaste => SuiClipboard.HasContent && Document != null;

	private SuiElement ResolvePasteParent()
	{
		if ( Selected == null ) return Document.GetRoot();
		// If selected is a container, paste INTO it.
		if ( IsContainer( Selected.Type ) ) return Selected;
		// Else paste as sibling (= same parent).
		var parent = Document.GetElement( Selected.ParentId );
		return parent ?? Document.GetRoot();
	}

	private static SuiElement CloneSubtreeForClipboard(
		SuiElement source,
		Dictionary<string, SuiElement> byId,
		List<SuiElement> output )
	{
		var clone = source.Clone();
		clone.Children = new List<string>(); // will be repopulated below with cloned ids
		output.Add( clone );
		foreach ( var childId in source.Children ?? new List<string>() )
		{
			if ( !byId.TryGetValue( childId, out var child ) ) continue;
			var clonedChild = CloneSubtreeForClipboard( child, byId, output );
			clonedChild.ParentId = clone.Id; // preserves intra-subtree linkage
			clone.Children.Add( clonedChild.Id );
		}
		return clone;
	}

	/// <summary>
	/// Reparent an element to a new container at a specific child index.
	/// Refuses to move root and refuses to create cycles.
	/// </summary>
	public void ReparentElement( SuiElement element, SuiElement newParent, int insertIndex )
	{
		if ( element == null || newParent == null ) return;
		if ( string.IsNullOrEmpty( element.ParentId ) ) return; // refuse root
		Execute( new SuiReparentElementCommand( element.Id, newParent.Id, insertIndex ) );
	}
	public void MoveElement( SuiElement element, float newX, float newY )
	{
		if ( element == null ) return;
		Execute( new SuiMoveElementCommand( element.Id, newX, newY ) );
		NotifySelectionDataMaybeChanged( element );
	}

	/// <summary>
	/// Change anchor while preserving the element's visual position. UMG / UI
	/// Builder convention: switching anchor only changes the reference point;
	/// the element should NOT jump on screen. The command captures the current
	/// rect under the old anchor and re-derives X/Y/W/H under the new one.
	/// </summary>
	public void SetAnchor( SuiElement element, SuiAnchor newAnchor )
	{
		if ( element?.Layout == null ) return;
		if ( element.Layout.Anchor == newAnchor ) return;
		Execute( new SuiSetAnchorCommand( element.Id, newAnchor ) );
		NotifySelectionDataMaybeChanged( element );
	}

	/// <summary>
	/// Re-fire SelectionChanged when an external mutation (canvas drag, anchor
	/// swap) changed properties of the currently-selected element. The Details
	/// panel uses SelectionChanged to rebuild itself, so this keeps its row
	/// values in sync without doing a full Refresh on every property edit
	/// (which would yank the scroll position).
	/// </summary>
	private void NotifySelectionDataMaybeChanged( SuiElement element )
	{
		if ( element == null ) return;
		if ( Selected == element || _selectedSet.Contains( element ) )
			SelectionChanged?.Invoke();
	}

	public void ResizeElement( SuiElement element, float newWidth, float newHeight )
	{
		if ( element == null ) return;
		Execute( new SuiResizeElementCommand( element.Id, newWidth, newHeight ) );
		NotifySelectionDataMaybeChanged( element );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Multi-element alignment (V1.0)
	//
	//  Filters: only Absolute-mode elements that share a single parent.
	//  Mixed parents / mixed anchors are out of scope — the bounding-box
	//  math assumes a common coord space. Locked elements are skipped.
	// ─────────────────────────────────────────────────────────────────────

	public void AlignSelection( SuiAlignElementsCommand.Mode mode )
	{
		var ids = CollectAlignableSelection();
		if ( ids == null || ids.Count < 2 )
		{
			Log.Info( "[Sui] Align needs ≥2 absolute-mode elements with the same parent." );
			return;
		}
		Execute( new SuiAlignElementsCommand( ids, mode ) );
		SelectionChanged?.Invoke();
	}

	public void DistributeSelection( SuiDistributeElementsCommand.Axis axis )
	{
		var ids = CollectAlignableSelection();
		if ( ids == null || ids.Count < 3 )
		{
			Log.Info( "[Sui] Distribute needs ≥3 absolute-mode elements with the same parent." );
			return;
		}
		Execute( new SuiDistributeElementsCommand( ids, axis ) );
		SelectionChanged?.Invoke();
	}

	/// <summary>
	/// Filters the current selection down to absolute-mode elements that all
	/// share the same parent and aren't locked. Returns null if no usable set.
	/// </summary>
	private System.Collections.Generic.List<string> CollectAlignableSelection()
	{
		if ( _selectedSet == null || _selectedSet.Count < 2 ) return null;
		string commonParent = null;
		var ids = new System.Collections.Generic.List<string>();
		foreach ( var el in _selectedSet )
		{
			if ( el?.Layout == null ) continue;
			if ( el.Layout.Mode != SuiLayoutMode.Absolute ) continue;
			if ( el.Flags?.Locked == true ) continue;
			if ( string.IsNullOrEmpty( el.ParentId ) ) continue; // skip root
			if ( commonParent == null ) commonParent = el.ParentId;
			else if ( commonParent != el.ParentId ) return null; // mixed parents
			ids.Add( el.Id );
		}
		return ids;
	}

	/// <summary>
	/// Generic property setter — see <see cref="SuiSetPropertyCommand{T}"/> for usage.
	/// </summary>
	public void SetProperty<T>(
		SuiElement element,
		Func<SuiElement, T> getter,
		Action<SuiElement, T> setter,
		T newValue,
		string description )
	{
		if ( element == null ) return;
		Execute( new SuiSetPropertyCommand<T>( element.Id, getter, setter, newValue, description ) );
	}
}
