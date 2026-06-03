using System.Collections.Generic;
using Sandbox;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// The converter catalog (PRD 18 § 5.3) — the editor reads this to populate the
/// bind-popup converter dropdown and to validate converter chains.
///
/// M1-A ships the built-in chips as an explicit hand-registered list: bulletproof,
/// no reflection fragility, and the signatures are forward-compat-locked anyway
/// (C3 — a built-in ref never changes). M1-C adds user <c>[SuiConverter]</c>
/// discovery via TypeLibrary and merges those entries alongside the built-ins.
/// </summary>
public static class SuiConverterCatalog
{
	private static List<SuiConverterMetadata> _builtins;

	/// <summary>Metadata for every shipped <see cref="SuiBuiltinConverters"/> chip (PRD 18 § 5.2).</summary>
	public static IReadOnlyList<SuiConverterMetadata> GetBuiltins()
		=> _builtins ??= BuildBuiltins();

	/// <summary>Look up a converter by ref — checks built-ins first, then user-discovered converters.</summary>
	public static SuiConverterMetadata Find( string converterRef )
	{
		if ( string.IsNullOrEmpty( converterRef ) ) return null;
		foreach ( var m in GetBuiltins() )
			if ( m.Ref == converterRef ) return m;
		foreach ( var m in GetUserConverters() )
			if ( m.Ref == converterRef ) return m;
		return null;
	}

	/// <summary>
	/// Every converter the bind popup can offer — built-ins plus any
	/// <c>[SuiConverter]</c> methods discovered in the user's project assembly
	/// (PRD 18 § 5.4). Re-runs the discovery on every call so newly-scaffolded
	/// converters appear immediately after the engine recompiles.
	/// </summary>
	public static IReadOnlyList<SuiConverterMetadata> GetAll()
	{
		var users = GetUserConverters();
		var built = GetBuiltins();
		if ( users.Count == 0 ) return built;
		var combined = new List<SuiConverterMetadata>( built.Count + users.Count );
		combined.AddRange( built );
		combined.AddRange( users );
		return combined;
	}

	/// <summary>
	/// User <c>[SuiConverter]</c> methods discovered via <c>TypeLibrary</c>.
	/// M1-D scope: only the <c>Game.GameConverters</c> container scaffolded by
	/// <c>SuiUserConverterScaffolder</c>. Arbitrary-type discovery is M3 polish.
	/// </summary>
	public static IReadOnlyList<SuiConverterMetadata> GetUserConverters()
	{
		var list = new List<SuiConverterMetadata>();
		try
		{
			var typeDesc = TypeLibrary.GetType( "Game.GameConverters" );
			if ( typeDesc == null ) return list;

			foreach ( var method in typeDesc.Methods )
			{
				if ( method == null ) continue;
				if ( !method.IsStatic || !method.IsPublic ) continue;

				var attr = method.GetCustomAttribute<SuiConverterAttribute>();
				if ( attr == null ) continue;

				var displayName = string.IsNullOrEmpty( attr.DisplayName ) ? method.Name : attr.DisplayName;
				var category    = string.IsNullOrEmpty( attr.Category ) ? "User" : attr.Category;
				var returnType  = method.ReturnType?.Name ?? "object";

				var inputs = new List<SuiConverterParam>();
				foreach ( var p in method.Parameters )
				{
					inputs.Add( new SuiConverterParam
					{
						Name = p.Name,
						Type = p.ParameterType?.Name ?? "object",
						HasDefault = false,
					} );
				}

				list.Add( new SuiConverterMetadata
				{
					Ref         = "user.Game.GameConverters." + method.Name,
					DisplayName = displayName,
					Category    = category,
					Description = attr.Description ?? "",
					Inputs      = inputs.ToArray(),
					ReturnType  = returnType,
					IsBuiltin   = false,
				} );
			}
		}
		catch ( System.Exception e )
		{
			Log.Warning( $"[SUI] Custom converter discovery failed: {e.Message}" );
		}
		return list;
	}

