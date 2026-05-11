using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Rename an element. Changes <see cref="SuiElement.Name"/> and — when the
/// current ClassName is still the auto-generated form (matches
/// <c>SanitizeClassName(oldName)</c>) — also re-syncs ClassName to the new
/// name. If the user customised ClassName manually, we leave it alone.
///
/// The stable <see cref="SuiElement.Id"/> never changes on rename.
/// </summary>
public sealed class SuiRenameElementCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly string _newName;
	private string _oldName;
	private string _oldClassName;
	private bool _classNameWasSynced;

	public string Description => $"Rename to '{_newName}'";

	public SuiRenameElementCommand( string elementId, string newName )
	{
		_elementId = elementId;
		_newName = newName;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;
		_oldName = el.Name;
		_oldClassName = el.Style?.ClassName;

		el.Name = _newName;

		// Auto-sync ClassName only when it currently matches what we'd generate
		// from the OLD name — i.e. the user hasn't manually customised it.
		if ( el.Style != null )
		{
			var autoFromOld = SuiDocumentValidator.SanitizeClassName( _oldName ?? "" );
			if ( string.Equals( _oldClassName ?? "", autoFromOld, System.StringComparison.Ordinal ) )
			{
				el.Style.ClassName = SuiDocumentValidator.SanitizeClassName( _newName ?? "" );
				_classNameWasSynced = true;
			}
		}
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;
		el.Name = _oldName;
		if ( _classNameWasSynced && el.Style != null )
			el.Style.ClassName = _oldClassName;
	}
}
