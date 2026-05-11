using Editor;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Shared style for thin, minimal scrollbars used by every panel (Palette,
/// Hierarchy, future Details, etc). 4px track, no arrows, semi-transparent
/// thumb that brightens on hover.
///
/// IMPORTANT: setting Qt stylesheets on the parent <see cref="ScrollArea"/>
/// does NOT cascade to the child <c>QScrollBar</c> widgets. Use
/// <see cref="ApplyTo"/> after creating a ScrollArea so the styles land on
/// the actual scrollbar widgets and FixedWidth/Height force the track size.
/// </summary>
internal static class SuiScrollStyle
{
	/// <summary>Apply the thin-scrollbar look to a <see cref="ScrollArea"/>.</summary>
	public static void ApplyTo( ScrollArea scroll )
	{
		if ( scroll == null ) return;

		scroll.SetStyles( "background-color: transparent; border: none;" );

		// All our panels are vertical-only — kill the horizontal scrollbar so
		// it can't appear when a child briefly exceeds the canvas width.
		scroll.HorizontalScrollbarMode = ScrollbarMode.Off;
		scroll.VerticalScrollbarMode = ScrollbarMode.Auto;

		if ( scroll.VerticalScrollbar.IsValid )
		{
			scroll.VerticalScrollbar.FixedWidth = 5;
			scroll.VerticalScrollbar.SetStyles( ScrollbarStylesheet );
		}
		if ( scroll.HorizontalScrollbar.IsValid )
		{
			scroll.HorizontalScrollbar.FixedHeight = 5;
			scroll.HorizontalScrollbar.SetStyles( ScrollbarStylesheet );
		}
	}

	private const string ScrollbarStylesheet =
		"background-color: transparent; border: none;" +

		// Vertical scrollbar — 5px track, 4px breathing room top/bottom so the
		// thumb's rounded corners aren't clipped at the edges.
		"QScrollBar:vertical {" +
			"width: 5px;" +
			"background: transparent;" +
			"border: none;" +
			"margin: 4px 0 4px 0;" +
		"}" +
		"QScrollBar::handle:vertical {" +
			"background-color: rgba(255, 255, 255, 0.18);" +
			"border-radius: 6px;" +
			"min-height: 24px;" +
		"}" +
		"QScrollBar::handle:vertical:hover {" +
			"background-color: rgba(255, 255, 255, 0.32);" +
		"}" +
		"QScrollBar::handle:vertical:pressed {" +
			"background-color: rgba(255, 255, 255, 0.45);" +
		"}" +
		// Hide arrow buttons.
		"QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical {" +
			"height: 0;" +
			"border: none;" +
			"background: transparent;" +
		"}" +
		"QScrollBar::add-page:vertical, QScrollBar::sub-page:vertical {" +
			"background: transparent;" +
		"}" +

		// Horizontal scrollbar — same look, 5px track, 4px breathing room
		// left/right so the thumb's rounded corners show fully.
		"QScrollBar:horizontal {" +
			"height: 5px;" +
			"background: transparent;" +
			"border: none;" +
			"margin: 0 4px 0 4px;" +
		"}" +
		"QScrollBar::handle:horizontal {" +
			"background-color: rgba(255, 255, 255, 0.18);" +
			"border-radius: 6px;" +
			"min-width: 24px;" +
		"}" +
		"QScrollBar::handle:horizontal:hover {" +
			"background-color: rgba(255, 255, 255, 0.32);" +
		"}" +
		"QScrollBar::handle:horizontal:pressed {" +
			"background-color: rgba(255, 255, 255, 0.45);" +
		"}" +
		"QScrollBar::add-line:horizontal, QScrollBar::sub-line:horizontal {" +
			"width: 0;" +
			"border: none;" +
			"background: transparent;" +
		"}" +
		"QScrollBar::add-page:horizontal, QScrollBar::sub-page:horizontal {" +
			"background: transparent;" +
		"}";
}
