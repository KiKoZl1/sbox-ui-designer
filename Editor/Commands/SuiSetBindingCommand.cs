using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Add a <see cref="SuiBinding"/> to an element, or replace the existing binding
/// on the same <see cref="SuiBinding.Property"/>. The replaced binding is captured
/// so undo restores it exactly (PRD 18 § 4).
/// </summary>
public sealed class SuiSetBindingCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly SuiBinding _binding;
	private SuiBinding _replaced;
	private int _index = -1;

	public string Description => $"Bind {_binding?.Property}";

	public SuiSetBindingCommand( string elementId, SuiBinding binding )
	{
		_elementId = elementId;
		_binding = binding;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null || _binding == null ) return;
		el.Bindings ??= new();

		_index = el.Bindings.FindIndex( b => b?.Property == _binding.Property );
		if ( _index >= 0 )
		{
			_replaced = el.Bindings[_index];
			el.Bindings[_index] = _binding;
		}
		else
		{
			el.Bindings.Add( _binding );
		}
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el?.Bindings == null || _binding == null ) return;

		var idx = el.Bindings.IndexOf( _binding );
		if ( idx < 0 ) return;

		if ( _replaced != null )
			el.Bindings[idx] = _replaced; // restore what we swapped out
		else
			el.Bindings.RemoveAt( idx ); // it was a fresh add
	}
}
