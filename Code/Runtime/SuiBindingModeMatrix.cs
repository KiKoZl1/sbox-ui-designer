using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Which binding modes are valid for each (element type, property) pair, and
/// which mode is the default (PRD 18 § 4.5). Codifies the matrix so the bind
/// popup and the validator agree on a single source of truth.
/// </summary>
public static class SuiBindingModeMatrix
{
	public readonly struct Entry
	{
		public Entry( bool oneTime, bool oneWay, bool twoWay, SuiBindingMode def, string targetType = null )
		{
			AllowOneTime = oneTime;
			AllowOneWay = oneWay;
			AllowTwoWay = twoWay;
			Default = def;
			TargetType = targetType;
		}

		public bool AllowOneTime { get; }
		public bool AllowOneWay { get; }
		public bool AllowTwoWay { get; }
		public SuiBindingMode Default { get; }

		/// <summary>
		/// Expected TypeRef of the bound value (e.g. "float", "string", "Color").
		/// Drives the "Expects: X" hint in the bind popup so the user knows what
		/// Variable type the property accepts. Null when the type is open / unspecified.
		/// </summary>
		public string TargetType { get; }
	}

	// Keyed "<ElementType>.<Property>" — the per-type bindable properties.
	// Each entry is (OneTime, OneWay, TwoWay, Default, TargetType). PRD 18 § 4.5
	// listed a minimal seed; the matrix is intentionally permissive about what
	// can be bound so users can drive almost any visual property from gameplay
	// data. The TargetType drives the "Expects: X" hint and the type icon in the
	// bind popup so users immediately know what kind of Variable they need.
	private static readonly Dictionary<string, Entry> _matrix = new()
	{
		// ── Text ──
		["Text.Text"]          = new( true, true, false, SuiBindingMode.OneWay, "string" ),
		["Text.FontSize"]      = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["Text.FontFamily"]    = new( true, true, false, SuiBindingMode.OneWay, "string" ),
		["Text.FontWeight"]    = new( true, true, false, SuiBindingMode.OneWay, "string" ),
		["Text.Color"]         = new( true, true, false, SuiBindingMode.OneWay, "Color" ),
		["Text.TextAlign"]     = new( true, true, false, SuiBindingMode.OneWay, "string" ),
		["Text.LineHeight"]    = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["Text.LetterSpacing"] = new( true, true, false, SuiBindingMode.OneWay, "float" ),

		// ── Image ──
		["Image.ImagePath"] = new( true, true, false, SuiBindingMode.OneWay, "string" ),
		["Image.Tint"]      = new( true, true, false, SuiBindingMode.OneWay, "Color" ),
		["Image.FitMode"]   = new( true, true, false, SuiBindingMode.OneWay, "string" ),

		// ── Button ──
		["Button.ButtonText"] = new( true, true, false, SuiBindingMode.OneWay, "string" ),

		// ── ProgressBar ──
		["ProgressBar.Value"]     = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["ProgressBar.Min"]       = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["ProgressBar.Max"]       = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["ProgressBar.FillColor"] = new( true, true, false, SuiBindingMode.OneWay, "Color" ),
		["ProgressBar.Direction"] = new( true, true, false, SuiBindingMode.OneWay, "string" ),

		// ── Grid / InventoryGrid / Hotbar ──
		["Grid.Columns"]             = new( true, true, false, SuiBindingMode.OneWay, "int" ),
		["Grid.Rows"]                = new( true, true, false, SuiBindingMode.OneWay, "int" ),
		["Grid.CellWidth"]           = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["Grid.CellHeight"]          = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["Grid.Gap"]                 = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["InventoryGrid.Columns"]    = new( true, true, false, SuiBindingMode.OneWay, "int" ),
		["InventoryGrid.Rows"]       = new( true, true, false, SuiBindingMode.OneWay, "int" ),
		["InventoryGrid.CellWidth"]  = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["InventoryGrid.CellHeight"] = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["InventoryGrid.Gap"]        = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["Hotbar.Columns"]           = new( true, true, false, SuiBindingMode.OneWay, "int" ),

		// ── InventorySlot / ItemIcon ──
		["InventorySlot.SlotIndex"] = new( true, true, false, SuiBindingMode.OneWay, "int" ),
		["InventorySlot.IconPath"]  = new( true, true, false, SuiBindingMode.OneWay, "string" ),
		["InventorySlot.Count"]     = new( true, true, false, SuiBindingMode.OneWay, "int" ),
		["ItemIcon.ImagePath"]      = new( true, true, false, SuiBindingMode.OneWay, "string" ),
		["ItemIcon.Tint"]           = new( true, true, false, SuiBindingMode.OneWay, "Color" ),

		// ── Input widgets (PRD 21) — TwoWay-capable; TwoWay is the default ──
		// M4 fix: DropDown bind property is `Value` (Sandbox.UI engine), not
		// `SelectedIndex`. Slider supports both float and int target Variables —
		// the slider casts on read (`int` rounds, `float` passes through).
		["TextEntry.Value"]       = new( true, true, true,  SuiBindingMode.TwoWay, "string" ),
		["TextEntry.Placeholder"] = new( true, true, false, SuiBindingMode.OneWay, "string" ),
		["Slider.Value"]          = new( true, true, true,  SuiBindingMode.TwoWay, "float" ),
		["Slider.Min"]            = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["Slider.Max"]            = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["Toggle.Checked"]        = new( true, true, true,  SuiBindingMode.TwoWay, "bool" ),
		["DropDown.Value"]        = new( true, true, true,  SuiBindingMode.TwoWay, "int" ),
	};

