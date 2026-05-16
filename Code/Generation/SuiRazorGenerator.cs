using System.Collections.Generic;
using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Razor markup emitter. Produces a single .razor file per .sui document
/// containing <c>@inherits PanelComponent</c> + the element tree wrapped in
/// <c>&lt;root&gt;</c>.
///
/// MVP invariants (per PRD doc 10 + ui-anti-patterns.md):
///  - Zero <c>@expression</c> anywhere in the markup body. Every value baked
///    in at generation time as a literal. Text content is HTML-escaped via
///    <see cref="SuiNameSanitizer.EscapeRazorText"/> so user-typed text can't
///    inject Razor directives.
///  - No <c>@code</c> block. No <c>BuildHash()</c> override. The default
///    runtime rebuild on hotload + interaction is sufficient because nothing
///    in the markup depends on a runtime variable.
///  - Element tags chosen to match the canonical s&box runtime panel tree:
///    <c>&lt;label&gt;</c> for Text, <c>&lt;div&gt;</c> for everything else.
///
/// V1.5 will lift the no-@expression rule when data binding lands and add a
/// matching <c>BuildHash()</c> override that includes every referenced
/// property — see PRD doc 10 / 12.
/// </summary>
public sealed class SuiRazorGenerator
{
	private readonly StringBuilder _sb = new();
	private SuiDocument _doc;
	private SuiGenerationContext _ctx;
	private Dictionary<string, SuiElement> _byId;

	public string Generate( SuiGenerationContext ctx, SuiGenerationResult result )
	{
		_sb.Clear();
		_ctx = ctx;

		_doc = ctx?.Document;
		if ( _doc == null )
		{
			result.Errors.Add( "razor: document is null" );
			return "";
		}

		_byId = new();
		foreach ( var el in _doc.Elements )
			if ( !string.IsNullOrEmpty( el.Id ) ) _byId[el.Id] = el;

		// Header (Razor comment block, parseable by manifest checker).
		_sb.Append( SuiHeaderEmitter.EmitRazorHeader( _doc ) );
		_sb.Append( '\n' );

		// Standard preamble. @inherits PanelComponent makes this the root of
		// a runtime UI tree (used inside ScreenPanel or WorldPanel).
		// @namespace places the generated type under ctx.Namespace so the
		// preview/runtime can find it deterministically via TypeLibrary.
		if ( !string.IsNullOrEmpty( ctx.Namespace ) )
			_sb.AppendLine( $"@namespace {ctx.Namespace}" );
		_sb.AppendLine( "@using Sandbox;" );
		_sb.AppendLine( "@using Sandbox.UI;" );
		_sb.AppendLine( "@inherits PanelComponent" );
		_sb.AppendLine();

		// Markup tree.
		_sb.AppendLine( "<root>" );
		var root = _doc.GetRoot();
		if ( root != null )
		{
			EmitElement( root, depth: 1 );
		}
		_sb.AppendLine( "</root>" );

		// V1.5 — @code block with Variables as [Property] fields + BuildHash()
		// override. Emitted only when the document declares Variables or has
		// reactive bindings; V1 documents stay byte-identical.
		EmitCodeBlock();

		return _sb.ToString();
	}

	private void EmitCodeBlock()
	{
		var hasVars = _doc.Variables != null && _doc.Variables.Count > 0;
		var childRefs = CollectChildReferences();
		var hasChildren = childRefs.Count > 0;
		if ( !hasVars && !hasChildren ) return;

		var body = new System.Text.StringBuilder();
		SuiVariableEmitter.EmitProperties( _doc.Variables, body );

		// V1.5-M2-K4 — named-instance fields for every SuiReference. Single
		// instance → [Property] FieldType FieldName = new(); ForEach → List<>.
		// Parent code reads/writes parent.FieldName.VarName; the Razor markup
		// (EmitSuiReferenceTag) pulls Var values from the same field, so wrapper
		// and panel stay in sync by reference.
		foreach ( var entry in childRefs )
		{
			var fqType = string.IsNullOrEmpty( entry.Target.Namespace )
				? entry.Target.ClassName
				: $"{entry.Target.Namespace}.{entry.Target.ClassName}";
			var fieldName = SuiNameSanitizer.ToCSharpIdentifier( entry.Element.Name );
			if ( string.IsNullOrEmpty( fieldName ) ) continue;

			if ( entry.IsForEach )
			{
				body.Append( "\t[Property, Group( \"Children\" )] public System.Collections.Generic.List<" )
					.Append( fqType ).Append( "> " ).Append( fieldName )
					.AppendLine( " { get; set; } = new();" );
			}
			else
			{
				body.Append( "\t[Property, Group( \"Children\" )] public " )
					.Append( fqType ).Append( ' ' ).Append( fieldName )
					.Append( " { get; set; } = new " ).Append( fqType ).AppendLine( "();" );
			}
		}

		SuiBuildHashEmitter.EmitBuildHash( _doc.Variables, _doc.Elements, body );

		if ( body.Length == 0 ) return;

		_sb.AppendLine();
		_sb.AppendLine( "@code" );
		_sb.AppendLine( "{" );
		_sb.Append( body );
		_sb.AppendLine( "}" );
	}

