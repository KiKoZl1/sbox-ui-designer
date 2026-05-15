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

			var argCount = meta.Inputs?.Length ?? 0;
			var args = new List<string>( argCount );
			for ( int i = 0; i < argCount; i++ )
			{
				var arg = step.Args != null && i < step.Args.Count ? step.Args[i] : null;
				args.Add( RenderArg( arg, current, doc, meta.Inputs[i]?.Type ) );
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

	/// <summary>
	/// Resolve a <c>ConverterRef</c> to a fully-qualified C# call target.
	/// <list type="bullet">
	///   <item><c>builtin.Name</c>       → <c>global::SboxUiDesigner.Runtime.SuiBuiltinConverters.Name</c></item>
	///   <item><c>user.Foo.Bar.Method</c> → <c>global::Foo.Bar.Method</c></item>
	///   <item><c>graph.path/to.action</c> → null (ActionGraph integration is M3)</item>
	/// </list>
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
