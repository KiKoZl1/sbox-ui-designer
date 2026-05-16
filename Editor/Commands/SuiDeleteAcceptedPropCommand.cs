using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Remove a <see cref="SuiAcceptedProp"/> from the document, remembering its
/// list index so undo re-inserts it in place. The matching
/// <c>FromAcceptedProp</c> Variable (if any) is NOT auto-deleted — that's a
/// separate decision the user makes after confirming nothing inside the doc
/// still references it.
/// </summary>
public sealed class SuiDeleteAcceptedPropCommand : ISuiCommand
{
	private readonly SuiAcceptedProp _prop;
	private int _index = -1;

	public string Description => $"Delete Accepted Prop '{_prop?.Name}'";

	public SuiDeleteAcceptedPropCommand( SuiAcceptedProp prop ) => _prop = prop;

	public void Apply( SuiDocument doc )
	{
		if ( doc?.AcceptedProps == null || _prop == null ) return;
		_index = doc.AcceptedProps.IndexOf( _prop );
		if ( _index >= 0 ) doc.AcceptedProps.RemoveAt( _index );
	}

	public void Undo( SuiDocument doc )
	{
		if ( doc?.AcceptedProps == null || _prop == null || _index < 0 ) return;
		var i = _index > doc.AcceptedProps.Count ? doc.AcceptedProps.Count : _index;
		doc.AcceptedProps.Insert( i, _prop );
	}
}