	/// <summary>
	/// Resolved metadata for each SuiReference element in the host doc — used
	/// both by EmitCodeBlock (to emit named-instance fields) and consumed by
	/// EmitSuiReferenceTag (to know the field name to dereference in markup).
	/// </summary>
	private readonly struct ChildRefEntry
	{
		public readonly SuiElement Element;
		public readonly SuiReferenceTarget Target;
		public readonly bool IsForEach;
		public ChildRefEntry( SuiElement el, SuiReferenceTarget tgt, bool fe )
		{ Element = el; Target = tgt; IsForEach = fe; }
	}

	private List<ChildRefEntry> CollectChildReferences()
	{
		var list = new List<ChildRefEntry>();
		if ( _doc?.Elements == null ) return list;

		foreach ( var el in _doc.Elements )
		{
			if ( el?.Type != SuiElementType.SuiReference ) continue;
			var data = el.SuiReference;
			if ( data == null || string.IsNullOrEmpty( data.SourceGuid ) ) continue;

			var target = _ctx?.ResolveReferencedClass?.Invoke( data.SourceGuid );
			if ( target == null || string.IsNullOrEmpty( target.ClassName ) ) continue;

			var isForEach = data.ForEach != null && !string.IsNullOrEmpty( data.ForEach.SourceVariableId );
			list.Add( new ChildRefEntry( el, target, isForEach ) );
		}
		return list;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Per-element markup emission
	// ─────────────────────────────────────────────────────────────────────

	private void EmitElement( SuiElement el, int depth )
	{
		if ( el == null ) return;
		var indent = new string( ' ', depth * 2 );
		// Two classes per element:
		//  - User class (from Style.ClassName) — shared across siblings, exposed
		//    for hand-written rules in <name>.User.scss.
		//  - Unique element class (sui-<id>) — used by the SCSS generator so
		//    per-element generated rules NEVER collide with siblings sharing
		//    the same user class (which would otherwise cascade-overwrite, e.g.
		//    six slots all ending up with the last slot's background-image).
		var className = SuiNameSanitizer.ToCssClass( el.Style?.ClassName ?? el.Type.ToString() );
		var uniqueClass = ElementUniqueClass( el );
		var combinedClass = className == uniqueClass ? className : $"{className} {uniqueClass}";

		switch ( el.Type )
		{
			case SuiElementType.Text:
				EmitTextElement( el, combinedClass, indent );
				break;

			case SuiElementType.SuiReference:
				EmitSuiReferenceElement( el, indent );
				break;

			default:
				EmitContainerElement( el, combinedClass, indent, depth );
				break;
		}
	}

	/// <summary>
	/// Stable per-element CSS class — derived from the element Id, prefixed
	/// with <c>sui-</c> so it never collides with user-defined classes.
	/// </summary>
	internal static string ElementUniqueClass( SuiElement el )
	{
		var raw = el?.Id ?? "";
		var safe = SuiNameSanitizer.ToCssClass( raw );
		return string.IsNullOrEmpty( safe ) ? "sui-el" : $"sui-{safe}";
	}

	/// <summary>
	/// Emit a SuiReference as an embedded child component tag. Resolves the
	/// child's namespace+class+AcceptedProps via <see cref="SuiGenerationContext.ResolveReferencedClass"/>,
	/// renders each <c>Props</c> entry as <c>Name=@expression</c> or <c>Name="literal"</c>,
	/// and wraps the whole thing in <c>@foreach</c> when <c>ForEach</c> is set
	/// (PRD 19 § 7.1 + 7.2).
	/// </summary>
	private void EmitSuiReferenceElement( SuiElement el, string indent )
	{
		var data = el.SuiReference;
		if ( data == null || string.IsNullOrEmpty( data.SourceGuid ) )
		{
			_sb.Append( indent ).Append( "@* SuiReference '" ).Append( el.Name )
				.AppendLine( "' has no source set — skipped. *@" );
			return;
		}

		var target = _ctx?.ResolveReferencedClass?.Invoke( data.SourceGuid );
		if ( target == null || string.IsNullOrEmpty( target.ClassName ) )
		{
			_sb.Append( indent ).Append( "@* SuiReference '" ).Append( el.Name )
				.Append( "' could not resolve SourceGuid='" ).Append( data.SourceGuid )
				.AppendLine( "' — child class not found. *@" );
			return;
		}

		var childWrapperType = string.IsNullOrEmpty( target.Namespace )
			? target.ClassName
			: $"{target.Namespace}.{target.ClassName}";
		var childPanelType = childWrapperType + "Panel";
		var fieldName = SuiNameSanitizer.ToCSharpIdentifier( el.Name );

		if ( string.IsNullOrEmpty( fieldName ) )
		{
			_sb.Append( indent ).Append( "@* SuiReference has empty/invalid Name '" ).Append( el.Name )
				.AppendLine( "' — cannot derive C# field. *@" );
			return;
		}

		var fe = data.ForEach;
		var isForEach = fe != null && !string.IsNullOrEmpty( fe.SourceVariableId );

		// V1.5-M2-K4 — named-instance addressing. The @code block (see
		// EmitCodeBlock) declared a field of type ChildWrapper named after
		// this element; the markup pulls each Variable value from that field.
		// User code does `parent.FieldName.VarName = X` and the next render
		// picks it up because the field is shared by reference between the
		// wrapper (Instance mode) and the panel.
		if ( isForEach )
		{
			_sb.Append( indent ).Append( "@if ( " ).Append( fieldName ).AppendLine( " != null )" );
			_sb.Append( indent ).AppendLine( "{" );
			_sb.Append( indent ).Append( "  @foreach ( var __item in " ).Append( fieldName ).AppendLine( " )" );
			_sb.Append( indent ).AppendLine( "  {" );
			EmitNamedChildTag( childPanelType, target.PublicVariables, "__item", indent + "    " );
			_sb.Append( indent ).AppendLine( "  }" );
			_sb.Append( indent ).AppendLine( "}" );
		}
		else
		{
			EmitNamedChildTag( childPanelType, target.PublicVariables, fieldName, indent );
		}
	}

	private void EmitNamedChildTag(
		string childPanelType,
		System.Collections.Generic.IList<SuiVariable> targetVars,
		string accessorExpr,
		string indent )
	{
		_sb.Append( indent ).Append( "<" ).Append( childPanelType );

		if ( targetVars != null )
		{
			foreach ( var v in targetVars )
			{
				if ( v == null || string.IsNullOrEmpty( v.Name ) ) continue;
				var def = SuiTypeMapper.DefaultLiteral( v.Type, v.Default );
				_sb.Append( " " ).Append( v.Name )
					.Append( "=@(" ).Append( accessorExpr ).Append( '?' ).Append( '.' ).Append( v.Name )
					.Append( " ?? " ).Append( def ).Append( ")" );
			}
		}

		_sb.AppendLine( " />" );
	}

	private string RenderPropAttribute( SuiVariable prop, System.Text.Json.Nodes.JsonNode node )
	{
		// Binding object marker `{ "$bind": { ... } }` is recognised by presence
		// of a "$bind" key — the literal otherwise renders straight as a Razor
		// literal/string. The binding source-Variable name is rendered as a
		// Razor @(VarName) expression; the host's [Property] is in scope.
		if ( node is System.Text.Json.Nodes.JsonObject obj && obj.ContainsKey( "$bind" ) )
		{
			var bind = obj["$bind"];
			var varId = bind?["VariableId"]?.GetValue<string>();
			var varName = ResolveVariableNameById( varId );
			return string.IsNullOrEmpty( varName )
				? "@(default(" + SuiTypeMapper.ToCSharp( prop.Type ) + "))"
				: "@(" + varName + ")";
		}

		switch ( prop.Type )
		{
			case "int":
			case "long":
				return "@(" + node.ToJsonString() + ")";
			case "float":
				return "@(" + node.ToJsonString() + "f)";
			case "double":
				return "@(" + node.ToJsonString() + ")";
			case "bool":
				return "@(" + node.ToJsonString().ToLowerInvariant() + ")";
			case "string":
				return "\"" + EscapeForAttr( node.GetValue<string>() ?? "" ) + "\"";
			case "Color":
				// Hex string literal → emit as `new Color( "#xxxxxx" )` via runtime parse
				return "@(global::Sandbox.Color.Parse(\"" + EscapeForAttr( node.GetValue<string>() ?? "#ffffff" ) + "\"))";
			default:
				// Engine types / resource refs — best-effort emit as a string literal
				return node is System.Text.Json.Nodes.JsonValue v && v.TryGetValue<string>( out var s )
					? "\"" + EscapeForAttr( s ) + "\""
					: "@(" + node.ToJsonString() + ")";
		}
	}

	private static string EscapeForAttr( string s )
		=> (s ?? "").Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );

