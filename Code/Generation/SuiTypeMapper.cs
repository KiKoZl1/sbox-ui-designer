using System.Globalization;
using System.Text.Json.Nodes;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Maps SUI TypeRef strings to C# source — type names for <c>[Property]</c>
/// declarations + literal renderings for default values. Used by every emitter
/// that writes typed C# from a <see cref="SuiVariable"/> (PRD 18 § 3.4).
///
/// All type names are emitted with <c>global::</c> qualifiers so the generated
/// code is robust against namespace shadowing in the consuming project.
/// </summary>
public static class SuiTypeMapper
{
	/// <summary>SUI TypeRef → fully-qualified C# type name suitable for a field/property declaration.</summary>
	public static string ToCSharp( string typeRef )
	{
		if ( string.IsNullOrEmpty( typeRef ) ) return "object";

		if ( typeRef.StartsWith( "List<" ) && typeRef.EndsWith( ">" ) )
		{
			var inner = typeRef.Substring( "List<".Length, typeRef.Length - "List<".Length - 1 );
			return $"global::System.Collections.Generic.List<{ToCSharp( inner )}>";
		}
		if ( typeRef.StartsWith( "Enum:" ) )
			return "global::" + typeRef.Substring( "Enum:".Length );
		if ( typeRef.StartsWith( "Component:" ) )
			return "global::" + typeRef.Substring( "Component:".Length );

		return typeRef switch
		{
			"int"       => "int",
			"long"      => "long",
			"float"     => "float",
			"double"    => "double",
			"bool"      => "bool",
			"string"    => "string",
			"Color"     => "global::Color",
			"Vector2"   => "global::Vector2",
			"Vector3"   => "global::Vector3",
			"Vector4"   => "global::Vector4",
			"Angles"    => "global::Angles",
			"Rotation"  => "global::Rotation",
			"Transform" => "global::Transform",
			"Texture"   => "global::Sandbox.Texture",
			"Resource"  => "global::Sandbox.GameResource",
			"Sound"     => "global::Sandbox.SoundEvent",
			"Material"  => "global::Sandbox.Material",
			// V1.5-M2-K7 — unknown TypeRef is taken as a C# type identifier in
			// scope at the call site (e.g. another SUI-generated wrapper class
			// like `Slot` that resolves via `@namespace Game.UI` on the parent
			// Razor file). Falling back to `object` silently broke `List<Slot>`
			// in TestForEach — the parent's `Items` ended up `List<object>` and
			// callers couldn't do `Items.Count`. Empty stays `object` because
			// there's literally no name to reference.
			_           => typeRef,
		};
	}

	/// <summary>
	/// Render a Variable default value (stored as a JSON node) as a C# literal
	/// expression. Tolerant — on any parse failure falls back to the type's
	/// natural zero so the generated code always compiles.
	///
	/// Accepts both SUI TypeRef names ("int", "float", "Color") and C# type
	/// names from the converter catalog ("Int32", "Single", "Color") because
	/// converter inputs use the latter (SuiConverterParam.Type).
	/// </summary>
	public static string DefaultLiteral( string typeRef, JsonNode value )
	{
		if ( value == null ) return ZeroLiteral( typeRef );
		var t = Normalize( typeRef );

		try
		{
			return t switch
			{
				"int"       => ToLong( value ).ToString( CultureInfo.InvariantCulture ),
				"long"      => $"{ToLong( value ).ToString( CultureInfo.InvariantCulture )}L",
				"float"     => $"{ToDouble( value ).ToString( "G17", CultureInfo.InvariantCulture )}f",
				"double"    => $"{ToDouble( value ).ToString( "G17", CultureInfo.InvariantCulture )}d",
				"bool"      => ToBool( value ) ? "true" : "false",
				"string"    => EscapeStringLiteral( SafeString( value ) ),
				"Color"     => RenderColorLiteral( value ),
				"Vector2"   => RenderVectorLiteral( value, 2 ),
				"Vector3"   => RenderVectorLiteral( value, 3 ),
				"Vector4"   => RenderVectorLiteral( value, 4 ),
				"Angles"    => RenderAnglesLiteral( value ),
				"Rotation"  => RenderRotationLiteral( value ),
				"Transform" => "global::Transform.Zero", // no useful literal form
				_           => ZeroLiteral( typeRef ),
			};
		}
		catch
		{
			return ZeroLiteral( typeRef );
		}
	}

