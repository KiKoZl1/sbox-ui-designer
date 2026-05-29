using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// V1.5 M3 — set / replace an event slot on an element (PRD 20 § 3.2).
/// Captures the prior binding so Undo restores it intact (including a clean
/// remove if the slot didn't exist before).
///
/// <para>Use <see cref="SuiClearEventCommand"/> to revert a slot back to
/// "Not bound" (removes the entry from the element's Events map).</para>
/// </summary>
public sealed class SuiSetEventCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly string _eventName;
	private readonly SuiEventBinding _newBinding;

	private SuiEventBinding _previousBinding;
	private bool _previousHadEntry;

	public string Description => $"Set event '{_eventName}'";

	public SuiSetEventCommand( string elementId, string eventName, SuiEventBinding newBinding )
	{
		_elementId = elementId;
		_eventName = eventName;
		_newBinding = newBinding;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null || string.IsNullOrEmpty( _eventName ) ) return;

		el.Events ??= new System.Collections.Generic.Dictionary<string, SuiEventBinding>();
		_previousHadEntry = el.Events.TryGetValue( _eventName, out var prev );
		_previousBinding = prev?.Clone();

		el.Events[_eventName] = _newBinding?.Clone() ?? new SuiEventBinding();
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el?.Events == null ) return;

		if ( _previousHadEntry )
			el.Events[_eventName] = _previousBinding?.Clone();
		else
			el.Events.Remove( _eventName );
	}
}
