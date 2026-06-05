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
		// Same emission also threads `<elName>Highlighted` → `.highlighted`
		// via the same `<Name>Class()` helper (single class-string source).
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

		// V1.5 M4 — Two-way bind backing fields for TextEntry / Checkbox /
		// DropDown. Each widget gets a public field that user code reads /
		// writes; Razor `Value:bind` / `Checked:bind` keeps it in sync with
		// the widget. Included in BuildHash so author-side mutations
		// trigger a re-render.
		var inputBindExprs = EmitInputBindFields( body );
		foreach ( var expr in inputBindExprs )
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
	/// and one <c>public bool &lt;Name&gt;Highlighted</c> field per Button /
	/// InventorySlot / ItemIcon in the document. Authoring-time defaults
	/// live in <c>SuiElementProps.IsDisabled</c> / <c>SuiElementProps.IsHighlighted</c>.
	/// Returns the list of hash expressions to push into BuildHash so a runtime
	/// flip re-renders the class string with/without the `.disabled` and
	/// `.highlighted` classes.
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

			// V1.5 M3.5 — Highlighted toggle. Two emission paths so the class
			// helper agrees with however the value gets fed in:
			//   1) Element has a binding on `IsHighlighted` → the Variable
			//      already owns the panel-side field (emitted by the Variable
			//      codegen). Reference that Variable name directly in the
			//      class helper and SKIP the dedicated field — otherwise we'd
			//      emit a TabActiveHighlighted field that the binding never
			//      writes to (binding sets the Variable's IsTabActiveHighlighted
			//      twin), the class helper would read the wrong field, and the
			//      .highlighted CSS would never fire even with the bool toggled
			//      from gameplay code. Confirmed-broken pattern in quest_journal
			//      pre-2026-06-05.
			//   2) No binding → emit the dedicated `<ElementName>Highlighted`
			//      field same as before. Gameplay code writes it via
			//      `Hud.View?.<Name>Highlighted = true` (mirrors the IsDisabled
			//      pattern — no Variable round-trip needed).
			string highlightedExpr;
			var highlightedBinding = el.Bindings?.Find( b => b != null && b.Property == "IsHighlighted" );
			if ( highlightedBinding != null && highlightedBinding.Source != null )
			{
				var sourceVar = _doc.Variables?.Find( v => v != null && v.Id == highlightedBinding.Source.VariableId );
				if ( sourceVar != null && !string.IsNullOrEmpty( sourceVar.Name ) )
				{
					highlightedExpr = SuiNameSanitizer.ToCSharpIdentifier( sourceVar.Name );
					// Variable already in BuildHash via the Variable codegen path
					// (SuiBuildHashEmitter walks the doc's Variables list). Do NOT
					// double-add here.
				}
				else
				{
					// Binding declared but source variable missing/null. Render
					// with a literal `false` so the class helper compiles; the
					// underlying authoring issue will surface as a validator
					// warning, not a runtime exception.
					highlightedExpr = "false";
				}
			}
			else
			{
				var highlightedField = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "Highlighted" );
				var highlightedDefault = (el.Props?.IsHighlighted ?? false) ? "true" : "false";
				body.Append( "\tpublic bool " ).Append( highlightedField )
					.Append( " { get; set; } = " ).Append( highlightedDefault ).AppendLine( ";" );
				hashes.Add( highlightedField );
				highlightedExpr = highlightedField;
			}

			// V1.5 M3.5 — emit a pure C# helper that returns the class string
			// for this element. The Razor markup invokes it via the simple
			// expression `class="@<Name>Class()"` — no mixed content, no
			// nested quote-escape gymnastics that would confuse the Razor
			// parser (which is what broke commit `1d2ef34`).
			// Concatenates BOTH `.disabled` and `.highlighted` from the same
			// helper so the markup stays a single attribute expression.
			var methodName = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "Class" );
			var staticClasses = BuildElementClassLiteral( el );
			body.Append( "\tprivate string " ).Append( methodName )
				.Append( "() => \"" ).Append( staticClasses )
				.Append( "\" + (" ).Append( fieldName ).Append( " ? \" disabled\" : \"\")" )
				.Append( " + (" ).Append( highlightedExpr ).AppendLine( " ? \" highlighted\" : \"\");" );
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
			.Append( bindAttrs );
		// V1.5 — Text elements with ExposeAsVariable=true need the @ref so
		// the renderer's typed Label field captures the live engine instance.
		// Without this the field stays null forever and controllers can't
		// reach the live label to mutate .Text per frame (typewriter, runtime
		// scoreboard, etc.). Container emitters already call this; Text
		// was the lone code path that skipped it.
		SuiElementRefEmitter.EmitRazorRef( el, _sb );
		_sb.Append( ">" )
			.Append( body )
			.AppendLine( "</label>" );
	}

	private void EmitContainerElement( SuiElement el, string className, string indent, int depth )
	{
		var hasChildren = el.Children != null && el.Children.Count > 0;

		// Open tag — class attribute first, then V1.5 data-sui-* binding attrs
		// + a unified inline style="" body.
		//
		// IMPORTANT: do NOT inject `position: relative` here for ProgressBar
		// containers. The element's SCSS already emits `position: absolute`
		// (via EmitLayout — every Absolute-mode element gets it) AND
		// `overflow: hidden` (via EmitStyle when Overflow=Hidden on the
		// element, which the ProgressBar default already sets). Injecting
		// inline `position: relative` overrides the SCSS-declared absolute,
		// dropping the bar out of its own X/Y placement and into the parent
		// container's flex flow. With multiple bars in the same parent,
		// each bar staircases right (Sandbox.UI flex with auto-shrink). The
		// inner `.sui-progress-fill` div anchors via its own SCSS rule
		// (SuiScssGenerator emits it scoped to typeName when ProgressBar
		// is present) — that rule uses position:absolute + left/top/bottom:0,
		// so it stretches the parent's content box regardless of whether
		// the parent is relative or absolute, as long as it's positioned.
		// The parent's SCSS-declared `position: absolute` satisfies that.
		var dataAttrs = SuiBindingEmitter.EmitElementDataAttrs( el, _doc );
		var styleBody = SuiBindingEmitter.EmitElementStyleBody( el, _doc );

		// V1.5 M3.5 (PRD 25) — interactive types ship runtime-toggleable
		// IsDisabled + IsHighlighted bools. Class string gains "disabled" /
		// "highlighted" via the same `<Name>Class()` helper; tabindex="0"
		// makes <div> tab-focusable so :focus actually fires for keyboard /
		// controller nav.
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
	/// V1.5 M4 — emit per-Slider state on the renderer, mirroring engine
	/// SliderControl exactly:
	/// <list type="bullet">
	///   <item>`<Name>SliderValue` — backing value (settable for binding).</item>
	///   <item>`<Name>SliderPosition` — computed property used in the
	///         inline `style="left: @(<Name>SliderPosition)%;"`. Engine
	///         uses this exact form; complex inline expressions with `%`
	///         confuse the Sandbox.UI Razor parser.</item>
	///   <item>`<Name>SliderValue_OnTrackInput` — mousedown handler. Jumps
	///         to the click position.</item>
	///   <item>`<Name>SliderValue_OnTrackMove` — mousemove handler. Engine
	///         pattern: only updates while `HasActive` is true (mouse
	///         button held after pressing on this panel). HasActive is
	///         the right signal in UI mode — `Input.Down("attack1")` is
	///         only valid in cursor-locked game mode and silently
	///         returns false in UI cursor mode.</item>
	/// </list>
	/// Returns the value field names so BuildHash picks them up.
	/// </summary>
	private System.Collections.Generic.List<string> EmitSliderValueFields( System.Text.StringBuilder body )
	{
		var hashes = new System.Collections.Generic.List<string>();
		if ( _doc?.Elements == null ) return hashes;

		var inv = System.Globalization.CultureInfo.InvariantCulture;

		bool needsTickOverride = false;
		var sliderTickEntries = new System.Collections.Generic.List<(string Field, string Visual, string Cast, bool ReleaseCommit)>();

		foreach ( var el in _doc.Elements )
		{
			if ( el == null || el.Type != SuiElementType.Slider ) continue;
			var baseName = el.Name ?? el.Id;
			// V1.5 M4 — if bound, the "commit field" is the Variable's name
			// (declared elsewhere by SuiVariableEmitter). If unbound, we
			// declare a local public field as the commit target.
			var boundVar = TryGetBoundVariableName( el, "Value" );
			var trigger = TryGetBindUpdateTrigger( el, "Value" );
			var commitField = boundVar ?? SuiNameSanitizer.ToCSharpIdentifier( baseName + "SliderValue" );
			var posName = SuiNameSanitizer.ToCSharpIdentifier( baseName + "SliderPosition" );
			if ( string.IsNullOrEmpty( commitField ) ) continue;

			var defaultLit = (el.Props?.SliderValue ?? 0f).ToString( "0.###", inv );
			var minLit = (el.Props?.SliderMin ?? 0f).ToString( "0.###", inv );
			var maxLit = (el.Props?.SliderMax ?? 100f).ToString( "0.###", inv );
			var stepLit = (el.Props?.SliderStep ?? 1f).ToString( "0.###", inv );

			// OnChange (or unbound) writes straight to the commit field. The
			// visual position reads the same field — no buffer needed.
			//
			// OnRelease / Manual need a SEPARATE visual buffer so dragging
			// updates the position locally without committing the Variable
			// each frame. The commit happens on mouseup (OnRelease) or via
			// an explicit wrapper.Commit<Name>SliderValue() call (Manual).
			var useVisualBuffer = boundVar != null &&
				( trigger == SuiBindingUpdateTrigger.OnRelease
					|| trigger == SuiBindingUpdateTrigger.Manual );
			var visualField = useVisualBuffer
				? "_" + char.ToLowerInvariant( commitField[0] ) + commitField.Substring( 1 ) + "Visual"
				: commitField;

			var boundV = TryGetBoundVariable( el, "Value" );
			var castPrefix = boundV != null && boundV.Type == "int" ? "(int)" : "";

			// Backing field: emit ONLY when unbound (no Variable to back it).
			// Bound sliders write into the Variable's public property which
			// SuiVariableEmitter already declared.
			if ( boundVar == null )
			{
				body.Append( "\tpublic float " ).Append( commitField )
					.Append( " { get; set; } = " ).Append( defaultLit ).AppendLine( "f;" );
			}
			// Visual buffer for deferred-commit triggers. Initialised to the
			// default; we resync from the Variable on idle Ticks so external
			// code can still drive the displayed position.
			if ( useVisualBuffer )
			{
				body.Append( "\tprivate float " ).Append( visualField )
					.Append( " = " ).Append( defaultLit ).AppendLine( "f;" );
			}
			body.Append( "\tprivate float " ).Append( posName )
				.Append( " => global::Sandbox.MathX.LerpInverse( (float)" ).Append( visualField )
				.Append( ", " ).Append( minLit ).Append( "f, " ).Append( maxLit )
				.AppendLine( "f, true ) * 100f;" );
			body.Append( "\tprivate global::Sandbox.UI.Panel " ).Append( commitField ).AppendLine( "_TrackPanel;" );

			// mousedown: capture track panel, jump value to click position.
			body.Append( "\tprivate void " ).Append( commitField ).AppendLine( "_OnTrackInput( global::Sandbox.UI.PanelEvent baseEvent )" );
			body.AppendLine( "\t{" );
			body.AppendLine( "\t\tif ( baseEvent is not global::Sandbox.UI.MousePanelEvent e ) return;" );
			body.AppendLine( "\t\tif ( e.Button != \"mouseleft\" ) return;" );
			body.AppendLine( "\t\tif ( e.Target == null || !e.Target.IsValid() ) return;" );
			body.Append( "\t\t" ).Append( commitField ).AppendLine( "_TrackPanel = e.Target;" );
			body.Append( "\t\t" ).Append( commitField ).Append( "_ScreenToValue( global::Sandbox.Mouse.Position );" ).AppendLine();
			body.AppendLine( "\t}" );

			// mousemove: only update while target has active (mouse held after pressing).
			body.Append( "\tprivate void " ).Append( commitField ).AppendLine( "_OnTrackMove( global::Sandbox.UI.PanelEvent baseEvent )" );
			body.AppendLine( "\t{" );
			body.AppendLine( "\t\tif ( baseEvent is not global::Sandbox.UI.MousePanelEvent e ) return;" );
			body.AppendLine( "\t\tif ( e.Target == null || !e.Target.IsValid() ) return;" );
			body.AppendLine( "\t\tif ( !e.Target.HasActive ) return;" );
			body.Append( "\t\t" ).Append( commitField ).AppendLine( "_TrackPanel = e.Target;" );
			body.Append( "\t\t" ).Append( commitField ).Append( "_ScreenToValue( global::Sandbox.Mouse.Position );" ).AppendLine();
			body.AppendLine( "\t}" );

			// Math mirrors engine SliderControl.ScreenPosToValue exactly.
			// Writes go to the VISUAL buffer (which equals the commit field
			// when the trigger is OnChange — so per-frame updates flow
			// straight to the Variable). For OnRelease/Manual the visual
			// buffer is separate; the commit happens later.
			body.Append( "\tprivate void " ).Append( commitField ).AppendLine( "_ScreenToValue( global::Vector2 pos )" );
			body.AppendLine( "\t{" );
			body.Append( "\t\tvar track = " ).Append( commitField ).AppendLine( "_TrackPanel;" );
			body.AppendLine( "\t\tif ( track == null || !track.IsValid() ) return;" );
			body.AppendLine( "\t\tvar normalized = global::Sandbox.MathX.LerpInverse( pos.x, track.Box.Left, track.Box.Right, true );" );
			body.Append( "\t\tvar v = global::Sandbox.MathX.LerpTo( " ).Append( minLit ).Append( "f, " ).Append( maxLit ).AppendLine( "f, normalized, true );" );
			body.Append( "\t\tvar step = " ).Append( stepLit ).AppendLine( "f;" );
			body.AppendLine( "\t\tif ( step > 0 ) v = global::System.MathF.Round( v / step ) * step;" );
			// VISUAL field is float-typed; cast only when committing to a
			// typed bound Variable (int Variable needs the cast).
			body.Append( "\t\t" ).Append( visualField ).Append( " = " );
			if ( useVisualBuffer )
				body.AppendLine( "v;" );
			else
				body.Append( castPrefix ).AppendLine( "v;" );
			body.AppendLine( "\t\tStateHasChanged();" );
			body.AppendLine( "\t}" );

			// Manual commit method for user code to call from a button handler.
			if ( useVisualBuffer && trigger == SuiBindingUpdateTrigger.Manual )
			{
				body.Append( "\tpublic void Commit" ).Append( commitField ).AppendLine( "()" );
				body.AppendLine( "\t{" );
				body.Append( "\t\t" ).Append( commitField ).Append( " = " ).Append( castPrefix ).Append( visualField ).AppendLine( ";" );
				body.AppendLine( "\t\tStateHasChanged();" );
				body.AppendLine( "\t}" );
			}

			if ( useVisualBuffer )
			{
				needsTickOverride = true;
				sliderTickEntries.Add( (commitField, visualField, castPrefix, trigger == SuiBindingUpdateTrigger.OnRelease) );
			}

			hashes.Add( useVisualBuffer ? visualField : commitField );
		}

		// Shared Tick override for any slider that uses a visual buffer:
		//   • OnRelease commits visual → Variable on the HasActive true→false
		//     transition (the moment the user releases the mouse).
		//   • BOTH modes resync visual ← Variable when an EXTERNAL write
		//     changed the Variable. "External" = changed since the previous
		//     Tick frame's snapshot; this protects Manual mode from the
		//     "drag, release, visual jumps back" bug where the persistent
		//     visual-vs-Variable diff was misread as a resync need.
		if ( needsTickOverride )
		{
			body.AppendLine( "\tpublic override void Tick()" );
			body.AppendLine( "\t{" );
			body.AppendLine( "\t\tbase.Tick();" );
			foreach ( var entry in sliderTickEntries )
			{
				var wasActive = "_" + entry.Field + "WasActive";
				var lastSeen = "_" + entry.Field + "LastSeen";
				body.Append( "\t\tvar " ).Append( wasActive ).Append( "Now = " )
					.Append( entry.Field ).AppendLine( "_TrackPanel?.HasActive ?? false;" );

				// OnRelease commit on transition.
				if ( entry.ReleaseCommit )
				{
					body.Append( "\t\tif ( _" ).Append( entry.Field ).Append( "WasActive && !" ).Append( wasActive ).AppendLine( "Now )" );
					body.AppendLine( "\t\t{" );
					body.Append( "\t\t\t" ).Append( entry.Field ).Append( " = " ).Append( entry.Cast ).Append( entry.Visual ).AppendLine( ";" );
					body.AppendLine( "\t\t}" );
				}

				// Idle external-write detection: resync visual ONLY when the
				// Variable's value actually moved between Ticks (someone else
				// wrote to it). A persistent Manual-mode diff doesn't count
				// because LastSeen also stays at the diverged value.
				body.Append( "\t\tif ( !" ).Append( wasActive ).Append( "Now && (float)" ).Append( entry.Field ).Append( " != (float)" ).Append( lastSeen ).AppendLine( " )" );
				body.AppendLine( "\t\t{" );
				body.Append( "\t\t\t" ).Append( entry.Visual ).Append( " = (float)" ).Append( entry.Field ).AppendLine( ";" );
				body.AppendLine( "\t\t}" );

				// Snapshot the Variable for next-frame comparison + advance
				// the active-state edge tracker.
				body.Append( "\t\t" ).Append( lastSeen ).Append( " = (float)" ).Append( entry.Field ).AppendLine( ";" );
				body.Append( "\t\t_" ).Append( entry.Field ).Append( "WasActive = " ).Append( wasActive ).AppendLine( "Now;" );
			}
			body.AppendLine( "\t}" );

			// Per-slider state across frames: active edge + last-seen Variable.
			foreach ( var entry in sliderTickEntries )
			{
				body.Append( "\tprivate bool _" ).Append( entry.Field ).AppendLine( "WasActive;" );
				body.Append( "\tprivate float _" ).Append( entry.Field ).Append( "LastSeen = " )
					.Append( entry.Visual.StartsWith( "_" ) ? "0f" : "float.NaN" ).AppendLine( ";" );
			}
		}

		return hashes;
	}

	/// <summary>
	/// V1.5 M4 — if the element has a TwoWay binding on <paramref name="property"/>
	/// to a Variable, returns the Variable's name (sanitized C# identifier).
	/// Returns null when no binding exists — caller falls back to a private
	/// local field so the widget remains interactive but the value stays
	/// internal (Unreal-style "no binding = silently functional").
	/// </summary>
	private string TryGetBoundVariableName( SuiElement el, string property )
	{
		var v = TryGetBoundVariable( el, property );
		return v == null ? null : SuiNameSanitizer.ToCSharpIdentifier( v.Name );
	}

	private SuiVariable TryGetBoundVariable( SuiElement el, string property )
	{
		if ( el?.Bindings == null || _doc?.Variables == null ) return null;
		foreach ( var b in el.Bindings )
		{
			if ( b == null || b.Property != property ) continue;
			if ( b.Source == null || string.IsNullOrEmpty( b.Source.VariableId ) ) continue;
			foreach ( var v in _doc.Variables )
			{
				if ( v?.Id == b.Source.VariableId ) return v;
			}
		}
		return null;
	}

	/// <summary>
	/// Returns the configured <see cref="SuiBindingUpdateTrigger"/> for a
	/// (element, property) binding pair, or <see cref="SuiBindingUpdateTrigger.OnChange"/>
	/// when no binding exists. Used by the TextEntry / Slider emit paths to
	/// choose between realtime, blur, submit, release, or manual write-back.
	/// </summary>
	private SuiBindingUpdateTrigger TryGetBindUpdateTrigger( SuiElement el, string property )
	{
		if ( el?.Bindings == null ) return SuiBindingUpdateTrigger.OnChange;
		foreach ( var b in el.Bindings )
		{
			if ( b == null || b.Property != property ) continue;
			return b.UpdateTrigger;
		}
		return SuiBindingUpdateTrigger.OnChange;
	}

	/// <summary>
	/// V1.5 M4 — emit PRIVATE backing fields for input widgets that have NO
	/// binding. Bound widgets reach the public Variable directly through
	/// `:bind="@<VarName>"` and don't need a local field. Unbound widgets
	/// still need somewhere to store their interactive state so the user
	/// can click/type, but the value never leaves the panel.
	/// </summary>
	private System.Collections.Generic.List<string> EmitInputBindFields( System.Text.StringBuilder body )
	{
		var hashes = new System.Collections.Generic.List<string>();
		if ( _doc?.Elements == null ) return hashes;

		foreach ( var el in _doc.Elements )
		{
			if ( el == null ) continue;
			var baseName = el.Name ?? el.Id;
			if ( string.IsNullOrEmpty( baseName ) ) continue;

			// Skip widgets that have a Variable binding — the Variable
			// supplies its own field; we'd be duplicating.
			switch ( el.Type )
			{
				case SuiElementType.TextEntry:
					{
						// When the binding uses a non-realtime trigger
						// (OnLostFocus/OnSubmit/Manual), the markup captures
						// an @ref to read the panel's .Text at the right
						// moment. The ref field must exist on the panel even
						// when the value field is owned by a Variable.
						//
						// Public visibility: the wrapper's Apply API reads
						// `view.<Name>Ref.Text` from outside the panel class
						// (D-029). Private would CS0122 the wrapper.
						var trigger = TryGetBindUpdateTrigger( el, "Value" );
						if ( trigger != SuiBindingUpdateTrigger.OnChange )
						{
							var refName = SuiNameSanitizer.ToCSharpIdentifier( baseName + "Ref" );
							body.Append( "\tpublic global::Sandbox.UI.TextEntry " ).Append( refName ).AppendLine( ";" );
						}

						if ( TryGetBoundVariableName( el, "Value" ) != null ) break;
						var fieldName = SuiNameSanitizer.ToCSharpIdentifier( baseName + "Value" );
						if ( string.IsNullOrEmpty( fieldName ) ) continue;
						var initial = el.Props?.PreviewValue ?? "";
						body.Append( "\tprivate string " ).Append( fieldName )
							.Append( " = \"" ).Append( EscapeForAttr( initial ) ).AppendLine( "\";" );
						hashes.Add( fieldName );
					}
					break;

				case SuiElementType.Toggle:
					{
						if ( TryGetBoundVariableName( el, "Checked" ) != null ) break;
						var fieldName = SuiNameSanitizer.ToCSharpIdentifier( baseName + "Checked" );
						if ( string.IsNullOrEmpty( fieldName ) ) continue;
						var initial = el.Props?.ToggleChecked == true ? "true" : "false";
						body.Append( "\tprivate bool " ).Append( fieldName )
							.Append( " = " ).Append( initial ).AppendLine( ";" );
						hashes.Add( fieldName );
					}
					break;

				case SuiElementType.DropDown:
					{
						if ( TryGetBoundVariableName( el, "Value" ) != null ) break;
						var fieldName = SuiNameSanitizer.ToCSharpIdentifier( baseName + "Value" );
						if ( string.IsNullOrEmpty( fieldName ) ) continue;
						var idx = el.Props?.DropDownSelectedIndex ?? 0;
						body.Append( "\tprivate int " ).Append( fieldName )
							.Append( " = " ).Append( idx ).AppendLine( ";" );
						hashes.Add( fieldName );
					}
					break;
			}
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
	/// V1.5 M4 (PRD 21) — Slider markup mirrors engine SliderControl.razor
	/// exactly (just our own class names): a positioned `<Name>Position`
	/// helper property fed into `style="left: @(<Name>Position)%;"` (note
	/// the trailing semicolon — engine uses this form and Sandbox.UI's
	/// Razor parser appears to need it for the inline % to resolve).
	///
	/// Drag handling mirrors engine: `onmousedown` jumps to the click
	/// position; `onmousemove` checks `e.Target.HasActive` (true while the
	/// mouse stays pressed on the track) and updates the value. No Tick
	/// polling — engine itself doesn't need it.
	/// </summary>
	private void EmitSliderWithTooltip( SuiElement el, string className, string indent, string dataAttrs, string styleBody )
	{
		var p = el.Props;
		// V1.5 M4 — if the Slider is bound to a Variable, the user-visible
		// name is the Variable name (`Volume`). Otherwise we fall back to a
		// private local field (widget interacts but value stays internal).
		var boundVar = TryGetBoundVariableName( el, "Value" );
		var valueField = boundVar ?? SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "SliderValue" );
		var posProp = SuiNameSanitizer.ToCSharpIdentifier( ( el.Name ?? el.Id ) + "SliderPosition" );

		_sb.Append( indent ).Append( "<div class=\"" ).Append( className ).Append( " sui-slider\"" ).Append( dataAttrs );
		if ( styleBody.Length > 0 )
			_sb.Append( " style=\"" ).Append( styleBody ).Append( "\"" );
		SuiElementRefEmitter.EmitRazorRef( el, _sb );
		SuiEventEmitter.EmitRazorAttributes( el, _sb );
		_sb.AppendLine( ">" );

		var inner = new string( ' ', (indent.Length / 2 + 1) * 2 );

		// Track. onmousedown jumps, onmousemove updates while HasActive on
		// the same panel (engine convention).
		_sb.Append( inner ).Append( "<div class=\"sui-slider-track\"" )
			.Append( " onmousedown=@(e => " ).Append( valueField ).Append( "_OnTrackInput(e))" )
			.Append( " onmousemove=@(e => " ).Append( valueField ).Append( "_OnTrackMove(e))>" ).AppendLine();

		_sb.Append( inner ).Append( "  <div class=\"sui-slider-fill\" style=\"width: @(" )
			.Append( posProp ).AppendLine( ")%;\"></div>" );

		_sb.Append( inner ).Append( "  <div class=\"sui-slider-thumb\" style=\"left: @(" )
			.Append( posProp ).AppendLine( ")%;\"></div>" );

		if ( p?.SliderShowValue == true )
		{
			_sb.Append( inner ).Append( "  <div class=\"sui-slider-tooltip\" style=\"left: @(" )
				.Append( posProp ).AppendLine( ")%;\">" );
			_sb.Append( inner ).Append( "    <label class=\"sui-slider-tooltip-label\">@(" ).Append( valueField ).AppendLine( ".ToString(\"0.##\"))</label>" );
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
		var baseName = el.Name ?? el.Id;
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
				{
					// Two-way bind target: the bound Variable's name when one
					// exists, else the private local field emitted by
					// EmitInputBindFields (widget interacts but value stays
					// internal — Unreal-style "unbound = silently functional").
					var teTarget = TryGetBoundVariableName( el, "Value" )
						?? SuiNameSanitizer.ToCSharpIdentifier( baseName + "Value" );
					var trigger = TryGetBindUpdateTrigger( el, "Value" );
					var refName = SuiNameSanitizer.ToCSharpIdentifier( baseName + "Ref" );

					switch ( trigger )
					{
						case SuiBindingUpdateTrigger.OnChange:
							// Realtime — engine's `Value:bind` fires per keystroke.
							_sb.Append( " Value:bind=\"@" ).Append( teTarget ).Append( "\"" );
							break;

						case SuiBindingUpdateTrigger.OnLostFocus:
							// One-way pre-fill + onblur captures the typed text
							// and commits to the bound field.
							_sb.Append( " Value=\"@" ).Append( teTarget ).Append( "\"" )
								.Append( " @ref=\"" ).Append( refName ).Append( "\"" )
								.Append( " onblur=@(e => " ).Append( teTarget ).Append( " = " ).Append( refName ).Append( "?.Text ?? \"\")" );
							break;

						case SuiBindingUpdateTrigger.OnSubmit:
							// Same as OnLostFocus but waits for Enter (engine
							// `onsubmit` fires only on the Enter key).
							_sb.Append( " Value=\"@" ).Append( teTarget ).Append( "\"" )
								.Append( " @ref=\"" ).Append( refName ).Append( "\"" )
								.Append( " onsubmit=@(e => " ).Append( teTarget ).Append( " = " ).Append( refName ).Append( "?.Text ?? \"\")" );
							break;

						case SuiBindingUpdateTrigger.Manual:
							// Pre-fill only. The wrapper exposes Commit<Name>()
							// that reads the panel ref's .Text and writes the bound field.
							_sb.Append( " Value=\"@" ).Append( teTarget ).Append( "\"" )
								.Append( " @ref=\"" ).Append( refName ).Append( "\"" );
							break;
					}
				}
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
				{
					var toggleTarget = TryGetBoundVariableName( el, "Checked" )
						?? SuiNameSanitizer.ToCSharpIdentifier( baseName + "Checked" );
					_sb.Append( " Checked:bind=\"@" ).Append( toggleTarget ).Append( "\"" );
				}
				break;

			case SuiElementType.DropDown:
				// Static options list ships as the initial @Options value via
				// the @code block (see EmitDropDownOptions). Runtime-bindable
				// Options is deferred to V1.6 per PRD 21 § 11 #4.
				if ( p?.DropDownOptions != null && p.DropDownOptions.Count > 0 )
				{
					var optsField = SuiNameSanitizer.ToCSharpIdentifier( baseName + "Options" );
					_sb.Append( " Options=@" ).Append( optsField );

					var ddTarget = TryGetBoundVariableName( el, "Value" )
						?? SuiNameSanitizer.ToCSharpIdentifier( baseName + "Value" );
					_sb.Append( " Value:bind=\"@" ).Append( ddTarget ).Append( "\"" );
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

		// position / left / top / bottom / pointer-events for .sui-progress-fill
		// are emitted ONCE in SCSS (SuiScssGenerator emits the global rule when
		// the document has any ProgressBar element). Sandbox.UI's inline-style
		// parser silently drops `position: absolute` on the first child of an
		// absolutely-positioned parent — fills collapse into flex flow and bars
		// staircase. width % + bg-color stay inline because they're per-instance
		// (value-driven + FillColor-driven) and the engine honours them there.
		_sb.Append( indent )
			.Append( "<div class=\"sui-progress-fill\" style=\"width: @(" )
			.Append( pct )
			.Append( ")%; background-color: @(" )
			.Append( fillColorExpr )
			.AppendLine( ");\"></div>" );
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