	// Properties bindable on ANY element type — style / layout / state knobs
	// that every element exposes via SuiStyleData + SuiLayoutData.
	private static readonly Dictionary<string, Entry> _universal = new()
	{
		// State
		["Visibility"] = new( true, true, false, SuiBindingMode.OneWay, "bool" ),
		["Enabled"]    = new( true, true, false, SuiBindingMode.OneWay, "bool" ),

		// Style — colour / chrome
		["BackgroundColor"] = new( true, true, false, SuiBindingMode.OneWay, "Color" ),
		["BorderColor"]     = new( true, true, false, SuiBindingMode.OneWay, "Color" ),
		["BorderWidth"]     = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["BorderRadius"]    = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["Opacity"]         = new( true, true, false, SuiBindingMode.OneWay, "float" ),

		// Layout — size
		["Width"]  = new( true, true, false, SuiBindingMode.OneWay, "float" ),
		["Height"] = new( true, true, false, SuiBindingMode.OneWay, "float" ),
	};

	/// <summary>True if <paramref name="property"/> can be bound at all on <paramref name="elementType"/>.</summary>
	public static bool IsBindable( SuiElementType elementType, string property )
		=> TryGet( elementType, property, out _ );

	/// <summary>Look up the mode entry for a (type, property) pair — per-type rules first, then universal.</summary>
	public static bool TryGet( SuiElementType elementType, string property, out Entry entry )
	{
		if ( !string.IsNullOrEmpty( property ) )
		{
			if ( _matrix.TryGetValue( $"{elementType}.{property}", out entry ) ) return true;
			if ( _universal.TryGetValue( property, out entry ) ) return true;
		}
		entry = default;
		return false;
	}

	/// <summary>True if <paramref name="mode"/> is permitted for the (type, property) pair.</summary>
	public static bool IsModeAllowed( SuiElementType elementType, string property, SuiBindingMode mode )
	{
		if ( !TryGet( elementType, property, out var e ) ) return false;
		return mode switch
		{
			SuiBindingMode.OneTime        => e.AllowOneTime,
			SuiBindingMode.OneWay         => e.AllowOneWay,
			SuiBindingMode.TwoWay         => e.AllowTwoWay,
			SuiBindingMode.OneWayToSource => false, // reserved for V1.6
			_ => false,
		};
	}

	/// <summary>The default binding mode for a (type, property) pair, or OneWay if unknown.</summary>
	public static SuiBindingMode DefaultMode( SuiElementType elementType, string property )
		=> TryGet( elementType, property, out var e ) ? e.Default : SuiBindingMode.OneWay;

	/// <summary>
	/// The expected TypeRef of the bound value for a (type, property) pair —
	/// e.g. <c>"float"</c> for <c>ProgressBar.Value</c>, <c>"Color"</c> for
	/// <c>Image.Tint</c>. Drives the bind popup's "Expects: X" hint and type icon.
	/// Null when the matrix entry is missing or has no declared type.
	/// </summary>
	public static string GetTargetType( SuiElementType elementType, string property )
		=> TryGet( elementType, property, out var e ) ? e.TargetType : null;

	/// <summary>
	/// Every property that can be bound on <paramref name="elementType"/> — the
	/// per-type entries plus the universal ones. Drives the bind popup's property
	/// dropdown.
	/// </summary>
	public static IEnumerable<string> BindableProperties( SuiElementType elementType )
	{
		var prefix = elementType + ".";
		foreach ( var key in _matrix.Keys )
		{
			if ( key.StartsWith( prefix ) )
				yield return key.Substring( prefix.Length );
		}
		foreach ( var key in _universal.Keys )
			yield return key;
	}

	/// <summary>
	/// Which write-back triggers apply to a given (element type, property)
	/// pair. Drives the BindPopup's <c>Update Trigger</c> dropdown — when
	/// the list has only one entry the dropdown is hidden entirely (no UI
	/// noise for widgets that have no real choice). Always includes
	/// <see cref="SuiBindingUpdateTrigger.OnChange"/> + <see cref="SuiBindingUpdateTrigger.Manual"/>.
	/// </summary>
	public static IReadOnlyList<SuiBindingUpdateTrigger> AllowedUpdateTriggers(
		SuiElementType elementType, string property )
	{
		// TextEntry.Value supports per-keystroke OnChange, OnLostFocus
		// (commit on blur), OnSubmit (commit on Enter), or Manual.
		if ( elementType == SuiElementType.TextEntry && property == "Value" )
		{
			return new[]
			{
				SuiBindingUpdateTrigger.OnChange,
				SuiBindingUpdateTrigger.OnLostFocus,
				SuiBindingUpdateTrigger.OnSubmit,
				SuiBindingUpdateTrigger.Manual,
			};
		}

		// Slider.Value supports per-drag OnChange or OnRelease (defer until
		// mouse-up — useful when the bound code is expensive). Manual leaves
		// the wrapper field stale until user calls Commit.
		if ( elementType == SuiElementType.Slider && property == "Value" )
		{
			return new[]
			{
				SuiBindingUpdateTrigger.OnChange,
				SuiBindingUpdateTrigger.OnRelease,
				SuiBindingUpdateTrigger.Manual,
			};
		}

		// Everything else (Checkbox.Checked, DropDown.Value, anything else
		// reading back from the UI) is atomic — only OnChange + Manual.
		return new[]
		{
			SuiBindingUpdateTrigger.OnChange,
			SuiBindingUpdateTrigger.Manual,
		};
	}
}
