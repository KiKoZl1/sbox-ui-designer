using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Heuristic that suggests a converter to bridge a source type to a target type
/// in the bind popup (PRD 18 § 4.9). M1-C ships single-step suggestions — pick
/// the first built-in whose first input accepts the source type and whose return
/// type matches the target. Multi-step chain search is a later refinement.
/// </summary>
public static class SuiConverterSuggester
{
	/// <summary>
	/// ConverterRef of a single built-in that maps <paramref name="sourceType"/> →
	/// <paramref name="targetType"/>, or null if none is needed / found.
	/// </summary>
	public static string Suggest( string sourceType, string targetType )
	{
		if ( string.IsNullOrEmpty( sourceType ) || string.IsNullOrEmpty( targetType ) ) return null;
		if ( TypesCompatible( sourceType, targetType ) ) return null; // direct bind — no converter needed

		foreach ( var c in SuiConverterCatalog.GetBuiltins() )
		{
			if ( c.Inputs == null || c.Inputs.Length == 0 ) continue;
			if ( !TypesCompatible( sourceType, c.Inputs[0].Type ) ) continue;
			if ( TypesCompatible( c.ReturnType, targetType ) ) return c.Ref;
		}
		return null;
	}

	/// <summary>
	/// Loose type compatibility for suggestion + validation purposes: exact match
	/// (after normalising TypeRef ↔ C# names), the generic placeholder <c>T</c>,
	/// or both being numeric.
	/// </summary>
	public static bool TypesCompatible( string a, string b )
	{
		if ( string.IsNullOrEmpty( a ) || string.IsNullOrEmpty( b ) ) return false;
		var na = Norm( a );
		var nb = Norm( b );
		if ( na == nb ) return true;
		if ( na == "T" || nb == "T" ) return true;        // generic converter slot
		return IsNumeric( na ) && IsNumeric( nb );        // numeric family widens freely
	}

	/// <summary>Normalise a Variable TypeRef ("int") and a C# type name ("Int32") to one vocabulary.</summary>
	private static string Norm( string t ) => t switch
	{
		"int" => "Int32",
		"long" => "Int64",
		"float" => "Single",
		"double" => "Double",
		"bool" => "Boolean",
		"string" => "String",
		_ => t,
	};

	private static bool IsNumeric( string t )
		=> t is "Int32" or "Int64" or "Single" or "Double";
}
