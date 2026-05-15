using System;
using System.Collections.Generic;
using System.Text;

namespace SboxUiDesigner.Runtime;

/// <summary>Rounding mode for <see cref="SuiBuiltinConverters.FloatToInt"/>.</summary>
public enum FloatToIntMode
{
	Round,
	Floor,
	Ceil,
	Truncate,
}

/// <summary>
/// The built-in converter chip library (PRD 18 § 5.2) — pure static functions
/// the generator calls directly from emitted Razor (e.g.
/// <c>@(global::SboxUiDesigner.Runtime.SuiBuiltinConverters.Divide(Health, MaxHealth))</c>).
///
/// Every method is a pure function with no side effects. All carry
/// <see cref="SuiConverterAttribute"/> so user converters and built-ins discover
/// uniformly. Identity is <c>builtin.&lt;Name&gt;</c> (forward-compat C3 — these
/// signatures never change; a retired converter gets a new name, the old stays).
/// </summary>
public static class SuiBuiltinConverters
{
	// ──────────────────────────────── Math ────────────────────────────────

	[SuiConverter( "Add", Category = "Math", Description = "a + b" )]
	public static float Add( float a, float b ) => a + b;

	[SuiConverter( "Subtract", Category = "Math", Description = "a - b" )]
	public static float Subtract( float a, float b ) => a - b;

	[SuiConverter( "Multiply", Category = "Math", Description = "a * b" )]
	public static float Multiply( float a, float b ) => a * b;

	[SuiConverter( "Divide", Category = "Math", Description = "numerator / denominator (0 if denominator is 0)" )]
	public static float Divide( float numerator, float denominator )
		=> denominator == 0f ? 0f : numerator / denominator;

	[SuiConverter( "Modulo", Category = "Math", Description = "a % b (0 if b is 0)" )]
	public static float Modulo( float a, float b ) => b == 0f ? 0f : a % b;

	[SuiConverter( "Power", Category = "Math", Description = "baseV raised to exp" )]
	public static float Power( float baseV, float exp ) => MathF.Pow( baseV, exp );

	[SuiConverter( "Absolute", Category = "Math", Description = "Absolute value of v" )]
	public static float Absolute( float v ) => MathF.Abs( v );

	[SuiConverter( "Negate", Category = "Math", Description = "-v" )]
	public static float Negate( float v ) => -v;

	// ───────────────────────── Range / Interpolation ──────────────────────

	[SuiConverter( "Clamp", Category = "Range", Description = "Clamp v between min and max" )]
	public static float Clamp( float v, float min, float max )
		=> v < min ? min : v > max ? max : v;

	[SuiConverter( "Clamp01", Category = "Range", Description = "Clamp v between 0 and 1" )]
	public static float Clamp01( float v ) => v < 0f ? 0f : v > 1f ? 1f : v;

	[SuiConverter( "Map", Category = "Range", Description = "Remap v from one range to another" )]
	public static float Map( float v, float fromMin, float fromMax, float toMin, float toMax )
	{
		var span = fromMax - fromMin;
		if ( span == 0f ) return toMin;
		return toMin + (v - fromMin) / span * (toMax - toMin);
	}

	[SuiConverter( "Lerp", Category = "Range", Description = "Linear interpolation a→b by t" )]
	public static float Lerp( float a, float b, float t ) => a + (b - a) * t;

	[SuiConverter( "InverseLerp", Category = "Range", Description = "Where v falls in the a→b range (0..1)" )]
	public static float InverseLerp( float a, float b, float v )
	{
		var span = b - a;
		return span == 0f ? 0f : (v - a) / span;
	}

	[SuiConverter( "SmoothStep", Category = "Range", Description = "Smooth (eased) interpolation a→b by t" )]
	public static float SmoothStep( float a, float b, float t )
	{
		var u = t < 0f ? 0f : t > 1f ? 1f : t;
		u = u * u * (3f - 2f * u);
		return a + (b - a) * u;
	}

	[SuiConverter( "Round", Category = "Range", Description = "Round v to the nearest integer" )]
	public static int Round( float v ) => (int)MathF.Round( v );

	[SuiConverter( "Floor", Category = "Range", Description = "Round v down to an integer" )]
	public static int Floor( float v ) => (int)MathF.Floor( v );

