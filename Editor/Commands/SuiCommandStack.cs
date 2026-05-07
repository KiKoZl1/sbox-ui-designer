using System;
using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Two-stack undo/redo manager. Commands are pushed onto the undo stack as
/// they are applied; pushing always clears the redo stack.
/// </summary>
public sealed class SuiCommandStack
{
	private readonly Stack<ISuiCommand> _undo = new();
	private readonly Stack<ISuiCommand> _redo = new();

	/// <summary>Maximum number of commands kept in the undo stack. 0 = unlimited.</summary>
	public int MaxDepth { get; set; } = 256;

	public bool CanUndo => _undo.Count > 0;
	public bool CanRedo => _redo.Count > 0;

	public string UndoName => CanUndo ? _undo.Peek().Description : null;
	public string RedoName => CanRedo ? _redo.Peek().Description : null;

	public event Action Changed;

	public void Push( ISuiCommand cmd, SuiDocument doc )
	{
		if ( cmd == null || doc == null ) return;
		cmd.Apply( doc );
		_undo.Push( cmd );
		_redo.Clear();
		TrimDepth();
		Changed?.Invoke();
	}

	public void Undo( SuiDocument doc )
	{
		if ( !CanUndo || doc == null ) return;
		var cmd = _undo.Pop();
		cmd.Undo( doc );
		_redo.Push( cmd );
		Changed?.Invoke();
	}

	public void Redo( SuiDocument doc )
	{
		if ( !CanRedo || doc == null ) return;
		var cmd = _redo.Pop();
		cmd.Apply( doc );
		_undo.Push( cmd );
		Changed?.Invoke();
	}

	public void Clear()
	{
		_undo.Clear();
		_redo.Clear();
		Changed?.Invoke();
	}

	private void TrimDepth()
	{
		if ( MaxDepth <= 0 ) return;
		while ( _undo.Count > MaxDepth )
		{
			// Stack<T> doesn't support trimming the bottom; rebuild.
			var arr = _undo.ToArray();
			_undo.Clear();
			for ( int i = arr.Length - 2; i >= 0; i-- )
				_undo.Push( arr[i] );
		}
	}
}
