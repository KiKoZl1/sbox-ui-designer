using System;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Generic property mutation — caller supplies a getter/setter pair scoped to
/// the element. Captures the old value at Apply time so Undo can reverse cleanly.
///
/// Usage:
/// <code>
/// new SuiSetPropertyCommand&lt;float&gt;(
///     element.Id,
///     el =&gt; el.Style.Opacity,
///     (el, v) =&gt; el.Style.Opacity = v,
///     0.5f,
///     "Set opacity"
/// );
/// </code>
/// </summary>
public sealed class SuiSetPropertyCommand<T> : ISuiCommand
{
	private readonly string _elementId;
	private readonly Func<SuiElement, T> _getter;
	private readonly Action<SuiElement, T> _setter;
	private readonly T _newValue;
	private readonly string _description;
	private T _oldValue;

	public string Description => _description;

	public SuiSetPropertyCommand(
		string elementId,
		Func<SuiElement, T> getter,
		Action<SuiElement, T> setter,
		T newValue,
		string description )
	{
		_elementId = elementId;
		_getter = getter;
		_setter = setter;
		_newValue = newValue;
		_description = description ?? "Set property";
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;
		_oldValue = _getter( el );
		_setter( el, _newValue );
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;
		_setter( el, _oldValue );
	}
}