	/// <summary>Resolve a Variable Id to its Name within the current host document.</summary>
	private string ResolveVariableNameById( string varId )
	{
		if ( string.IsNullOrEmpty( varId ) || _doc?.Variables == null ) return null;
		foreach ( var v in _doc.Variables )
		{
			if ( v?.Id == varId ) return v.Name;
		}
		return null;
	}

	private void EmitTextElement( SuiElement el, string className, string indent )
	{
		// V1.5 — if the Text element's Text property is bound, the body becomes
		// a Razor @() expression; otherwise it stays the literal authored value.
		var bodyExpr = SuiBindingEmitter.TryGetTextBodyExpression( el, _doc );
		var body = bodyExpr ?? SuiNameSanitizer.EscapeRazorText( el.Props?.Text ?? "" );

		var bindAttrs = SuiBindingEmitter.EmitElementAttributes( el, _doc );
		_sb.Append( indent ).Append( "<label class=\"" ).Append( className ).Append( "\"" )
			.Append( bindAttrs ).Append( ">" )
			.Append( body )
			.AppendLine( "</label>" );
	}

	private void EmitContainerElement( SuiElement el, string className, string indent, int depth )
	{
		var hasChildren = el.Children != null && el.Children.Count > 0;

		// Open tag — class attribute first, then V1.5 data-sui-* binding attrs
		// + a unified inline style="" body (extended with positioning rules for
		// the ProgressBar fill-child special case).
		var dataAttrs = SuiBindingEmitter.EmitElementDataAttrs( el, _doc );
		var styleBody = SuiBindingEmitter.EmitElementStyleBody( el, _doc );
		var needsProgressFill = SuiBindingEmitter.HasProgressFillBindings( el );
		if ( needsProgressFill )
		{
			styleBody = "position: relative; overflow: hidden;"
				+ ( styleBody.Length > 0 ? " " + styleBody : "" );
		}

		_sb.Append( indent ).Append( "<div class=\"" ).Append( className ).Append( "\"" )
			.Append( dataAttrs );
		if ( styleBody.Length > 0 )
			_sb.Append( " style=\"" ).Append( styleBody ).Append( "\"" );

		// Self-closing if no children and no intrinsic content (e.g. Button label).
		if ( !hasChildren && !HasIntrinsicContent( el ) )
		{
			_sb.AppendLine( "></div>" );
			return;
		}

		_sb.AppendLine( ">" );

		// Type-specific intrinsic content (Button label, ProgressBar fill+label, etc.)
		EmitIntrinsicContent( el, depth + 1 );

		// Children
		foreach ( var childId in el.Children )
		{
			if ( _byId.TryGetValue( childId, out var child ) )
				EmitElement( child, depth + 1 );
		}

		// Close tag
		_sb.Append( indent ).AppendLine( "</div>" );
	}

