using System;

namespace SboxUiDesigner.EditorUi.Canvas;

/// <summary>
/// Sticky error banner painted at the top of the canvas viewport. Used to
/// surface compile errors / schema violations without blanking the canvas
/// content (PRD doc 09 § Hotload considerations: never blank canvas on typo).
///
/// The banner stays visible until the user clicks the X (calls OnDismiss),
/// the issue is resolved (caller clears <c>Viewport.ErrorBanner</c>), or a
/// new banner overrides it.
/// </summary>
public sealed class SuiCanvasErrorBanner
{
	/// <summary>Single-line bold title. Required.</summary>
	public string Title { get; set; }

	/// <summary>Optional second line, single-line, elided if too long.</summary>
	public string Detail { get; set; }

	/// <summary>Invoked when user clicks anywhere on the banner (excluding X).</summary>
	public Action OnClick { get; set; }

	/// <summary>Invoked when user clicks the X dismiss icon.</summary>
	public Action OnDismiss { get; set; }
}
