using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>Remove a <see cref="SuiBinding"/> from an element, remembering its index for undo.</summary>
public sealed class SuiClearBindingCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly SuiBinding _binding;
	private int _index = -1;

	public string Description => $"Unbind {_binding?.Property}";

	public SuiClearBindingCommand( string elementId, SuiBinding binding )
	{
		_elementId = elementId;
		_binding = binding;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el?.Bindings == null || _binding == null ) return;
		_index = el.Bindings.IndexOf( _binding );
		if ( _index >= 0 ) el.Bindings.RemoveAt( _index );
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el?.Bindings == null || _binding == null || _index < 0 ) return;
		var i = _index > el.Bindings.Count ? el.Bindings.Count : _index;
		el.Bindings.Insert( i, _binding );
	}
}
