using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Build the Razor <c>@()</c> expression for a <see cref="SuiBinding"/>. The
/// expression evaluates to the value that flows into the target property at
/// runtime — for a direct bind it's just the source Variable's name, for a
/// chain it's the nested-call sequence
/// <c>Last( Mid( First( SourceVar, arg ), arg ), arg )</c>.
/// </summary>
public static class SuiBindingExpressionEmitter
{
	/// <summary>
	/// Render the binding's evaluation expression as a C# source string —
	/// dropped between <c>@(</c> and <c>)</c> inside a Razor attribute.
	/// Falls back to <c>default</c> on any unresolved reference rather than
	/// emitting invalid code.
	/// </summary>
	public static string Emit( SuiBinding b, SuiDocument doc )
	{
		if ( b?.Source == null || string.IsNullOrEmpty( b.Source.VariableId ) ) return "default";

		var source = FindVariable( doc, b.Source.VariableId );
		if ( source == null ) return "default /* unknown source variable */";

		var current = source.Name;
		if ( b.Converters == null || b.Converters.Count == 0 ) return current;

		foreach ( var step in b.Converters )
		{
			if ( step == null ) continue;
			var meta = SuiConverterCatalog.Find( step.ConverterRef );
			var fqn = ResolveConverterFqn( step.ConverterRef );
			if ( string.IsNullOrEmpty( fqn ) || meta == null )
			{
				return $"default /* unknown converter {step.ConverterRef} */";
			}

			// Variadic detection: when the last meta input is `IsParams`, the
			// element type for any additional user-added args is the array's
			// element type (e.g. `Object[]` → `Object`). We iterate up to the
			// user's actual arg count so all appended variadic values reach
			// the call site — C# auto-wraps the tail into the params array.
			var metaCount = meta.Inputs?.Length ?? 0;
			var lastIdx = metaCount - 1;
			var isVariadic = lastIdx >= 0 && meta.Inputs[lastIdx].IsParams;
			var variadicElemType = isVariadic
				? StripArraySuffix( meta.Inputs[lastIdx].Type )
				: null;
			var emitCount = isVariadic
				? System.Math.Max( metaCount, step.Args?.Count ?? 0 )
				: metaCount;

			var args = new List<string>( emitCount );
			for ( int i = 0; i < emitCount; i++ )
			{
				var arg = step.Args != null && i < step.Args.Count ? step.Args[i] : null;
				// Variadic slot uses the array's element type; fixed slots use
				// the corresponding meta parameter type.
				var paramType = isVariadic && i >= lastIdx
					? variadicElemType
					: (i < metaCount ? meta.Inputs[i]?.Type : null);
				args.Add( RenderArg( arg, current, doc, paramType ) );
			}
			current = $"{fqn}({string.Join( ", ", args )})";
		}
		return current;
	}

	private static string RenderArg( SuiConverterArg arg, string chainInput, SuiDocument doc, string paramType )
	{
		if ( arg == null ) return SuiTypeMapper.ZeroLiteral( paramType );

		switch ( arg.Kind )
		{
			case SuiConverterArgKind.ChainRef:
				// "Source" + "Previous" both resolve to whatever value is currently
				// flowing into this step (we don't reconstruct earlier steps).
				return string.IsNullOrEmpty( chainInput ) ? "default" : chainInput;

			case SuiConverterArgKind.Variable:
				var v = FindVariable( doc, arg.VariableId );
				return v?.Name ?? SuiTypeMapper.ZeroLiteral( paramType );

			case SuiConverterArgKind.Literal:
			default:
				return arg.Literal == null
					? SuiTypeMapper.ZeroLiteral( paramType )
					: SuiTypeMapper.DefaultLiteral( paramType, arg.Literal );
		}
	}

	private static SuiVariable FindVariable( SuiDocument doc, string id )
	{
		if ( doc?.Variables == null || string.IsNullOrEmpty( id ) ) return null;
		foreach ( var v in doc.Variables )
			if ( v?.Id == id ) return v;
		return null;
	}

	/// <summary>Strip trailing <c>[]</c> — used to resolve the element type of a variadic params slot.</summary>
	private static string StripArraySuffix( string t )
	{
		if ( string.IsNullOrEmpty( t ) ) return t;
		if ( t.EndsWith( "[]" ) ) return t.Substring( 0, t.Length - 2 );
		return t;
	}

	/// <summary>
	/// Resolve a <c>ConverterRef</c> to a fully-qualified C# call target.
	/// <list type="bullet">
	///   <item><c>builtin.Name</c>       → <c>global::SboxUiDesigner.Runtime.SuiBuiltinConverters.Name</c></item>
	///   <item><c>user.Foo.Bar.Method</c> → <c>global::Foo.Bar.Method</c></item>
	/// </list>
	/// Legacy <c>graph.*</c> refs (ActionGraph integration) were ripped at M4
	/// close per DEVIATION D-017 — Doo is the visual scripting backend.
	/// </summary>
	public static string ResolveConverterFqn( string converterRef )
	{
		if ( string.IsNullOrEmpty( converterRef ) ) return null;
		if ( converterRef.StartsWith( "builtin." ) )
			return "global::SboxUiDesigner.Runtime.SuiBuiltinConverters." + converterRef.Substring( "builtin.".Length );
		if ( converterRef.StartsWith( "user." ) )
			return "global::" + converterRef.Substring( "user.".Length );
		return null; // graph.* deferred to M3
	}
}
