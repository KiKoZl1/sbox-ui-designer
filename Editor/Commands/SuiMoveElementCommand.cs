using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Move an absolute-mode element by setting its X/Y. For Flex children, position
/// is parent-controlled — the controller will not generate this command for them.
/// </summary>
public sealed class SuiMoveElementCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly float _newX;
	private readonly float _newY;
	private float _oldX;
	private float _oldY;

	public string Description => "Move element";

	public SuiMoveElementCommand( string elementId, float newX, float newY )
	{
		_elementId = elementId;
		_newX = newX;
		_newY = newY;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null || el.Layout == null ) return;
		_oldX = el.Layout.X;
		_oldY = el.Layout.Y;
		el.Layout.X = _newX;
		el.Layout.Y = _newY;
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null || el.Layout == null ) return;
		el.Layout.X = _oldX;
		el.Layout.Y = _oldY;
	}
}
