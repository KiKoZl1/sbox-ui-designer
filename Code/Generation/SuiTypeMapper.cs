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
	/// </summary>
	public static string DefaultLiteral( string typeRef, JsonNode value )
	{
		if ( value == null ) return ZeroLiteral( typeRef );

		try
		{
			return typeRef switch
			{
				"int"       => ToLong( value ).ToString( CultureInfo.InvariantCulture ),
				"long"      => $"{ToLong( value ).ToString( CultureInfo.InvariantCulture )}L",
				"float"     => $"{ToDouble( value ).ToString( "G17", CultureInfo.InvariantCulture )}f",
				"double"    => $"{ToDouble( value ).ToString( "G17", CultureInfo.InvariantCulture )}d",
				"bool"      => ToBool( value ) ? "true" : "false",
				"string"    => EscapeStringLiteral( value.GetValue<string>() ),
				"Color"     => $"global::Color.Parse({EscapeStringLiteral( value.GetValue<string>() )}) ?? global::Color.White",
				_           => ZeroLiteral( typeRef ),
			};
		}
		catch
		{
			return ZeroLiteral( typeRef );
		}
	}

	/// <summary>Conservative zero / default literal per TypeRef.</summary>
	public static string ZeroLiteral( string typeRef ) => typeRef switch
	{
		"int" or "long"       => "0",
		"float"               => "0f",
		"double"              => "0d",
		"bool"                => "false",
		"string"              => "\"\"",
		_                     => "default",
	};

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