	private bool HasIntrinsicContent( SuiElement el ) => el.Type switch
	{
		SuiElementType.Button when !string.IsNullOrEmpty( el.Props?.ButtonText )
			|| SuiBindingEmitter.TryGetButtonBodyExpression( el, _doc ) != null => true,
		SuiElementType.ProgressBar when SuiBindingEmitter.HasProgressFillBindings( el ) => true,
		_ => false,
	};

	private void EmitIntrinsicContent( SuiElement el, int depth )
	{
		var indent = new string( ' ', depth * 2 );

		switch ( el.Type )
		{
			case SuiElementType.Button:
			{
				// If ButtonText is bound, the label body becomes a Razor
				// expression — the button updates live as gameplay code
				// reassigns the bound Variable. Otherwise the literal Props
				// value stays baked in at compile time.
				var boundBody = SuiBindingEmitter.TryGetButtonBodyExpression( el, _doc );
				if ( boundBody != null )
				{
					_sb.Append( indent ).Append( "<label class=\"label\">" )
						.Append( boundBody )
						.AppendLine( "</label>" );
				}
				else if ( !string.IsNullOrEmpty( el.Props?.ButtonText ) )
				{
					var text = SuiNameSanitizer.EscapeRazorText( el.Props.ButtonText );
					_sb.Append( indent ).Append( "<label class=\"label\">" )
						.Append( text )
						.AppendLine( "</label>" );
				}
				break;
			}

			case SuiElementType.ProgressBar:
				if ( SuiBindingEmitter.HasProgressFillBindings( el ) )
					EmitProgressFill( el, indent );
				break;
		}
	}