	[SuiConverter( "Ceil", Category = "Range", Description = "Round v up to an integer" )]
	public static int Ceil( float v ) => (int)MathF.Ceiling( v );

	// ───────────────────────────── Type conversion ────────────────────────

	[SuiConverter( "IntToFloat", Category = "Conversion", Description = "int → float" )]
	public static float IntToFloat( int v ) => v;

	[SuiConverter( "FloatToInt", Category = "Conversion", Description = "float → int with a rounding mode" )]
	public static int FloatToInt( float v, FloatToIntMode mode = FloatToIntMode.Round ) => mode switch
	{
		FloatToIntMode.Floor    => (int)MathF.Floor( v ),
		FloatToIntMode.Ceil     => (int)MathF.Ceiling( v ),
		FloatToIntMode.Truncate => (int)v,
		_                       => (int)MathF.Round( v ),
	};

	[SuiConverter( "LongToInt", Category = "Conversion", Description = "long → int" )]
	public static int LongToInt( long v ) => (int)v;

	[SuiConverter( "IntToString", Category = "Conversion", Description = "int → string" )]
	public static string IntToString( int v ) => v.ToString();

	[SuiConverter( "FloatToString", Category = "Conversion", Description = "float → string with a fixed decimal count" )]
	public static string FloatToString( float v, int decimals = 2 )
		=> v.ToString( "F" + (decimals < 0 ? 0 : decimals) );

	[SuiConverter( "Parse", Category = "Conversion", Description = "string → float (0 if unparseable)" )]
	public static float Parse( string v )
		=> float.TryParse( v, out var f ) ? f : 0f;

	// ─────────────────────────── Comparison / Logic ───────────────────────

	[SuiConverter( "Equal", Category = "Logic", Description = "a == b" )]
	public static bool Equal<T>( T a, T b ) => EqualityComparer<T>.Default.Equals( a, b );

	[SuiConverter( "NotEqual", Category = "Logic", Description = "a != b" )]
	public static bool NotEqual<T>( T a, T b ) => !EqualityComparer<T>.Default.Equals( a, b );

	[SuiConverter( "GreaterThan", Category = "Logic", Description = "a > b" )]
	public static bool GreaterThan( float a, float b ) => a > b;

	[SuiConverter( "LessThan", Category = "Logic", Description = "a < b" )]
	public static bool LessThan( float a, float b ) => a < b;

	[SuiConverter( "If", Category = "Logic", Description = "cond ? ifTrue : ifFalse" )]
	public static T If<T>( bool cond, T ifTrue, T ifFalse ) => cond ? ifTrue : ifFalse;

	[SuiConverter( "And", Category = "Logic", Description = "a && b" )]
	public static bool And( bool a, bool b ) => a && b;

	[SuiConverter( "Or", Category = "Logic", Description = "a || b" )]
	public static bool Or( bool a, bool b ) => a || b;

	[SuiConverter( "Not", Category = "Logic", Description = "!v" )]
	public static bool Not( bool v ) => !v;

	// ──────────────────────────────── String ──────────────────────────────

	[SuiConverter( "Concat", Category = "String", Description = "a + b" )]
	public static string Concat( string a, string b ) => (a ?? "") + (b ?? "");

	[SuiConverter( "Format", Category = "String", Description = "string.Format(template, args)" )]
	public static string Format( string template, params object[] args )
		=> string.IsNullOrEmpty( template ) ? "" : string.Format( template, args ?? Array.Empty<object>() );

	[SuiConverter( "Uppercase", Category = "String", Description = "Convert v to UPPERCASE" )]
	public static string Uppercase( string v ) => v?.ToUpperInvariant() ?? "";

	[SuiConverter( "Lowercase", Category = "String", Description = "Convert v to lowercase" )]
	public static string Lowercase( string v ) => v?.ToLowerInvariant() ?? "";

	[SuiConverter( "TitleCase", Category = "String", Description = "Capitalise The First Letter Of Each Word" )]
	public static string TitleCase( string v )
	{
		if ( string.IsNullOrEmpty( v ) ) return "";
		var sb = new StringBuilder( v.Length );
		bool atWordStart = true;
		foreach ( var ch in v )
		{
			if ( char.IsWhiteSpace( ch ) ) { atWordStart = true; sb.Append( ch ); continue; }
			sb.Append( atWordStart ? char.ToUpperInvariant( ch ) : char.ToLowerInvariant( ch ) );
			atWordStart = false;
		}
		return sb.ToString();
	}

