using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// V1.5 M3 — revert an event slot to "Not bound" by removing its entry from
/// the element's <see cref="SuiElement.Events"/> map (PRD 20 § 4.1). The
/// prior binding is captured so Undo can put it back exactly as it was.
/// </summary>
public sealed class SuiClearEventCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly string _eventName;

	private SuiEventBinding _previousBinding;
	private bool _previousHadEntry;

	public string Description => $"Clear event '{_eventName}'";

	public SuiClearEventCommand( string elementId, string eventName )
	{
		_elementId = elementId;
		_eventName = eventName;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el?.Events == null || string.IsNullOrEmpty( _eventName ) ) return;

		_previousHadEntry = el.Events.TryGetValue( _eventName, out var prev );
		_previousBinding = prev?.Clone();

		el.Events.Remove( _eventName );
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;

		if ( !_previousHadEntry || _previousBinding == null ) return;

		el.Events ??= new System.Collections.Generic.Dictionary<string, SuiEventBinding>();
		el.Events[_eventName] = _previousBinding.Clone();
	}
}
