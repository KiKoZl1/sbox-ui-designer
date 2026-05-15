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
		public Entry( bool oneTime, bool oneWay, bool twoWay, SuiBindingMode def )
		{
			AllowOneTime = oneTime;
			AllowOneWay = oneWay;
			AllowTwoWay = twoWay;
			Default = def;
		}

		public bool AllowOneTime { get; }
		public bool AllowOneWay { get; }
		public bool AllowTwoWay { get; }
		public SuiBindingMode Default { get; }
	}

	// Keyed "<ElementType>.<Property>" — the per-type bindable properties.
	private static readonly Dictionary<string, Entry> _matrix = new()
	{
		["Text.Text"]             = new( true, true, false, SuiBindingMode.OneWay ),
		["Image.ImagePath"]       = new( true, true, false, SuiBindingMode.OneWay ),
		["Image.Tint"]            = new( true, true, false, SuiBindingMode.OneWay ),
		["Button.ButtonText"]     = new( true, true, false, SuiBindingMode.OneWay ),
		["ProgressBar.Value"]     = new( true, true, false, SuiBindingMode.OneWay ),
		["ProgressBar.FillColor"] = new( true, true, false, SuiBindingMode.OneWay ),

		// Input widgets (PRD 21) — TwoWay-capable; TwoWay is the default.
		// NOTE: the exact DropDown TwoWay target is reworked at M4 (PRD 21 M0
		// spike found DropDown has no SelectedIndex) — the entry is kept here
		// per PRD 18 § 4.5 as written and patched alongside PRD 21 at M4.
		["TextEntry.Value"]        = new( true, true, true, SuiBindingMode.TwoWay ),
		["Slider.Value"]           = new( true, true, true, SuiBindingMode.TwoWay ),
		["Toggle.Checked"]         = new( true, true, true, SuiBindingMode.TwoWay ),
		["DropDown.SelectedIndex"] = new( true, true, true, SuiBindingMode.TwoWay ),
	};

	// Properties bindable on ANY element type.
	private static readonly Dictionary<string, Entry> _universal = new()
	{
		["Visibility"]      = new( true, true, false, SuiBindingMode.OneWay ),
		["Opacity"]         = new( true, true, false, SuiBindingMode.OneWay ),
		["BackgroundColor"] = new( true, true, false, SuiBindingMode.OneWay ),
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
}