	[SuiConverter( "Length", Category = "String", Description = "Character count of v" )]
	public static int Length( string v ) => v?.Length ?? 0;

	[SuiConverter( "Substring", Category = "String", Description = "Slice of v from start, length chars" )]
	public static string Substring( string v, int start, int length )
	{
		if ( string.IsNullOrEmpty( v ) || start < 0 || start >= v.Length || length <= 0 ) return "";
		if ( start + length > v.Length ) length = v.Length - start;
		return v.Substring( start, length );
	}

	[SuiConverter( "Replace", Category = "String", Description = "Replace every oldStr in v with newStr" )]
	public static string Replace( string v, string oldStr, string newStr )
		=> string.IsNullOrEmpty( v ) || string.IsNullOrEmpty( oldStr ) ? (v ?? "") : v.Replace( oldStr, newStr ?? "" );

	// ──────────────────────────────── Color ───────────────────────────────

	[SuiConverter( "MakeColor", Category = "Color", Description = "Build a Color from r, g, b, a (0..1)" )]
	public static Color MakeColor( float r, float g, float b, float a ) => new Color( r, g, b, a );

	[SuiConverter( "ColorFromHex", Category = "Color", Description = "Parse a hex string into a Color" )]
	public static Color ColorFromHex( string hex )
		=> string.IsNullOrEmpty( hex ) ? Color.White : Color.Parse( hex ) ?? Color.White;

	[SuiConverter( "ColorLerp", Category = "Color", Description = "Linear interpolation between two colors" )]
	public static Color ColorLerp( Color a, Color b, float t ) => Color.Lerp( a, b, t );

	[SuiConverter( "WithAlpha", Category = "Color", Description = "Color c with its alpha replaced" )]
	public static Color WithAlpha( Color c, float a ) => c.WithAlpha( a );

	[SuiConverter( "Darken", Category = "Color", Description = "Darken c by amount (0..1)" )]
	public static Color Darken( Color c, float amount )
	{
		var k = 1f - (amount < 0f ? 0f : amount > 1f ? 1f : amount);
		return new Color( c.r * k, c.g * k, c.b * k, c.a );
	}

	[SuiConverter( "Lighten", Category = "Color", Description = "Lighten c toward white by amount (0..1)" )]
	public static Color Lighten( Color c, float amount )
	{
		var k = amount < 0f ? 0f : amount > 1f ? 1f : amount;
		return new Color( c.r + (1f - c.r) * k, c.g + (1f - c.g) * k, c.b + (1f - c.b) * k, c.a );
	}

	// ────────────────────────────── Collection ────────────────────────────

	[SuiConverter( "Count", Category = "Collection", Description = "Number of items in list" )]
	public static int Count<T>( List<T> list ) => list?.Count ?? 0;

	[SuiConverter( "IsEmpty", Category = "Collection", Description = "True if list is null or empty" )]
	public static bool IsEmpty<T>( List<T> list ) => list == null || list.Count == 0;

	[SuiConverter( "First", Category = "Collection", Description = "First item, or fallback if empty" )]
	public static T First<T>( List<T> list, T fallback )
		=> list != null && list.Count > 0 ? list[0] : fallback;

	[SuiConverter( "Last", Category = "Collection", Description = "Last item, or fallback if empty" )]
	public static T Last<T>( List<T> list, T fallback )
		=> list != null && list.Count > 0 ? list[list.Count - 1] : fallback;

	[SuiConverter( "ElementAt", Category = "Collection", Description = "Item at index, or fallback if out of range" )]
	public static T ElementAt<T>( List<T> list, int index, T fallback )
		=> list != null && index >= 0 && index < list.Count ? list[index] : fallback;

	[SuiConverter( "Slice", Category = "Collection", Description = "Sub-list of count items starting at start" )]
	public static List<T> Slice<T>( List<T> list, int start, int count )
	{
		var result = new List<T>();
		if ( list == null || start < 0 || count <= 0 ) return result;
		var end = start + count;
		if ( end > list.Count ) end = list.Count;
		for ( int i = start; i < end; i++ ) result.Add( list[i] );
		return result;
	}
}
