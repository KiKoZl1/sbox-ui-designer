using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// V1.5 M3 — toggle <see cref="SuiElementFlags.ExposeAsVariable"/> on a
/// single element (PRD 20 § 5.2). When true, the generator emits
/// <c>@ref="&lt;FieldName&gt;"</c> on the markup tag and declares a typed field
/// on the renderer Panel class so the user's <c>&lt;Name&gt;.partial.cs</c>
/// can poke the live element imperatively.
/// </summary>
public sealed class SuiSetExposeCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly bool _newValue;
	private bool _previousValue;

	public string Description => _newValue ? "Expose as variable" : "Un-expose";

	public SuiSetExposeCommand( string elementId, bool newValue )
	{
		_elementId = elementId;
		_newValue = newValue;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;
		el.Flags ??= new SuiElementFlags();
		_previousValue = el.Flags.ExposeAsVariable;
		el.Flags.ExposeAsVariable = _newValue;
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el?.Flags == null ) return;
		el.Flags.ExposeAsVariable = _previousValue;
	}
}
