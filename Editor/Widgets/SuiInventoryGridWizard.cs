using System;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Modal wizard for creating an InventoryGrid (mockup Image 5). Asks for
/// grid dimensions + slot size + gap + anchor + slot style, shows a mini
/// preview, and on accept builds the grid + populates with empty slots.
/// </summary>
public sealed class SuiInventoryGridWizard : Window
{
	public Action<InventoryGridConfig> OnCreate;

	private int _columns = 8;
	private int _rows = 4;
	private float _slotSize = 64;
	private float _gap = 8;
	private SuiAnchor _anchor = SuiAnchor.MiddleCenter;

	private LineEdit _colsEdit, _rowsEdit, _sizeEdit, _gapEdit;
	private SuiAnchorPicker _anchorPicker;
	private Widget _preview;
	private Label _sizeLabel, _totalLabel;

	public SuiInventoryGridWizard()
	{
		Title = "Create Inventory Grid";
		WindowTitle = "Create Inventory Grid";
		Size = new Vector2( 540, 420 );
		MinimumSize = new Vector2( 540, 420 );
		SetWindowIcon( "grid_view" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 12;
		Canvas.Layout.Spacing = 8;

		BuildLayout();
		Show();
	}

	private void BuildLayout()
	{
		var twoCol = new Widget( Canvas );
		twoCol.Layout = Layout.Row();
		twoCol.Layout.Spacing = 16;

		// Left: settings.
		var settings = new Widget( twoCol );
		settings.Layout = Layout.Column();
		settings.Layout.Spacing = 6;

		var settingsHeader = new Label( "Grid Settings", settings );
		settingsHeader.SetStyles( "color: #9ca3af; font-size: 11px; font-weight: 600; letter-spacing: 1px;" );
		settings.Layout.Add( settingsHeader );

		_colsEdit = AddIntField( settings, "Columns", _columns, v => { _columns = v; RefreshPreview(); } );
		_rowsEdit = AddIntField( settings, "Rows", _rows, v => { _rows = v; RefreshPreview(); } );
		_sizeEdit = AddFloatField( settings, "Slot Size (px)", _slotSize, v => { _slotSize = v; RefreshPreview(); } );
		_gapEdit = AddFloatField( settings, "Gap (px)", _gap, v => { _gap = v; RefreshPreview(); } );

		var anchorRow = new Widget( settings );
		anchorRow.Layout = Layout.Row();
		anchorRow.Layout.Spacing = 8;
		var anchorLbl = new Label( "Anchor", anchorRow );
		anchorLbl.FixedWidth = 96;
		anchorLbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		anchorRow.Layout.Add( anchorLbl );
		_anchorPicker = new SuiAnchorPicker( anchorRow );
		_anchorPicker.SetCurrent( _anchor );
		_anchorPicker.AnchorSelected = a => { _anchor = a; };
		anchorRow.Layout.Add( _anchorPicker );
		anchorRow.Layout.AddStretchCell();
		settings.Layout.Add( anchorRow );

		twoCol.Layout.Add( settings, 1 );

		// Right: preview.
		var previewCol = new Widget( twoCol );
		previewCol.Layout = Layout.Column();
		previewCol.Layout.Spacing = 6;

		var previewHeader = new Label( "Preview", previewCol );
		previewHeader.SetStyles( "color: #9ca3af; font-size: 11px; font-weight: 600; letter-spacing: 1px;" );
		previewCol.Layout.Add( previewHeader );

		_preview = new GridPreview( previewCol, this );
		_preview.MinimumSize = new Vector2( 240, 180 );
		previewCol.Layout.Add( _preview, 1 );

		_sizeLabel = new Label( "", previewCol );
		_sizeLabel.SetStyles( "color: #d1d5db; font-size: 11px;" );
		previewCol.Layout.Add( _sizeLabel );

		_totalLabel = new Label( "", previewCol );
		_totalLabel.SetStyles( "color: #d1d5db; font-size: 11px;" );
		previewCol.Layout.Add( _totalLabel );

		twoCol.Layout.Add( previewCol, 1 );

		Canvas.Layout.Add( twoCol, 1 );

		// Footer buttons.
		var footer = new Widget( Canvas );
		footer.Layout = Layout.Row();
		footer.Layout.Spacing = 8;
		footer.FixedHeight = 32;
		footer.Layout.AddStretchCell();
		var cancelBtn = new Button( "Cancel", "close", footer );
		cancelBtn.Clicked += Close;
		footer.Layout.Add( cancelBtn );
		var createBtn = new Button( "Create Grid", "check", footer );
		createBtn.SetStyles( "background-color: #2563eb; color: white;" );
		createBtn.Clicked += () =>
		{
			OnCreate?.Invoke( new InventoryGridConfig
			{
				Columns = _columns,
				Rows = _rows,
				SlotSize = _slotSize,
				Gap = _gap,
				Anchor = _anchor,
			} );
			Close();
		};
		footer.Layout.Add( createBtn );
		Canvas.Layout.Add( footer );

		RefreshPreview();
	}

	private LineEdit AddIntField( Widget parent, string label, int initial, Action<int> onChange )
	{
		var row = new Widget( parent );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 6;
		var lbl = new Label( label, row );
		lbl.FixedWidth = 96;
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );
		var le = new LineEdit( row );
		le.Text = initial.ToString();
		le.MaximumWidth = 100;
		le.EditingFinished += () =>
		{
			if ( int.TryParse( le.Text, out var v ) ) onChange( Math.Max( 1, v ) );
		};
		row.Layout.Add( le, 1 );
		parent.Layout.Add( row );
		return le;
	}