	private static List<SuiConverterMetadata> BuildBuiltins()
	{
		var list = new List<SuiConverterMetadata>();

		void C( string display, string category, string desc, string returnType,
			params (string name, string type, bool hasDefault)[] inputs )
		{
			var ps = new SuiConverterParam[inputs.Length];
			for ( int i = 0; i < inputs.Length; i++ )
			{
				ps[i] = new SuiConverterParam
				{
					Name = inputs[i].name,
					Type = inputs[i].type,
					HasDefault = inputs[i].hasDefault,
				};
			}

			list.Add( new SuiConverterMetadata
			{
				Ref = "builtin." + display,
				DisplayName = display,
				Category = category,
				Description = desc,
				Inputs = ps,
				ReturnType = returnType,
				IsBuiltin = true,
			} );
		}

		// ── Math ──
		C( "Add", "Math", "a + b", "Single", ("a", "Single", false), ("b", "Single", false) );
		C( "Subtract", "Math", "a - b", "Single", ("a", "Single", false), ("b", "Single", false) );
		C( "Multiply", "Math", "a * b", "Single", ("a", "Single", false), ("b", "Single", false) );
		C( "Divide", "Math", "numerator / denominator (0 if denominator is 0)", "Single", ("numerator", "Single", false), ("denominator", "Single", false) );
		C( "Modulo", "Math", "a % b (0 if b is 0)", "Single", ("a", "Single", false), ("b", "Single", false) );
		C( "Power", "Math", "baseV raised to exp", "Single", ("baseV", "Single", false), ("exp", "Single", false) );
		C( "Absolute", "Math", "Absolute value of v", "Single", ("v", "Single", false) );
		C( "Negate", "Math", "-v", "Single", ("v", "Single", false) );

		// ── Range / Interpolation ──
		C( "Clamp", "Range", "Clamp v between min and max", "Single", ("v", "Single", false), ("min", "Single", false), ("max", "Single", false) );
		C( "Clamp01", "Range", "Clamp v between 0 and 1", "Single", ("v", "Single", false) );
		C( "Map", "Range", "Remap v from one range to another", "Single", ("v", "Single", false), ("fromMin", "Single", false), ("fromMax", "Single", false), ("toMin", "Single", false), ("toMax", "Single", false) );
		C( "Lerp", "Range", "Linear interpolation a to b by t", "Single", ("a", "Single", false), ("b", "Single", false), ("t", "Single", false) );
		C( "InverseLerp", "Range", "Where v falls in the a to b range (0..1)", "Single", ("a", "Single", false), ("b", "Single", false), ("v", "Single", false) );
		C( "SmoothStep", "Range", "Smooth (eased) interpolation a to b by t", "Single", ("a", "Single", false), ("b", "Single", false), ("t", "Single", false) );
		C( "Round", "Range", "Round v to the nearest integer", "Int32", ("v", "Single", false) );
		C( "Floor", "Range", "Round v down to an integer", "Int32", ("v", "Single", false) );
		C( "Ceil", "Range", "Round v up to an integer", "Int32", ("v", "Single", false) );

		// ── Type conversion ──
		C( "IntToFloat", "Conversion", "int to float", "Single", ("v", "Int32", false) );
		C( "FloatToInt", "Conversion", "float to int with a rounding mode", "Int32", ("v", "Single", false), ("mode", "FloatToIntMode", true) );
		C( "LongToInt", "Conversion", "long to int", "Int32", ("v", "Int64", false) );
		C( "IntToString", "Conversion", "int to string", "String", ("v", "Int32", false) );
		C( "FloatToString", "Conversion", "float to string with a fixed decimal count", "String", ("v", "Single", false), ("decimals", "Int32", true) );
		C( "Parse", "Conversion", "string to float (0 if unparseable)", "Single", ("v", "String", false) );

		// ── Comparison / Logic ──
		C( "Equal", "Logic", "a == b", "Boolean", ("a", "T", false), ("b", "T", false) );
		C( "NotEqual", "Logic", "a != b", "Boolean", ("a", "T", false), ("b", "T", false) );
		C( "GreaterThan", "Logic", "a > b", "Boolean", ("a", "Single", false), ("b", "Single", false) );
		C( "LessThan", "Logic", "a < b", "Boolean", ("a", "Single", false), ("b", "Single", false) );
		C( "GreaterOrEqual", "Logic", "a >= b", "Boolean", ("a", "Single", false), ("b", "Single", false) );
		C( "LessOrEqual", "Logic", "a <= b", "Boolean", ("a", "Single", false), ("b", "Single", false) );
		C( "If", "Logic", "cond ? ifTrue : ifFalse", "T", ("cond", "Boolean", false), ("ifTrue", "T", false), ("ifFalse", "T", false) );
		C( "And", "Logic", "a && b", "Boolean", ("a", "Boolean", false), ("b", "Boolean", false) );
		C( "Or", "Logic", "a || b", "Boolean", ("a", "Boolean", false), ("b", "Boolean", false) );
		C( "Not", "Logic", "!v", "Boolean", ("v", "Boolean", false) );

		// ── String ──
		C( "Concat", "String", "a + b", "String", ("a", "String", false), ("b", "String", false) );
		C( "Format", "String", "string.Format(template, args)", "String", ("template", "String", false), ("args", "object[]", false) );
		C( "Uppercase", "String", "Convert v to UPPERCASE", "String", ("v", "String", false) );
		C( "Lowercase", "String", "Convert v to lowercase", "String", ("v", "String", false) );
		C( "TitleCase", "String", "Capitalise The First Letter Of Each Word", "String", ("v", "String", false) );
		C( "Length", "String", "Character count of v", "Int32", ("v", "String", false) );
		C( "Substring", "String", "Slice of v from start, length chars", "String", ("v", "String", false), ("start", "Int32", false), ("length", "Int32", false) );
		C( "Replace", "String", "Replace every oldStr in v with newStr", "String", ("v", "String", false), ("oldStr", "String", false), ("newStr", "String", false) );
		C( "Contains", "String", "True if haystack contains needle", "Boolean", ("haystack", "String", false), ("needle", "String", false) );
		C( "StartsWith", "String", "True if s starts with prefix", "Boolean", ("s", "String", false), ("prefix", "String", false) );
		C( "EndsWith", "String", "True if s ends with suffix", "Boolean", ("s", "String", false), ("suffix", "String", false) );
		C( "Trim", "String", "Strip leading + trailing whitespace from s", "String", ("s", "String", false) );
		C( "IndexOf", "String", "Index of needle in s, or -1 if not found", "Int32", ("s", "String", false), ("needle", "String", false) );
		C( "Split", "String", "Split s on every occurrence of delimiter", "String[]", ("s", "String", false), ("delimiter", "String", false) );

		// ── Color ──
		C( "MakeColor", "Color", "Build a Color from r, g, b, a (0..1)", "Color", ("r", "Single", false), ("g", "Single", false), ("b", "Single", false), ("a", "Single", false) );
		C( "ColorFromHex", "Color", "Parse a hex string into a Color", "Color", ("hex", "String", false) );
		C( "ColorLerp", "Color", "Linear interpolation between two colors", "Color", ("a", "Color", false), ("b", "Color", false), ("t", "Single", false) );
		C( "WithAlpha", "Color", "Color c with its alpha replaced", "Color", ("c", "Color", false), ("a", "Single", false) );
		C( "Darken", "Color", "Darken c by amount (0..1)", "Color", ("c", "Color", false), ("amount", "Single", false) );
		C( "Lighten", "Color", "Lighten c toward white by amount (0..1)", "Color", ("c", "Color", false), ("amount", "Single", false) );

		// ── Collection ──
		C( "Count", "Collection", "Number of items in list", "Int32", ("list", "List<T>", false) );
		C( "IsEmpty", "Collection", "True if list is null or empty", "Boolean", ("list", "List<T>", false) );
		C( "First", "Collection", "First item, or fallback if empty", "T", ("list", "List<T>", false), ("fallback", "T", false) );
		C( "Last", "Collection", "Last item, or fallback if empty", "T", ("list", "List<T>", false), ("fallback", "T", false) );
		C( "ElementAt", "Collection", "Item at index, or fallback if out of range", "T", ("list", "List<T>", false), ("index", "Int32", false), ("fallback", "T", false) );
		C( "Slice", "Collection", "Sub-list of count items starting at start", "List<T>", ("list", "List<T>", false), ("start", "Int32", false), ("count", "Int32", false) );

		return list;
	}
}
