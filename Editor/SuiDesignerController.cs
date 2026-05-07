using System;
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
	public SuiElement Selected { get; private set; }
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
		Selected = doc?.GetRoot();
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

	// ─────────────────────────────────────────────────────────────────────
	//  Selection
	// ─────────────────────────────────────────────────────────────────────

	public void SetSelected( SuiElement element )
	{
		if ( Selected == element ) return;
		Selected = element;
		SelectionChanged?.Invoke();
	}

	public void SetSelectedById( string elementId )
	{
		if ( Document == null ) return;
		var el = Document.GetElement( elementId );
		SetSelected( el );
	}

	public void ClearSelection() => SetSelected( null );

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
		Execute( new SuiRenameElementCommand( element.Id, newName ) );
	}

	public void MoveElement( SuiElement element, float newX, float newY )
	{
		if ( element == null ) return;
		Execute( new SuiMoveElementCommand( element.Id, newX, newY ) );
	}

	public void ResizeElement( SuiElement element, float newWidth, float newHeight )
	{
		if ( element == null ) return;
		Execute( new SuiResizeElementCommand( element.Id, newWidth, newHeight ) );
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