	private LineEdit AddFloatField( Widget parent, string label, float initial, Action<float> onChange )
	{
		var row = new Widget( parent );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 6;
		var lbl = new Label( label, row );
		lbl.FixedWidth = 96;
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );
		var le = new LineEdit( row );
		le.Text = initial.ToString();
		le.MaximumWidth = 100;
		le.EditingFinished += () =>
		{
			if ( float.TryParse( le.Text, out var v ) ) onChange( MathF.Max( 1, v ) );
		};
		row.Layout.Add( le, 1 );
		parent.Layout.Add( row );
		return le;
	}

	private void RefreshPreview()
	{
		var w = _columns * _slotSize + (_columns - 1) * _gap;
		var h = _rows * _slotSize + (_rows - 1) * _gap;
		if ( _sizeLabel != null ) _sizeLabel.Text = $"Grid Size: {(int)w} × {(int)h}";
		if ( _totalLabel != null ) _totalLabel.Text = $"Total Slots: {_columns * _rows}";
		_preview?.Update();
	}

	public sealed class InventoryGridConfig
	{
		public int Columns;
		public int Rows;
		public float SlotSize;
		public float Gap;
		public SuiAnchor Anchor;
	}

	private sealed class GridPreview : Widget
	{
		private readonly SuiInventoryGridWizard _wizard;
		public GridPreview( Widget parent, SuiInventoryGridWizard wizard ) : base( parent )
		{
			_wizard = wizard;
		}

		protected override void OnPaint()
		{
			Paint.SetBrush( new Color( 0.10f, 0.10f, 0.12f ) );
			Paint.SetPen( new Color( 0.30f, 0.30f, 0.35f ), 1 );
			Paint.DrawRect( LocalRect, 4 );

			var cols = _wizard._columns;
			var rows = _wizard._rows;
			if ( cols <= 0 || rows <= 0 ) return;

			// Fit grid into available area with uniform scale.
			var availW = LocalRect.Width - 16;
			var availH = LocalRect.Height - 16;
			var slotW = (availW - (cols - 1) * 2) / cols;
			var slotH = (availH - (rows - 1) * 2) / rows;
			var slot = MathF.Min( slotW, slotH );
			var totalW = cols * slot + (cols - 1) * 2;
			var totalH = rows * slot + (rows - 1) * 2;
			var ox = LocalRect.Left + (LocalRect.Width - totalW) * 0.5f;
			var oy = LocalRect.Top + (LocalRect.Height - totalH) * 0.5f;

			Paint.SetPen( new Color( 0.55f, 0.55f, 0.65f ), 1 );
			Paint.ClearBrush();
			for ( int r = 0; r < rows; r++ )
				for ( int c = 0; c < cols; c++ )
				{
					var x = ox + c * (slot + 2);
					var y = oy + r * (slot + 2);
					Paint.DrawRect( new Rect( x, y, slot, slot ), 2 );
				}
		}
	}
}
