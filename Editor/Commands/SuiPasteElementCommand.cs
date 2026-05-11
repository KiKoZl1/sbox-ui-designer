using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Paste a <see cref="SuiClipboardPayload"/> into the document under
/// <c>parentId</c>. The clipboard subtree gets fresh IDs throughout, name
/// uniquified to avoid collisions, and is appended at the end of the parent's
/// children. The id of the pasted root is exposed via
/// <see cref="ResultingElementId"/> so the controller can select it.
/// </summary>
public sealed class SuiPasteElementCommand : ISuiCommand
{
	private readonly SuiClipboardPayload _payload;
	private readonly string _parentId;

	private List<SuiElement> _addedElements;
	private string _newRootId;
	private int _insertIndex = -1;

	public string Description => "Paste";
	public string ResultingElementId => _newRootId;

	public SuiPasteElementCommand( SuiClipboardPayload payload, string parentId )
	{
		_payload = payload;
		_parentId = parentId;
	}

	public void Apply( SuiDocument doc )
	{
		if ( doc == null || _payload?.Root == null ) return;

		var parent = doc.GetElement( _parentId ) ?? doc.GetRoot();
		if ( parent == null ) return;

		// Remap old subtree ids → new ids so the paste can be re-applied
		// after an undo without colliding.
		var idMap = new Dictionary<string, string>();
		foreach ( var el in _payload.All )
			idMap[el.Id] = SuiDocument.NewElementId();

		_addedElements = new List<SuiElement>();
		foreach ( var el in _payload.All )
		{
			var clone = el.Clone();
			clone.Id = idMap[el.Id];
			clone.ParentId = idMap.TryGetValue( el.ParentId ?? "", out var newParent ) ? newParent : null;
			clone.Children = new List<string>();
			foreach ( var oldChild in el.Children ?? new List<string>() )
				if ( idMap.TryGetValue( oldChild, out var newChild ) )
					clone.Children.Add( newChild );
			_addedElements.Add( clone );
		}

		// Pasted root: pin to the chosen parent + uniquify name.
		var pastedRoot = FindById( _addedElements, idMap[_payload.Root.Id] );
		if ( pastedRoot == null ) return;

		pastedRoot.ParentId = parent.Id;
		pastedRoot.Name = SuggestUniqueName( doc, pastedRoot.Name );
		if ( pastedRoot.Style != null )
			pastedRoot.Style.ClassName = SuiDocumentValidator.SanitizeClassName( pastedRoot.Name );

		_insertIndex = parent.Children.Count;
		parent.Children.Add( pastedRoot.Id );
		_newRootId = pastedRoot.Id;

		foreach ( var el in _addedElements ) doc.Elements.Add( el );
	}

	public void Undo( SuiDocument doc )
	{
		if ( doc == null || _addedElements == null ) return;

		var parent = doc.GetElement( _parentId ) ?? doc.GetRoot();
		if ( parent != null && _insertIndex >= 0 && _insertIndex < parent.Children.Count
			&& parent.Children[_insertIndex] == _newRootId )
		{
			parent.Children.RemoveAt( _insertIndex );
		}
		else if ( parent != null )
		{
			// Defensive: if the index drifted (shouldn't, but handle).
			parent.Children.Remove( _newRootId );
		}

		foreach ( var el in _addedElements )
			doc.Elements.Remove( el );
		_addedElements = null;
	}

	private static SuiElement FindById( List<SuiElement> list, string id )
	{
		foreach ( var el in list ) if ( el.Id == id ) return el;
		return null;
	}

	private static string SuggestUniqueName( SuiDocument doc, string baseName )
	{
		if ( string.IsNullOrEmpty( baseName ) ) baseName = "Element";

		// Strip an existing "_<int>" so paste of "Panel_2" picks "Panel_3" not "Panel_2_2".
		var bare = baseName;
		var underscore = baseName.LastIndexOf( '_' );
		if ( underscore > 0 && underscore < baseName.Length - 1
			&& int.TryParse( baseName.Substring( underscore + 1 ), out _ ) )
		{
			bare = baseName.Substring( 0, underscore );
		}

		if ( !NameExists( doc, baseName ) ) return baseName;
		for ( int i = 2; i < 1000; i++ )
		{
			var candidate = $"{bare}_{i}";
			if ( !NameExists( doc, candidate ) ) return candidate;
		}
		return $"{bare}_{System.Guid.NewGuid().ToString( "N" ).Substring( 0, 4 )}";
	}

	private static bool NameExists( SuiDocument doc, string name )
	{
		foreach ( var el in doc.Elements )
			if ( string.Equals( el.Name, name, System.StringComparison.OrdinalIgnoreCase ) ) return true;
		return false;
	}
}