	/// <summary>Conservative zero / default literal per TypeRef.</summary>
	public static string ZeroLiteral( string typeRef ) => Normalize( typeRef ) switch
	{
		"int" or "long"       => "0",
		"float"               => "0f",
		"double"              => "0d",
		"bool"                => "false",
		"string"              => "\"\"",
		"Color"               => "global::Color.White",
		"Vector2"             => "global::Vector2.Zero",
		"Vector3"             => "global::Vector3.Zero",
		"Vector4"             => "global::Vector4.Zero",
		"Angles"              => "global::Angles.Zero",
		"Rotation"            => "global::Rotation.Identity",
		"Transform"           => "global::Transform.Zero",
		_                     => "default",
	};

	/// <summary>
	/// Map a converter-catalog C# type name back to its SUI TypeRef form so the
	/// switch tables can stay short. <c>List&lt;T&gt;</c>, <c>Enum:</c>,
	/// <c>Component:</c>, and unknown user types pass through unchanged.
	/// </summary>
	private static string Normalize( string typeRef ) => typeRef switch
	{
		"Single"  => "float",
		"Double"  => "double",
		"Int32"   => "int",
		"Int64"   => "long",
		"Boolean" => "bool",
		"String"  => "string",
		_         => typeRef,
	};

	private static string SafeString( JsonNode n )
	{
		try { return n.GetValue<string>() ?? ""; }
		catch
		{
			// Fall back to the raw JSON repr (with quotes stripped).
			var s = n?.ToJsonString() ?? "";
			if ( s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"' )
				s = s.Substring( 1, s.Length - 2 );
			return s;
		}
	}

	/// <summary>
	/// The literal dialog stores Color as a hex/named string. Defer parsing to
	/// runtime via <c>Color.Parse</c> so the user can also type "red", etc.
	/// </summary>
	private static string RenderColorLiteral( JsonNode value )
	{
		var hex = SafeString( value );
		return $"(global::Color.Parse({EscapeStringLiteral( hex )}) ?? global::Color.White)";
	}

	/// <summary>
	/// The literal dialog stores vectors as comma-separated text — "1, 2, 3".
	/// Parse here to emit a strongly-typed <c>new Vector3(...)</c> at codegen.
	/// </summary>
	private static string RenderVectorLiteral( JsonNode value, int components )
	{
		var s = SafeString( value );
		var parts = s.Split( ',' );
		var nums = new double[components];
		for ( int i = 0; i < components; i++ )
		{
			var raw = i < parts.Length ? parts[i].Trim() : "0";
			if ( !double.TryParse( raw, NumberStyles.Any, CultureInfo.InvariantCulture, out nums[i] ) )
				nums[i] = 0;
		}
		var args = string.Join( ", ", FormatComponents( nums, "f" ) );
		return components switch
		{
			2 => $"new global::Vector2({args})",
			3 => $"new global::Vector3({args})",
			4 => $"new global::Vector4({args})",
			_ => "default",
		};
	}

	private static string RenderAnglesLiteral( JsonNode value )
	{
		var s = SafeString( value );
		var parts = s.Split( ',' );
		var nums = new double[3];
		for ( int i = 0; i < 3; i++ )
		{
			var raw = i < parts.Length ? parts[i].Trim() : "0";
			if ( !double.TryParse( raw, NumberStyles.Any, CultureInfo.InvariantCulture, out nums[i] ) )
				nums[i] = 0;
		}
		var args = string.Join( ", ", FormatComponents( nums, "f" ) );
		return $"new global::Angles({args})";
	}

	private static string RenderRotationLiteral( JsonNode value )
	{
		// Authored as pitch,yaw,roll — convert via Angles for the most intuitive
		// editor experience.
		var inner = RenderAnglesLiteral( value );
		return $"global::Rotation.From({inner})";
	}

	private static string[] FormatComponents( double[] nums, string suffix )
	{
		var arr = new string[nums.Length];
		for ( int i = 0; i < nums.Length; i++ )
			arr[i] = nums[i].ToString( "G17", CultureInfo.InvariantCulture ) + suffix;
		return arr;
	}

	private static long ToLong( JsonNode n )
	{
		try { return n.GetValue<long>(); }
		catch
		{
			if ( long.TryParse( n.ToJsonString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var l ) ) return l;
			if ( double.TryParse( n.ToJsonString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d ) ) return (long)d;
			return 0;
		}
	}

	private static double ToDouble( JsonNode n )
	{
		try { return n.GetValue<double>(); }
		catch
		{
			if ( double.TryParse( n.ToJsonString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d ) ) return d;
			return 0d;
		}
	}

	private static bool ToBool( JsonNode n )
	{
		try { return n.GetValue<bool>(); }
		catch
		{
			var raw = n.ToJsonString();
			return string.Equals( raw, "true", System.StringComparison.OrdinalIgnoreCase );
		}
	}

	private static string EscapeStringLiteral( string s )
		=> "\"" + (s ?? "").Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" ) + "\"";
}
