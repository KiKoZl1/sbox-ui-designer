using System;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// V1.5 M3.5 (PRD 25 § 5) — embedded editor for a single
/// <see cref="SuiInteractiveStateStyle"/>. Used inside the per-tab
/// content of <see cref="SuiDetailsWidget"/>'s Button States section.
/// One instance per tab (Hover / Pressed / Disabled / Focused).
///
/// Self-contained: holds its own rows + a Clear button that nulls the
/// owning <see cref="SuiInteractiveStateStyle"/> reference. Edits flow
/// through the <paramref name="mutate"/> callback so the parent widget
/// can route them through the undo command stack.
/// </summary>
public sealed class SuiInteractiveStateEditor : Widget
{
	private readonly Func<SuiInteractiveStateStyle> _read;
	private readonly Action<Action<SuiInteractiveStateStyle>, string> _mutate;
	private readonly Action _onClear;

	private readonly Action<string, string, Action<string>> _addImage;
	private readonly Action<string, string, Action<string>> _addColor;
	private readonly Action<string, float, Action<float>> _addFloat;

	/// <summary>
	/// Builds an editor for one override state.
	/// <para>The <paramref name="read"/> delegate returns the current style
	/// instance (may be null when the override hasn't been authored yet).
	/// The first edit auto-creates the instance via <paramref name="mutate"/>.</para>
	/// <para>The Add* delegates come from <see cref="SuiDetailsWidget"/> so this
	/// embedded editor matches the rest of the panel (paired rows, color swatch,
	/// undo-tracked commits).</para>
	/// </summary>
	public SuiInteractiveStateEditor(
		Widget parent,
		Func<SuiInteractiveStateStyle> read,
		Action<Action<SuiInteractiveStateStyle>, string> mutate,
		Action onClear,
		Action<string, string, Action<string>> addImage,
		Action<string, string, Action<string>> addColor,
		Action<string, float, Action<float>> addFloat ) : base( parent )
	{
		_read = read;
		_mutate = mutate;
		_onClear = onClear;
		_addImage = addImage;
		_addColor = addColor;
		_addFloat = addFloat;

		Layout = Layout.Column();
		Layout.Margin = new Sandbox.UI.Margin( 4, 6, 4, 6 );
		Layout.Spacing = 0;

		Build();
	}

	private void Build()
	{
		var style = _read();

		// Top row: Clear State button — disabled when the state is null/empty
		// (nothing to clear).
		var topRow = new Widget( this );
		topRow.Layout = Layout.Row();
		topRow.Layout.Margin = new Sandbox.UI.Margin( 0, 0, 0, 4 );
		topRow.Layout.AddStretchCell( 1 );

		var clearBtn = new Button( "Clear State", "clear", topRow );
		clearBtn.ToolTip = "Remove every override field on this state — falls back to the element's Normal visuals.";
		clearBtn.FixedHeight = 22;
		clearBtn.Enabled = style != null && !style.IsEmpty();
		clearBtn.Clicked += () => _onClear?.Invoke();
		topRow.Layout.Add( clearBtn );

		Layout.Add( topRow );

		// Eight fields. Empty strings + sentinel floats (-1 / 1) mean "no
		// override" — Normal cascades through unchanged.
		_addImage( "Background Image", style?.BackgroundImage ?? "", v =>
			_mutate( s => s.BackgroundImage = v ?? "", "Set state background image" ) );

		_addColor( "Background Color", style?.BackgroundColor ?? "", v =>
			_mutate( s => s.BackgroundColor = v ?? "", "Set state background color" ) );

		_addColor( "Border Color", style?.BorderColor ?? "", v =>
			_mutate( s => s.BorderColor = v ?? "", "Set state border color" ) );

		_addFloat( "Border Width", style?.BorderWidth ?? -1f, v =>
			_mutate( s => s.BorderWidth = v, "Set state border width" ) );

		_addFloat( "Border Radius", style?.BorderRadius ?? -1f, v =>
			_mutate( s => s.BorderRadius = v, "Set state border radius" ) );

		_addColor( "Text Color", style?.TextColor ?? "", v =>
			_mutate( s => s.TextColor = v ?? "", "Set state text color" ) );

		_addFloat( "Scale", style?.Scale ?? 1f, v =>
			_mutate( s => s.Scale = v, "Set state scale" ) );

		_addFloat( "Opacity", style?.Opacity ?? -1f, v =>
			_mutate( s => s.Opacity = v, "Set state opacity" ) );

		// Hint footer.
		var hint = new Label(
			"Empty strings inherit Normal. Border Width / Radius / Opacity = -1 means no override.",
			this );
		hint.WordWrap = true;
		hint.SetStyles( "color: rgb(120,125,135); font-size: 10px; padding: 6px 4px 0 4px;" );
		Layout.Add( hint );
	}
}
