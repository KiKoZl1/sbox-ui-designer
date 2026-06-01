using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// V1.5 M3 — closed-set table of which events each <see cref="SuiElementType"/>
/// surfaces (PRD 20 § 3.1). The validator rejects events outside this matrix
/// at save time; the Designer Details panel iterates the matrix to know which
/// rows to show on a given element.
///
/// <para>Each event entry carries:
/// <list type="bullet">
///   <item>The Razor attribute the generator emits (<c>"onclick"</c>, <c>"onhover"</c>…)</item>
///   <item>The delegate signature in Code mode (<c>Action</c>, <c>Action&lt;float&gt;</c>…)</item>
///   <item>The parameter name to show in the generated stub when typed
///         (e.g. <c>void OnX(float value)</c> instead of <c>void OnX(float t)</c>)</item>
/// </list>
/// Adding a new event row is forward-compat (additive). Renaming or removing
/// is a breaking change requiring schema migration.</para>
/// </summary>
public static class SuiEventMatrix
{
	public readonly struct Entry
	{
		/// <summary>"OnClick", "OnValueChanged" — what the user sees in the Designer + what's keyed in <see cref="SuiElement.Events"/>.</summary>
		public string Name { get; init; }

		/// <summary>Razor attribute that fires this event ("onclick", "onhover", "onsubmit"…).</summary>
		public string RazorAttribute { get; init; }

		/// <summary>
		/// C# delegate signature for Code mode.
		/// <c>null</c> ⇒ <c>Action</c>; otherwise a fully-qualified type like
		/// <c>Action&lt;float&gt;</c> or <c>Action&lt;string&gt;</c>.
		/// </summary>
		public string CodeDelegate { get; init; }

		/// <summary>
		/// Parameter name in the generated Code-mode stub when the delegate is typed.
		/// Semantic name ("value", "index") reads better than generic "arg".
		/// Empty for <c>Action</c> (no arg).
		/// </summary>
		public string ParameterName { get; init; }
	}

	private static readonly Dictionary<SuiElementType, Entry[]> _matrix = Build();

	/// <summary>Empty list if the element type has no events.</summary>
	public static IReadOnlyList<Entry> For( SuiElementType type )
		=> _matrix.TryGetValue( type, out var rows ) ? rows : System.Array.Empty<Entry>();

	/// <summary>True when this element type surfaces an event called <paramref name="name"/>.</summary>
	public static bool Supports( SuiElementType type, string name )
	{
		if ( string.IsNullOrEmpty( name ) ) return false;
		foreach ( var e in For( type ) )
			if ( e.Name == name ) return true;
		return false;
	}

	public static bool TryGet( SuiElementType type, string name, out Entry entry )
	{
		foreach ( var e in For( type ) )
		{
			if ( e.Name == name )
			{
				entry = e;
				return true;
			}
		}
		entry = default;
		return false;
	}

	// Builders — every panel-like type inherits OnHover/OnUnhover. Buttons +
	// InventorySlot get clicks. Input widgets (TextEntry/Slider/Toggle/DropDown)
	// land in M4 but the matrix already declares their events so the codegen
	// path is uniform when those element types light up.
	//
	// Entries are inlined inside Build() (not declared as static fields) to
	// avoid the C# static field initialization order trap — declaring them as
	// `private static readonly` AFTER `_matrix = Build()` (textual order) meant
	// Build() read them before they were populated, producing the validator
	// false-positive "Button does not surface OnClick".
	private static Dictionary<SuiElementType, Entry[]> Build()
	{
		// Razor attribute names match Sandbox.UI's MousePanelEvent vocabulary
		// (engine UI/Input/PanelInput.cs + InputEventQueue.cs):
		// onmouseover / onmouseout fire on hover enter/exit — onhover/onunhover
		// don't exist and were silently dropped by the engine. onrightclick also
		// doesn't exist: right-click arrives via onclick with e.Button=="mouseright",
		// so until M4 surfaces MousePanelEvent handlers, OnRightClick is wired as
		// a plain onclick (fires on ANY button — known gap).
		var OnHover    = new Entry { Name = "OnHover",       RazorAttribute = "onmouseover",   CodeDelegate = null, ParameterName = "" };
		var OnUnhover  = new Entry { Name = "OnUnhover",     RazorAttribute = "onmouseout",    CodeDelegate = null, ParameterName = "" };
		var OnClick    = new Entry { Name = "OnClick",       RazorAttribute = "onclick",       CodeDelegate = null, ParameterName = "" };
		var OnRClick   = new Entry { Name = "OnRightClick",  RazorAttribute = "onclick",       CodeDelegate = null, ParameterName = "" };
		var OnDblClick = new Entry { Name = "OnDoubleClick", RazorAttribute = "ondoubleclick", CodeDelegate = null, ParameterName = "" };

		var panelLike = new[] { OnHover, OnUnhover, OnClick, OnRClick };
		var button    = new[] { OnClick, OnHover, OnUnhover };
		var slot      = new[] { OnClick, OnRClick, OnDblClick, OnHover };

		return new Dictionary<SuiElementType, Entry[]>
		{
			[SuiElementType.Button]         = button,
			[SuiElementType.Panel]          = panelLike,
			[SuiElementType.Overlay]        = panelLike,
			[SuiElementType.HorizontalBox]  = panelLike,
			[SuiElementType.VerticalBox]    = panelLike,
			[SuiElementType.Grid]           = panelLike,
			[SuiElementType.ScrollPanel]    = panelLike,
			[SuiElementType.Hotbar]         = panelLike,
			[SuiElementType.InventoryGrid]  = panelLike,
			[SuiElementType.InventorySlot]  = slot,
			[SuiElementType.ItemIcon]       = panelLike,
			[SuiElementType.Tooltip]        = panelLike,
			// Text / Image / ProgressBar / SuiReference / Canvas — no events.
			// Input widgets (TextEntry / Slider / Toggle / DropDown) land in M4
			// — their matrix rows get added when those SuiElementType entries
			// are introduced.
		};
	}
}
