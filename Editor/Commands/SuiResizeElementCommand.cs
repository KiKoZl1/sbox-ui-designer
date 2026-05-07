using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Resize an element by setting width/height. Negative values are clamped to 0
/// inside Apply (the validator also rejects them as a defense in depth).
/// </summary>
public sealed class SuiResizeElementCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly float _newWidth;
	private readonly float _newHeight;
	private float _oldWidth;
	private float _oldHeight;

	public string Description => "Resize element";

	public SuiResizeElementCommand( string elementId, float newWidth, float newHeight )
	{
		_elementId = elementId;
		_newWidth = newWidth < 0 ? 0 : newWidth;
		_newHeight = newHeight < 0 ? 0 : newHeight;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null || el.Layout == null ) return;
		_oldWidth = el.Layout.Width;
		_oldHeight = el.Layout.Height;
		el.Layout.Width = _newWidth;
		el.Layout.Height = _newHeight;
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null || el.Layout == null ) return;
		el.Layout.Width = _oldWidth;
		el.Layout.Height = _oldHeight;
	}
}
