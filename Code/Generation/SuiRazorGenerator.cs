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
	private Dictionary<string, SuiElement> _byId;

	public string Generate( SuiGenerationContext ctx, SuiGenerationResult result )
	{
		_sb.Clear();

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
		if ( _doc.Variables == null || _doc.Variables.Count == 0 ) return;

		var body = new System.Text.StringBuilder();
		SuiVariableEmitter.EmitProperties( _doc.Variables, body );
		SuiBuildHashEmitter.EmitBuildHash( _doc.Variables, _doc.Elements, body );

		if ( body.Length == 0 ) return;

		_sb.AppendLine();
		_sb.AppendLine( "@code" );
		_sb.AppendLine( "{" );
		_sb.Append( body );
		_sb.AppendLine( "}" );
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

		// Open tag — class attribute first, then any V1.5 data-sui-* binding attrs.
		var bindAttrs = SuiBindingEmitter.EmitElementAttributes( el, _doc );
		_sb.Append( indent ).Append( "<div class=\"" ).Append( className ).Append( "\"" )
			.Append( bindAttrs );

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
		SuiElementType.Button when !string.IsNullOrEmpty( el.Props?.ButtonText ) => true,
		_ => false,
	};

	private void EmitIntrinsicContent( SuiElement el, int depth )
	{
		var indent = new string( ' ', depth * 2 );

		switch ( el.Type )
		{
			case SuiElementType.Button:
				if ( !string.IsNullOrEmpty( el.Props?.ButtonText ) )
				{
					var text = SuiNameSanitizer.EscapeRazorText( el.Props.ButtonText );
					_sb.Append( indent ).Append( "<label class=\"label\">" )
						.Append( text )
						.AppendLine( "</label>" );
				}
				break;
		}
	}
}