	/// <summary>
	/// Emit the inner <c>.sui-progress-fill</c> child whose width is the
	/// clamped (value − min) / (max − min) of the ProgressBar's bound or
	/// literal Value / Min / Max. Drives the visual fill at runtime so the
	/// user gets a responsive bar from designer-binding alone.
	/// </summary>
	private void EmitProgressFill( SuiElement el, string indent )
	{
		var valueExpr     = ResolveBoundOrLiteralFloat( el, "Value", el.Props?.ProgressPreviewValue ?? 50f );
		var minExpr       = ResolveBoundOrLiteralFloat( el, "Min",   el.Props?.ProgressMin          ?? 0f );
		var maxExpr       = ResolveBoundOrLiteralFloat( el, "Max",   el.Props?.ProgressMax          ?? 100f );
		var fillColorExpr = ResolveBoundOrLiteralColor( el, "FillColor", el.Props?.ProgressFillColor ?? "#4ade80" );

		// Width % = Clamp01( (value - min) / (max - min) ) * 100
		var pct =
			"global::SboxUiDesigner.Runtime.SuiBuiltinConverters.Clamp01( "
			+ "global::SboxUiDesigner.Runtime.SuiBuiltinConverters.Divide( "
			+ "(float)(" + valueExpr + ") - (float)(" + minExpr + "), "
			+ "(float)(" + maxExpr + ") - (float)(" + minExpr + ")"
			+ " ) ) * 100f";

		_sb.Append( indent )
			.Append( "<div class=\"sui-progress-fill\" style=\"position: absolute; left: 0; top: 0; bottom: 0; width: @(" )
			.Append( pct )
			.Append( ")%; background-color: @(" )
			.Append( fillColorExpr )
			.AppendLine( "); pointer-events: none;\"></div>" );
	}

	/// <summary>Bound expression for <paramref name="property"/> if present, else the C# literal for the given float default.</summary>
	private string ResolveBoundOrLiteralFloat( SuiElement el, string property, float literalDefault )
	{
		if ( el.Bindings != null )
		{
			foreach ( var b in el.Bindings )
			{
				if ( b == null || b.Property != property ) continue;
				return SuiBindingExpressionEmitter.Emit( b, _doc );
			}
		}
		return literalDefault.ToString( "G9", System.Globalization.CultureInfo.InvariantCulture ) + "f";
	}

	/// <summary>Bound expression's <c>.Hex</c> if present, else the literal hex string from Props.</summary>
	private string ResolveBoundOrLiteralColor( SuiElement el, string property, string literalHex )
	{
		if ( el.Bindings != null )
		{
			foreach ( var b in el.Bindings )
			{
				if ( b == null || b.Property != property ) continue;
				return "(" + SuiBindingExpressionEmitter.Emit( b, _doc ) + ").Hex";
			}
		}
		return "\"" + ( literalHex ?? "#4ade80" ).Replace( "\"", "\\\"" ) + "\"";
	}
}
