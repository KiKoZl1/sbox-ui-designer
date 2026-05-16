using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Set (or clear) the <see cref="SuiElement.SuiReference"/> data block on an
/// element. Captures a deep-cloned before/after so undo restores cleanly.
/// Used by the SuiReference picker after the user picks a source .sui, by the
/// Details panel when editing Props / ForEach, and by the "swap source"
/// affordance.
/// </summary>
public sealed class SuiSetReferenceDataCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly SuiReferenceData _before;
	private readonly SuiReferenceData _after;

	public string Description => "Set SuiReference data";

	public SuiSetReferenceDataCommand( string elementId, SuiReferenceData before, SuiReferenceData after )
	{
		_elementId = elementId;
		_before = before?.Clone();
		_after = after?.Clone();
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;
		el.SuiReference = _after?.Clone();
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;
		el.SuiReference = _before?.Clone();
	}
}
