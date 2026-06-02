using System.Collections.Generic;
using System.Text;
using Sandbox;
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

		// V1.5-M2-K7 — @inherits Panel (NOT PanelComponent) so the generated
		// class can be nested inside other .sui markup via <Game.UI.<Name> />.
		// Standalone mounting goes through SuiPanel<TView>.Add() which spawns
		// a SuiHostPanelComponent + ScreenPanel and attaches the Panel as a
		// child. See DEVIATIONS D-014.
		if ( !string.IsNullOrEmpty( ctx.Namespace ) )
			_sb.AppendLine( $"@namespace {ctx.Namespace}" );
		_sb.AppendLine( "@using System;" );
		_sb.AppendLine( "@using Sandbox;" );
		_sb.AppendLine( "@using Sandbox.UI;" );
		_sb.AppendLine( "@inherits Panel" );
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
		var hasEvents = HasAnyEventsOrRefs( _doc );
		// V1.5 M4 — DropDown widgets need their static Options list emitted
		// even when no other field needs the @code block.
		var hasDropDown = HasAnyDropDown( _doc );
		var hasSlider = HasAnyOfType( _doc, SuiElementType.Slider );
		var hasInteractive = HasAnyInteractive( _doc );
		if ( !hasVars && !hasChildren && !hasEvents && !hasDropDown && !hasSlider && !hasInteractive ) return;

		var body = new System.Text.StringBuilder();
		SuiVariableEmitter.EmitProperties( _doc.Variables, body );

		// V1.5 M3 — renderer-side mirrors for Code-mode event handlers (the
		// wrapper assigns these in SyncFieldsTo) and typed fields captured
		// by `@ref` on exposed elements (Razor populates these at render time).
		SuiEventEmitter.EmitRendererFields( _doc, body );
		SuiElementRefEmitter.EmitRendererFields( _doc, body );

		// V1.5 M3.5 (PRD 25) — interactive elements ship a runtime-toggleable
		// `<elName>Disabled` bool. The markup wraps it into the class string
		// to add the `.disabled` CSS class, and SCSS `.disabled` carries
		// `pointer-events: none` so onclick is suppressed automatically.
		// Authoring-time default comes from SuiElementProps.IsDisabled.
		var disabledHashExprs = EmitInteractiveDisabledFields( body );

		// V1.5 M4 (PRD 21) — DropDown widgets ship a static List<Option>
		// field on the renderer so the @Options attribute resolves. Runtime
		// dynamic options (replace whole list from gameplay) is V1.6.
		EmitDropDownOptions( body );

		// V1.5-M2-K7-bugfix — also collect expressions that need to feed the
		// parent's BuildHash so that mutations on a child instance
		// (e.g. parent.MyHud.Health -= 10) trigger a parent re-render and
		// the embedded child tag picks up the new attribute values.
		var childHashExprs = new System.Collections.Generic.List<string>();

		// V1.5 M4 — Slider value field. SliderControl's `Value:bind` writes
		// into this field on every drag so our custom tooltip can compute
		// its horizontal position. Included in BuildHash so parent
		// re-renders on every value change.
		var sliderValueExprs = EmitSliderValueFields( body );
		foreach ( var expr in sliderValueExprs )
			childHashExprs.Add( expr );

		// V1.5-M2-K4 — named-instance fields for every SuiReference. Single
		// instance → [Property] FieldType FieldName = new(); ForEach → List<>.
		// Parent code reads/writes parent.FieldName.VarName; the Razor markup
		// (EmitSuiReferenceTag) pulls Var values from the same field, so wrapper
		// and panel stay in sync by reference.
		foreach ( var entry in childRefs )
		{
			var fieldName = SuiNameSanitizer.ToCSharpIdentifier( entry.Element.Name );
			if ( string.IsNullOrEmpty( fieldName ) ) continue;

			// Same global:: rationale as in EmitSuiReferenceElement — avoid
			// namespace ambiguity inside the parent's own namespace.
			var qualifiedType = string.IsNullOrEmpty( entry.Target.Namespace )
				? entry.Target.ClassName
				: $"global::{entry.Target.Namespace}.{entry.Target.ClassName}";

			// V1.5-M2-K7 — plain public, no [Property] (Panel base class).
			// The wrapper class (which IS a Component-friendly class) carries
			// the matching [Property] field so the inspector still shows
			// these for instance editing.
			if ( entry.IsForEach )
			{
				body.Append( "\tpublic System.Collections.Generic.List<" )
					.Append( qualifiedType ).Append( "> " ).Append( fieldName )
					.AppendLine( " { get; set; } = new();" );
				// ForEach hash: count is the cheap signal (re-render on add/remove).
				// Per-item ContentHash() also contributes via the body line below,
				// but Count goes in first as the cheapest invariant.
				childHashExprs.Add( fieldName + "?.Count ?? 0" );
			}
			else
			{
				body.Append( "\tpublic " )
					.Append( qualifiedType ).Append( ' ' ).Append( fieldName )
					.Append( " { get; set; } = new " ).Append( qualifiedType ).AppendLine( "();" );

				// V1.5-M2-K7-bugfix — use the wrapper's recursive ContentHash()
				// instead of listing each PublicVariable. This way mutations at
				// ANY depth (e.g. grand.parent.hud.Health) propagate up the
				// chain. Listing only direct PublicVariables broke 3+ level
				// composition because the middle wrapper might have zero
				// PublicVariables itself (its job is purely structural).
				childHashExprs.Add( fieldName + "?.ContentHash() ?? 0" );
			}
		}

		// Append interactive disabled fields to childHashExprs so a runtime
		// toggle invalidates BuildHash and re-renders the class string.
		foreach ( var expr in disabledHashExprs )
			childHashExprs.Add( expr );

		SuiBuildHashEmitter.EmitBuildHash( _doc.Variables, _doc.Elements, childHashExprs, body );

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

	/// <summary>
	/// True when any element in the doc has at least one Code-mode event slot
	/// or the <see cref="SuiElementFlags.ExposeAsVariable"/> flag set. Used to
	/// decide whether the @code block needs an emit pass even when the doc
	/// has no Variables and no SuiReference children.
	/// </summary>
	/// <summary>
	/// V1.5 M3.5 (PRD 25) — emit one <c>public bool &lt;Name&gt;Disabled</c>
	/// field per Button / InventorySlot / ItemIcon in the document. The
	/// authoring-time default lives in SuiElementProps.IsDisabled. Returns
	/// the list of hash expressions to push into BuildHash so a runtime
	/// flip re-renders the class string with/without the `.disabled` class.
	/// </summary>
	private System.Collections.Generic.List<string> EmitInteractiveDisabledFields( System.Text.StringBuilder body )
	{
		var hashes = new System.Collections.Generic.List<string>();
		if ( _doc?.Elements == null ) return hashes;

		foreach ( var el in _doc.Elements )
		{
			if ( el == null ) continue;
			if ( el.Type != SuiElementType.Button
				&& el.Type != SuiElementType.InventorySlot
				&& el.Type != SuiElementType.ItemIcon ) continue;

			var fieldName = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "Disabled" );
			if ( string.IsNullOrEmpty( fieldName ) ) continue;

			var defaultLit = (el.Props?.IsDisabled ?? false) ? "true" : "false";
			body.Append( "\tpublic bool " ).Append( fieldName )
				.Append( " { get; set; } = " ).Append( defaultLit ).AppendLine( ";" );
			hashes.Add( fieldName );

			// V1.5 M3.5 — emit a pure C# helper that returns the class string
			// for this element. The Razor markup invokes it via the simple
			// expression `class="@<Name>Class()"` — no mixed content, no
			// nested quote-escape gymnastics that would confuse the Razor
			// parser (which is what broke commit `1d2ef34`).
			var methodName = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "Class" );
			var staticClasses = BuildElementClassLiteral( el );
			body.Append( "\tprivate string " ).Append( methodName )
				.Append( "() => \"" ).Append( staticClasses )
				.Append( "\" + (" ).Append( fieldName ).AppendLine( " ? \" disabled\" : \"\");" );
		}
		return hashes;
	}

	/// <summary>
	/// Build the literal static-class string for an element — matches the
	/// non-interactive markup emit path (see <see cref="EmitElement"/>) so the
	/// runtime class string is identical whether the element ends up under
	/// <c>class="literal"</c> or under <c>class="@FooClass()"</c>.
	/// </summary>
	private string BuildElementClassLiteral( SuiElement el )
	{
		var userClass = SuiNameSanitizer.ToCssClass( el.Style?.ClassName ?? el.Type.ToString() );
		var uniqueClass = ElementUniqueClass( el );
		return userClass == uniqueClass ? userClass : userClass + " " + uniqueClass;
	}

	private static bool HasAnyDropDown( SuiDocument doc )
	{
		if ( doc?.Elements == null ) return false;
		foreach ( var el in doc.Elements )
		{
			if ( el != null && el.Type == SuiElementType.DropDown ) return true;
		}
		return false;
	}

	private static bool HasAnyOfType( SuiDocument doc, SuiElementType type )
	{
		if ( doc?.Elements == null ) return false;
		foreach ( var el in doc.Elements )
		{
			if ( el != null && el.Type == type ) return true;
		}
		return false;
	}

	private static bool HasAnyInteractive( SuiDocument doc )
	{
		if ( doc?.Elements == null ) return false;
		foreach ( var el in doc.Elements )
		{
			if ( el == null ) continue;
			if ( el.Type == SuiElementType.Button
				|| el.Type == SuiElementType.InventorySlot
				|| el.Type == SuiElementType.ItemIcon ) return true;
		}
		return false;
	}

	private static bool HasAnyEventsOrRefs( SuiDocument doc )
	{
		if ( doc?.Elements == null ) return false;
		foreach ( var el in doc.Elements )
		{
			if ( el == null ) continue;
			if ( el.Flags != null && el.Flags.ExposeAsVariable ) return true;
			if ( el.Events != null )
			{
				foreach ( var kv in el.Events )
				{
					if ( kv.Value != null && kv.Value.Mode == SuiEventMode.Code
						&& !string.IsNullOrEmpty( kv.Value.Handler ) ) return true;
				}
			}
		}
		return false;
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
	/// child's namespace+class+PublicVariables via <see cref="SuiGenerationContext.ResolveReferencedClass"/>,
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

		// V1.5-M2-K7-bugfix — s&box Razor markup tags are resolved by
		// CLASS NAME only (matches the ui-razor.md skill: `<HealthBar />`).
		// A tag with dots like `<Game.UI.HealthBar />` is treated as a
		// literal HTML tag and renders nothing — that was the embed bug
		// where TestparentPanel painted its own elements but the nested
		// `<Game.UI.HudBindtestPanel />` produced empty output.
		// Parent + child share the same namespace by default (Game.UI), so
		// the short tag resolves naturally inside the parent's @code class
		// without needing an extra @using directive.
		var childPanelType = target.ClassName + "Panel";
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
		// V1.5-M2-K7-bugfix — the SuiReference element has its own user class
		// (Style.ClassName) and a stable unique class (sui-<id>). The SCSS
		// generator emits position/size rules under the UNIQUE class, but the
		// markup tag must carry both classes for those rules to apply —
		// otherwise the child Panel falls back to default flex sizing and
		// stacks at top-left of the parent (the "twins on top of each other"
		// bug). Build the class string here and forward to EmitNamedChildTag.
		var userClass = SuiNameSanitizer.ToCssClass( el.Style?.ClassName ?? el.Type.ToString() );
		var uniqueClass = ElementUniqueClass( el );
		var combinedClass = userClass == uniqueClass ? userClass : $"{userClass} {uniqueClass}";

		if ( isForEach )
		{
			_sb.Append( indent ).Append( "@if ( " ).Append( fieldName ).AppendLine( " != null )" );
			_sb.Append( indent ).AppendLine( "{" );
			_sb.Append( indent ).Append( "  @foreach ( var __item in " ).Append( fieldName ).AppendLine( " )" );
			_sb.Append( indent ).AppendLine( "  {" );
			// V1.5-M2-K7-bugfix — guard each iteration on the item's IsShown.
			_sb.Append( indent ).AppendLine( "    @if ( __item == null || __item.IsShown )" );
			_sb.Append( indent ).AppendLine( "    {" );
			EmitNamedChildTag( childPanelType, target.PublicVariables, target.ChildReferenceFieldNames, "__item", indent + "      ", combinedClass );
			_sb.Append( indent ).AppendLine( "    }" );
			_sb.Append( indent ).AppendLine( "  }" );
			_sb.Append( indent ).AppendLine( "}" );
		}
		else
		{
			// V1.5-M2-K7-bugfix — guard the tag on the wrapper's IsShown so
			// Hide()/Show() on an embedded wrapper actually hides the nested
			// Panel via skip-rendering on the next BuildHash invalidation.
			// Inline `style="..."` proved fragile (Razor swallowed the whole
			// markup when the expression returned null in some configurations).
			_sb.Append( indent ).Append( "@if ( " ).Append( fieldName )
				.Append( " == null || " ).Append( fieldName ).AppendLine( ".IsShown )" );
			_sb.Append( indent ).AppendLine( "{" );
			EmitNamedChildTag( childPanelType, target.PublicVariables, target.ChildReferenceFieldNames, fieldName, indent + "  ", combinedClass );
			_sb.Append( indent ).AppendLine( "}" );
		}
	}

	private void EmitNamedChildTag(
		string childPanelType,
		System.Collections.Generic.IList<SuiVariable> targetVars,
		System.Collections.Generic.IList<string> targetChildRefFields,
		string accessorExpr,
		string indent,
		string cssClass )
	{
		_sb.Append( indent ).Append( "<" ).Append( childPanelType );
		if ( !string.IsNullOrEmpty( cssClass ) )
			_sb.Append( " class=\"" ).Append( cssClass ).Append( "\"" );

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

		// V1.5-M2-K7-bugfix — also forward the child wrapper's own SuiReference
		// fields so depth-2+ stays connected to user state. Without this, the
		// freshly Razor-constructed grandchild panel starts with default child
		// wrappers and grandchild mutations look invisible.
		if ( targetChildRefFields != null )
		{
			foreach ( var name in targetChildRefFields )
			{
				if ( string.IsNullOrEmpty( name ) ) continue;
				_sb.Append( " " ).Append( name )
					.Append( "=@(" ).Append( accessorExpr ).Append( '?' ).Append( '.' ).Append( name ).Append( ")" );
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

		// V1.5 M3.5 (PRD 25) — interactive types ship a runtime-toggleable
		// IsDisabled bool. Class string gains "disabled" when the field is
		// true; tabindex="0" makes <div> tab-focusable so :focus actually
		// fires for keyboard / controller nav.
		var isInteractive = el.Type == SuiElementType.Button
			|| el.Type == SuiElementType.InventorySlot
			|| el.Type == SuiElementType.ItemIcon;

		// V1.5 M4 (PRD 21) — input widgets emit real Sandbox.UI component
		// tags (PascalCase) instead of the generic <div>. They self-close
		// (no children, no intrinsic content beyond attributes).
		var inputTag = ResolveInputTag( el.Type );
		if ( inputTag != null )
		{
			// Slider needs the wrapper-div treatment because we render a
			// custom tooltip pill alongside it. Other widgets stay flat.
			if ( el.Type == SuiElementType.Slider )
			{
				EmitSliderWithTooltip( el, className, indent, dataAttrs, styleBody );
			}
			else
			{
				EmitInputWidgetTag( el, inputTag, className, indent, dataAttrs, styleBody );
			}
			return;
		}

		if ( isInteractive )
		{
			// Pure C# expression — `class="@<Name>Class()"`. The helper is
			// emitted by EmitInteractiveDisabledFields and returns the
			// concatenated class string. No mixed content, no nested quote
			// escapes — eliminates the class of Razor parser bugs that
			// regressed in commit `1d2ef34`.
			var classMethodName = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "Class" );
			_sb.Append( indent ).Append( "<div class=\"@" ).Append( classMethodName ).Append( "()\"" )
				.Append( " tabindex=\"0\"" )
				.Append( dataAttrs );
		}
		else
		{
			_sb.Append( indent ).Append( "<div class=\"" ).Append( className ).Append( "\"" )
				.Append( dataAttrs );
		}
		if ( styleBody.Length > 0 )
			_sb.Append( " style=\"" ).Append( styleBody ).Append( "\"" );
		// V1.5 M3 — `@ref="X"` for exposed elements + `onclick=@Handler` etc.
		// for every Code-mode event slot. Both emitters are no-ops when the
		// element has neither, so the tag stays clean for static UIs.
		SuiElementRefEmitter.EmitRazorRef( el, _sb );
		SuiEventEmitter.EmitRazorAttributes( el, _sb );

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

	/// <summary>
	/// V1.5 M4 — emit per-Slider state on the renderer:
	/// <list type="bullet">
	///   <item>`<Name>SliderValue` — the current value, bound to the
	///         tooltip + fill + thumb position via inline style.</item>
	///   <item>`<Name>SliderValue_OnTrackPress` — mouse-down handler that
	///         starts the drag.</item>
	///   <item>Single shared `Tick()` override that loops over every
	///         slider's drag state. Only emitted when at least one
	///         slider is present in the document.</item>
	/// </list>
	/// Returns the value field names so BuildHash picks them up.
	/// </summary>
	private System.Collections.Generic.List<string> EmitSliderValueFields( System.Text.StringBuilder body )
	{
		var hashes = new System.Collections.Generic.List<string>();
		if ( _doc?.Elements == null ) return hashes;

		var inv = System.Globalization.CultureInfo.InvariantCulture;
		var sliders = new System.Collections.Generic.List<(string Field, string Min, string Max, string Step)>();

		foreach ( var el in _doc.Elements )
		{
			if ( el == null || el.Type != SuiElementType.Slider ) continue;
			var fieldName = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "SliderValue" );
			if ( string.IsNullOrEmpty( fieldName ) ) continue;

			var defaultLit = (el.Props?.SliderValue ?? 0f).ToString( "0.###", inv );
			var minLit = (el.Props?.SliderMin ?? 0f).ToString( "0.###", inv );
			var maxLit = (el.Props?.SliderMax ?? 100f).ToString( "0.###", inv );
			var stepLit = (el.Props?.SliderStep ?? 1f).ToString( "0.###", inv );

			body.Append( "\tpublic float " ).Append( fieldName )
				.Append( " { get; set; } = " ).Append( defaultLit ).AppendLine( "f;" );
			body.Append( "\tprivate global::Sandbox.UI.Panel " ).Append( fieldName ).AppendLine( "_TrackPanel;" );
			body.Append( "\tprivate bool " ).Append( fieldName ).AppendLine( "_Dragging;" );
			// Engine invokes Razor `onmousedown` callbacks with the base
			// PanelEvent; we cast to MousePanelEvent inside (declaring the
			// handler directly with MousePanelEvent fails CS1503).
			//
			// Math mirrors engine SliderControl.ScreenPosToValue:
			//   normalized = LerpInverse(Mouse.Position.x, Box.Left, Box.Right)
			// Box.Left/Right are screen-pixel coordinates of the track.
			body.Append( "\tprivate void " ).Append( fieldName ).AppendLine( "_OnTrackPress( global::Sandbox.UI.PanelEvent baseEvent )" );
			body.AppendLine( "\t{" );
			body.AppendLine( "\t\tif ( baseEvent is not global::Sandbox.UI.MousePanelEvent e ) return;" );
			body.AppendLine( "\t\tif ( e.Button != \"mouseleft\" ) return;" );
			body.AppendLine( "\t\tif ( e.Target == null || !e.Target.IsValid() ) return;" );
			body.Append( "\t\t" ).Append( fieldName ).AppendLine( "_TrackPanel = e.Target;" );
			body.Append( "\t\t" ).Append( fieldName ).AppendLine( "_Dragging = true;" );
			body.Append( "\t\t" ).Append( fieldName ).Append( "_UpdateFromMouseScreen( global::Sandbox.Mouse.Position.x );" ).AppendLine();
			body.AppendLine( "\t}" );
			body.Append( "\tprivate void " ).Append( fieldName ).AppendLine( "_UpdateFromMouseScreen( float mouseScreenX )" );
			body.AppendLine( "\t{" );
			body.Append( "\t\tvar track = " ).Append( fieldName ).AppendLine( "_TrackPanel;" );
			body.AppendLine( "\t\tif ( track == null || !track.IsValid() ) return;" );
			body.AppendLine( "\t\tvar normalized = global::Sandbox.MathX.LerpInverse( mouseScreenX, track.Box.Left, track.Box.Right, true );" );
			body.Append( "\t\tvar v = global::Sandbox.MathX.LerpTo( " ).Append( minLit ).Append( "f, " ).Append( maxLit ).AppendLine( "f, normalized, true );" );
			body.Append( "\t\tvar step = " ).Append( stepLit ).AppendLine( "f;" );
			body.AppendLine( "\t\tif ( step > 0 ) v = global::System.MathF.Round( v / step ) * step;" );
			body.Append( "\t\t" ).Append( fieldName ).AppendLine( " = v;" );
			body.AppendLine( "\t\tStateHasChanged();" );
			body.AppendLine( "\t}" );

			sliders.Add( (fieldName, minLit, maxLit, stepLit) );
			hashes.Add( fieldName );
		}

		// Single shared Tick that drives every slider's drag. While left-mouse
		// is held + we're dragging, update from the current mouse position
		// using engine-style screen-pixel math (Box.Left/Right, no scaling).
		if ( sliders.Count > 0 )
		{
			body.AppendLine( "\tpublic override void Tick()" );
			body.AppendLine( "\t{" );
			body.AppendLine( "\t\tbase.Tick();" );
			body.AppendLine( "\t\tvar mouseDown = global::Sandbox.Input.Down( \"attack1\" );" );
			foreach ( var s in sliders )
			{
				body.Append( "\t\tif ( " ).Append( s.Field ).AppendLine( "_Dragging )" );
				body.AppendLine( "\t\t{" );
				body.Append( "\t\t\tvar tp = " ).Append( s.Field ).AppendLine( "_TrackPanel;" );
				body.AppendLine( "\t\t\tif ( tp == null || !tp.IsValid() || !mouseDown )" );
				body.AppendLine( "\t\t\t{" );
				body.Append( "\t\t\t\t" ).Append( s.Field ).AppendLine( "_Dragging = false;" );
				body.AppendLine( "\t\t\t}" );
				body.AppendLine( "\t\t\telse" );
				body.AppendLine( "\t\t\t{" );
				body.Append( "\t\t\t\t" ).Append( s.Field ).AppendLine( "_UpdateFromMouseScreen( global::Sandbox.Mouse.Position.x );" );
				body.AppendLine( "\t\t\t}" );
				body.AppendLine( "\t\t}" );
			}
			body.AppendLine( "\t}" );
		}

		return hashes;
	}

	/// <summary>
	/// V1.5 M4 (PRD 21) — emit a static <c>List&lt;Option&gt;</c> field per
	/// DropDown element so its <c>Options=@&lt;Name&gt;Options</c> attribute
	/// resolves. Authoring-time list lives in <c>SuiElementProps.DropDownOptions</c>;
	/// runtime mutation (replace whole list) lands in V1.6.
	/// </summary>
	private void EmitDropDownOptions( System.Text.StringBuilder body )
	{
		if ( _doc?.Elements == null ) return;
		foreach ( var el in _doc.Elements )
		{
			if ( el == null || el.Type != SuiElementType.DropDown ) continue;
			var fieldName = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "Options" );
			if ( string.IsNullOrEmpty( fieldName ) ) continue;

			body.Append( "\tpublic global::System.Collections.Generic.List<global::Sandbox.UI.Option> " )
				.Append( fieldName )
				.Append( " { get; set; } = new global::System.Collections.Generic.List<global::Sandbox.UI.Option> { " );

			var opts = el.Props?.DropDownOptions;
			if ( opts != null )
			{
				for ( int i = 0; i < opts.Count; i++ )
				{
					if ( i > 0 ) body.Append( ", " );
					var label = (opts[i] ?? "").Replace( "\"", "\\\"" );
					body.Append( "new global::Sandbox.UI.Option( \"" ).Append( label ).Append( "\", " ).Append( i ).Append( " )" );
				}
			}

			body.AppendLine( " };" );
		}
	}

	/// <summary>
	/// V1.5 M4 (PRD 21) — map input widget element types to their
	/// Sandbox.UI tag name. Casing matches Sandbox.UI's PascalCase Razor
	/// convention (skill ref ui-razor.md § Built-in Controls). Non-input
	/// types return null and the generic &lt;div&gt; path is used.
	/// </summary>
	private static string ResolveInputTag( SuiElementType t ) => t switch
	{
		SuiElementType.TextEntry => "TextEntry",
		// Engine tag is SliderControl (not SliderScale / Slider — those don't
		// exist as types). Confirmed against sbox-public + sbox-ui-lab.
		SuiElementType.Slider => "SliderControl",
		SuiElementType.Toggle => "Checkbox",
		SuiElementType.DropDown => "DropDown",
		_ => null,
	};

	/// <summary>
	/// V1.5 M4 (PRD 21) — Slider markup is 100% ours. NO engine SliderControl —
	/// the engine slider's value-tooltip can't be recolored from user-side
	/// SCSS (parser doesn't honor compound class selectors), so we build the
	/// entire control: track, fill, thumb, tooltip pill. Drag math lives in
	/// the renderer's @code helper (`OnSliderMouseDown/Move/Up`) which
	/// updates the authored Value via the bound property.
	/// </summary>
	private void EmitSliderWithTooltip( SuiElement el, string className, string indent, string dataAttrs, string styleBody )
	{
		var p = el.Props;
		var inv = System.Globalization.CultureInfo.InvariantCulture;
		var valueField = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "SliderValue" );
		var minLit = (p?.SliderMin ?? 0f).ToString( "0.###", inv );
		var maxLit = (p?.SliderMax ?? 100f).ToString( "0.###", inv );

		// Wrapper carries the authored position/size + sui-el-X class.
		_sb.Append( indent ).Append( "<div class=\"" ).Append( className ).Append( " sui-slider\"" ).Append( dataAttrs );
		if ( styleBody.Length > 0 )
			_sb.Append( " style=\"" ).Append( styleBody ).Append( "\"" );
		SuiElementRefEmitter.EmitRazorRef( el, _sb );
		SuiEventEmitter.EmitRazorAttributes( el, _sb );
		_sb.AppendLine( ">" );

		var inner = new string( ' ', (indent.Length / 2 + 1) * 2 );

		// Track — captures mousedown/move to drive the value. The
		// renderer's helper method computes Value from the mouse X
		// position relative to track bounds.
		_sb.Append( inner ).Append( "<div class=\"sui-slider-track\" onmousedown=@(e => " )
			.Append( valueField ).Append( "_OnTrackPress(e))>" ).AppendLine();

		// Fill bar — width follows the value via inline style. `bottom`/`top`
		// shorthand not needed; flex parent stacks horizontally.
		var posExpr = $"({valueField} - {minLit}f) / ({maxLit}f - {minLit}f) * 100";
		_sb.Append( inner ).Append( "  <div class=\"sui-slider-fill\" style=\"width: @(" )
			.Append( posExpr ).AppendLine( ")%\"></div>" );

		// Thumb — left position follows the same expression.
		_sb.Append( inner ).Append( "  <div class=\"sui-slider-thumb\" style=\"left: @(" )
			.Append( posExpr ).AppendLine( ")%\"></div>" );

		// Tooltip pill above the thumb — only when the author wants it.
		if ( p?.SliderShowValue == true )
		{
			_sb.Append( inner ).Append( "  <div class=\"sui-slider-tooltip\" style=\"left: @(" )
				.Append( posExpr ).AppendLine( ")%\">" );
			_sb.Append( inner ).Append( "    <label>@(" ).Append( valueField ).AppendLine( ".ToString(\"0.##\"))</label>" );
			_sb.Append( inner ).AppendLine( "    <div class=\"sui-slider-tooltip-tail\"></div>" );
			_sb.Append( inner ).AppendLine( "  </div>" );
		}

		_sb.Append( inner ).AppendLine( "</div>" );
		_sb.Append( indent ).AppendLine( "</div>" );
	}

	/// <summary>
	/// V1.5 M4 — emit one of the four Sandbox.UI input controls as a
	/// self-closing Razor tag with its widget-specific attributes. The class
	/// + dataAttrs + styleBody are still emitted so two-way binding metadata
	/// (`:bind`-suffix attributes) and Variable-bound style overrides work
	/// the same as on a regular `&lt;div&gt;`.
	/// </summary>
	private void EmitInputWidgetTag( SuiElement el, string tag, string className, string indent, string dataAttrs, string styleBody )
	{
		_sb.Append( indent ).Append( '<' ).Append( tag )
			.Append( " class=\"" ).Append( className ).Append( "\"" )
			.Append( dataAttrs );
		if ( styleBody.Length > 0 )
			_sb.Append( " style=\"" ).Append( styleBody ).Append( "\"" );

		var p = el.Props;
		var inv = System.Globalization.CultureInfo.InvariantCulture;
		switch ( el.Type )
		{
			case SuiElementType.TextEntry:
				if ( !string.IsNullOrEmpty( p?.PlaceholderText ) )
					_sb.Append( " Placeholder=\"" ).Append( EscapeForAttr( p.PlaceholderText ) ).Append( "\"" );
				// MaxLength is Nullable<int> on the engine type — Razor needs
				// a C# expression, not a string literal, or CS0029 fires.
				if ( p != null && p.MaxLength > 0 )
					_sb.Append( " MaxLength=\"@(" ).Append( p.MaxLength ).Append( ")\"" );
				if ( p != null && p.ReadOnly )
					_sb.Append( " ReadOnly=\"@true\"" );
				break;

			case SuiElementType.Slider:
				// Numerics on SliderControl are bound to float / int — Razor
				// expression form ("@(N)") is the idiomatic way per
				// sbox-public/.../GameSettings.razor.
				_sb.Append( " Min=\"@(" ).Append( p?.SliderMin.ToString( "0.###", inv ) ).Append( "f)\"" )
					.Append( " Max=\"@(" ).Append( p?.SliderMax.ToString( "0.###", inv ) ).Append( "f)\"" )
					.Append( " Step=\"@(" ).Append( p?.SliderStep.ToString( "0.###", inv ) ).Append( "f)\"" );
				// Pre-fill the value with the authored design-time preview
				// so the slider opens at the position the user set, not at 0.
				if ( p != null )
					_sb.Append( " Value=\"@(" ).Append( p.SliderValue.ToString( "0.###", inv ) ).Append( "f)\"" );
				// Always emit ShowValueTooltip explicitly so the engine
				// default (true) doesn't override our intent. Default in
				// SuiElementProps is false, so the tooltip is hidden unless
				// the author opts in via "Show Value Tooltip" in the
				// inspector.
				_sb.Append( " ShowValueTooltip=\"@" );
				_sb.Append( p?.SliderShowValue == true ? "true" : "false" );
				_sb.Append( "\"" );
				break;

			case SuiElementType.Toggle:
				if ( !string.IsNullOrEmpty( p?.ToggleLabelText ) )
					_sb.Append( " LabelText=\"" ).Append( EscapeForAttr( p.ToggleLabelText ) ).Append( "\"" );
				// Initial state — emit Checked when authoring-time true so the
				// engine's `.checked` class fires on first paint (canvas+Play
				// match without user code).
				if ( p != null && p.ToggleChecked )
					_sb.Append( " Checked=\"@true\"" );
				break;

			case SuiElementType.DropDown:
				// Static options list ships as the initial @Options value via
				// the @code block (see EmitDropDownOptions). Runtime-bindable
				// Options is deferred to V1.6 per PRD 21 § 11 #4.
				if ( p?.DropDownOptions != null && p.DropDownOptions.Count > 0 )
				{
					var optsField = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "Options" );
					_sb.Append( " Options=@" ).Append( optsField );

					// Pre-select Value so the DropDown shows the option title
					// from its first paint. Engine resolves Value -> Selected
					// via Option.Value lookup; we use the numeric index (the
					// same Option.Value we set in EmitDropDownOptions).
					if ( p.DropDownSelectedIndex >= 0 && p.DropDownSelectedIndex < p.DropDownOptions.Count )
						_sb.Append( " Value=\"@(" ).Append( p.DropDownSelectedIndex ).Append( ")\"" );
				}
				break;
		}

		SuiElementRefEmitter.EmitRazorRef( el, _sb );
		SuiEventEmitter.EmitRazorAttributes( el, _sb );

		_sb.Append( " />" ).AppendLine();
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
