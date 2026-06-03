using System.Collections.Generic;
using System.Text;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Validates a <see cref="SuiDocument"/> against the schema invariants documented
/// in PRD doc 05. Pure logic — no I/O, no editor dependency.
/// </summary>
public static class SuiDocumentValidator
{
	/// <summary>Errors block compile. Warnings are surfaced to the user but don't block.</summary>
	public sealed class Result
	{
		public List<string> Errors { get; } = new();
		public List<string> Warnings { get; } = new();
		public bool IsValid => Errors.Count == 0;
	}

	public static Result Validate( SuiDocument doc )
	{
		var r = new Result();
		if ( doc == null )
		{
			r.Errors.Add( "document is null" );
			return r;
		}

		if ( doc.SchemaVersion < SuiSchemaVersion.MinimumSupported )
			r.Errors.Add( $"schemaVersion {doc.SchemaVersion} is older than minimum supported {SuiSchemaVersion.MinimumSupported}" );

		if ( string.IsNullOrEmpty( doc.DocumentId ) )
			r.Errors.Add( "documentId is missing" );

		if ( string.IsNullOrEmpty( doc.Name ) )
			r.Errors.Add( "name is missing" );

		// Exactly one root.
		int rootCount = 0;
		foreach ( var el in doc.Elements )
		{
			if ( string.IsNullOrEmpty( el.ParentId ) ) rootCount++;
		}
		if ( rootCount == 0 ) r.Errors.Add( "no root element (expected exactly one element with null parentId)" );
		if ( rootCount > 1 ) r.Errors.Add( $"multiple root elements ({rootCount}) — expected exactly one" );

		// No duplicate ids.
		var seen = new HashSet<string>();
		foreach ( var el in doc.Elements )
		{
			if ( string.IsNullOrEmpty( el.Id ) )
			{
				r.Errors.Add( $"element with name '{el.Name}' has no id" );
				continue;
			}
			if ( !seen.Add( el.Id ) )
				r.Errors.Add( $"duplicate element id: {el.Id}" );
		}

		// Build a lookup for parent/child checks.
		var byId = new Dictionary<string, SuiElement>();
		foreach ( var el in doc.Elements )
		{
			if ( !string.IsNullOrEmpty( el.Id ) )
				byId[el.Id] = el;
		}

		// Every parentId must resolve to an existing element (or be null for root).
		foreach ( var el in doc.Elements )
		{
			if ( string.IsNullOrEmpty( el.ParentId ) ) continue;
			if ( !byId.ContainsKey( el.ParentId ) )
				r.Errors.Add( $"element '{el.Id}' references unknown parentId '{el.ParentId}'" );
		}

		// Every children id must resolve and must agree with the child's ParentId.
		foreach ( var el in doc.Elements )
		{
			foreach ( var childId in el.Children )
			{
				if ( !byId.TryGetValue( childId, out var child ) )
				{
					r.Errors.Add( $"element '{el.Id}' lists child '{childId}' which does not exist" );
					continue;
				}
				if ( child.ParentId != el.Id )
					r.Errors.Add( $"element '{el.Id}' lists child '{childId}' but child.parentId is '{child.ParentId}'" );
			}
		}

		// No hierarchy cycles.
		foreach ( var el in doc.Elements )
		{
			if ( HasCycle( el, byId ) )
				r.Errors.Add( $"hierarchy cycle detected starting at element '{el.Id}'" );
		}

		// Per-element style/layout sanity.
		foreach ( var el in doc.Elements )
		{
			if ( el.Style != null )
			{
				if ( el.Style.Opacity < 0f || el.Style.Opacity > 1f )
					r.Errors.Add( $"element '{el.Id}' opacity={el.Style.Opacity} out of range [0..1]" );
			}
			if ( el.Layout != null )
			{
				if ( el.Layout.Width < 0f || el.Layout.Height < 0f )
					r.Errors.Add( $"element '{el.Id}' has negative width/height" );
			}
			if ( el.Props != null )
			{
				if ( el.Props.FontSize <= 0f && el.Type == SuiElementType.Text )
					r.Errors.Add( $"text element '{el.Id}' has fontSize <= 0" );
				if ( (el.Type == SuiElementType.Grid || el.Type == SuiElementType.InventoryGrid)
					&& (el.Props.Columns < 1 || el.Props.Rows < 1) )
					r.Errors.Add( $"grid element '{el.Id}' has columns/rows < 1" );

				// V1.5 M3.5 — interactive state ranges (PRD 25 § 4.3). Only
				// validated on element types that surface interactive states.
				if ( IsInteractiveType( el.Type ) )
					ValidateInteractiveProps( el, r );
			}
		}

		// Output validity (warnings only when not configured).
		if ( doc.Output != null && doc.Output.Configured )
		{
			if ( string.IsNullOrEmpty( doc.Output.RootFolder ) )
				r.Errors.Add( "output is configured but rootFolder is empty" );
			if ( string.IsNullOrEmpty( doc.Output.ClassName ) )
				r.Errors.Add( "output is configured but className is empty" );
		}

		// V1.5 — Variables (PRD 18 § 3.6) and Bindings (PRD 18 § 4.10).
		ValidateVariables( doc, r );
		ValidateBindings( doc, r );

		// V1.5 M3 — Events (PRD 20 § 3.6) and Element references (§ 5.5).
		ValidateEvents( doc, r );
		ValidateExposedElements( doc, r );

		// V1.5 M3 (PRD 20 § 8.2) — cross-paradigm collision detection.
		// Single pass that catches every name that is claimed by more than
		// one source (Variable / SuiReference field / Code handler / Doo
		// property / @ref). Without this the dev compiles, gets a CS0102,
		// fixes one offender, recompiles, gets another. Detect all at once.
		ValidateNameConflicts( doc, r );

		return r;
	}

	private static void ValidateNameConflicts( SuiDocument doc, Result r )
	{
		var groups = SuiNameConflictDetector.Detect( doc );
		foreach ( var kv in groups )
		{
			var labels = new System.Text.StringBuilder();
			for ( int i = 0; i < kv.Value.Count; i++ )
			{
				if ( i > 0 ) labels.Append( " vs " );
				labels.Append( kv.Value[i].Where );
			}
			r.Errors.Add( $"name collision on '{kv.Key}': {labels}" );
		}
	}

	private static void ValidateEvents( SuiDocument doc, Result r )
	{
		if ( doc?.Elements == null ) return;

		foreach ( var el in doc.Elements )
		{
			if ( el?.Events == null || el.Events.Count == 0 ) continue;

			foreach ( var kv in el.Events )
			{
				var name = kv.Key;
				var binding = kv.Value;
				if ( binding == null ) continue;

				// Closed-set check — element type must surface this event.
				if ( !SuiEventMatrix.Supports( el.Type, name ) )
				{
					r.Errors.Add( $"element '{el.Name ?? el.Id}' has event '{name}' but {el.Type} does not surface it" );
					continue;
				}

				switch ( binding.Mode )
				{
					case SuiEventMode.Code:
						if ( string.IsNullOrEmpty( binding.Handler ) )
							r.Errors.Add( $"event '{el.Name ?? el.Id}.{name}' in Code mode has empty Handler" );
						else if ( !IsLegalIdentifier( binding.Handler ) )
							r.Errors.Add( $"event '{el.Name ?? el.Id}.{name}' Handler '{binding.Handler}' is not a legal C# identifier" );
						break;
					case SuiEventMode.Doo:
						if ( string.IsNullOrEmpty( binding.DooPropertyName ) )
							r.Errors.Add( $"event '{el.Name ?? el.Id}.{name}' in Doo mode has empty DooPropertyName" );
						else if ( !IsLegalIdentifier( binding.DooPropertyName ) )
							r.Errors.Add( $"event '{el.Name ?? el.Id}.{name}' DooPropertyName '{binding.DooPropertyName}' is not a legal C# identifier" );
						break;
				}
			}
		}
	}

	private static void ValidateExposedElements( SuiDocument doc, Result r )
	{
		if ( doc?.Elements == null ) return;

		// Only shape checks here — collision detection moved to the unified
		// SuiNameConflictDetector pass so every paradigm shares one report.
		foreach ( var el in doc.Elements )
		{
			if ( el?.Flags == null || !el.Flags.ExposeAsVariable ) continue;
			var field = el.Name;
			if ( string.IsNullOrEmpty( field ) )
			{
				r.Errors.Add( $"element '{el.Id}' is exposed but has no Name to generate a field from" );
				continue;
			}
			if ( !IsLegalIdentifier( field ) )
				r.Errors.Add( $"exposed element '{field}' is not a legal C# identifier" );
		}
	}

	private static bool IsLegalIdentifier( string s )
	{
		if ( string.IsNullOrEmpty( s ) ) return false;
		if ( !char.IsLetter( s[0] ) && s[0] != '_' ) return false;
		for ( int i = 1; i < s.Length; i++ )
			if ( !char.IsLetterOrDigit( s[i] ) && s[i] != '_' ) return false;
		return true;
	}

	private static bool HasCycle( SuiElement start, Dictionary<string, SuiElement> byId )
	{
		var visited = new HashSet<string>();
		var current = start;
		while ( current != null && !string.IsNullOrEmpty( current.ParentId ) )
		{
			if ( !visited.Add( current.Id ) ) return true;
			if ( !byId.TryGetValue( current.ParentId, out var parent ) ) return false;
			current = parent;
		}
		return false;
	}

	// ---------- V1.5 — Variables (PRD 18 § 3.6) ----------

	/// <summary>
	/// Names that would collide with a member the generator already emits on the
	/// PanelComponent class, plus the most common C# keywords. PRD 18 § 3.9.
	/// </summary>
	private static readonly HashSet<string> ReservedVariableNames = new()
	{
		"Components", "GameObject", "Scene", "Network", "Tags", "Enabled", "Owner",
		"ScreenPanel", "Panel", "BuildHash", "BuildRenderHash",
		"OnAwake", "OnEnabled", "OnDisabled", "OnUpdate", "OnFixedUpdate", "OnDestroy",
		// common C# keywords most likely to be typed as a Variable name
		"class", "struct", "int", "float", "double", "long", "bool", "string",
		"void", "object", "null", "true", "false", "new", "this", "base",
		"public", "private", "static", "return", "if", "else", "for", "foreach",
	};

	/// <summary>
	/// Validates the document's Variables (PRD 18 § 3.6). M0 scope covers the
	/// structural rules — Id presence + uniqueness, valid C# identifier names,
	/// case-sensitive name uniqueness, reserved-name rejection, non-empty type.
	/// The Type-match / Source-resolution rules (§ 3.6 rules 4–9) land with the
	/// binding system in M1.
	/// </summary>
	private static void ValidateVariables( SuiDocument doc, Result r )
	{
		if ( doc.Variables == null || doc.Variables.Count == 0 ) return;

		var seenIds = new HashSet<string>();
		var seenNames = new HashSet<string>(); // case-sensitive — PRD 18 § 3.6 rule 2

		foreach ( var v in doc.Variables )
		{
			if ( v == null )
			{
				r.Errors.Add( "null entry in Variables list" );
				continue;
			}

			if ( string.IsNullOrEmpty( v.Id ) )
				r.Errors.Add( $"variable '{v.Name}' has no id" );
			else if ( !seenIds.Add( v.Id ) )
				r.Errors.Add( $"duplicate variable id: {v.Id}" );

			if ( string.IsNullOrEmpty( v.Name ) )
			{
				r.Errors.Add( $"variable '{v.Id}' has no name" );
				continue;
			}

			if ( !IsValidCSharpIdentifier( v.Name ) )
				r.Errors.Add( $"variable '{v.Name}' is not a valid C# identifier" );
			else if ( ReservedVariableNames.Contains( v.Name ) )
				r.Errors.Add( $"variable name '{v.Name}' is reserved (collides with a generated member or C# keyword)" );

			if ( !seenNames.Add( v.Name ) )
				r.Errors.Add( $"duplicate variable name: '{v.Name}' (names are case-sensitive and must be unique within the document)" );

			if ( string.IsNullOrEmpty( v.Type ) )
				r.Errors.Add( $"variable '{v.Name}' has no type" );
		}
	}

	/// <summary>
	/// True if <paramref name="s"/> is a valid C# identifier — a letter or
	/// underscore followed by letters, digits, or underscores.
	/// </summary>
	public static bool IsValidCSharpIdentifier( string s )
	{
		if ( string.IsNullOrEmpty( s ) ) return false;
		if ( !(char.IsLetter( s[0] ) || s[0] == '_') ) return false;
		for ( int i = 1; i < s.Length; i++ )
		{
			if ( !(char.IsLetterOrDigit( s[i] ) || s[i] == '_') ) return false;
		}
		return true;
	}

	// ---------- V1.5 — Bindings (PRD 18 § 4.10) ----------

	/// <summary>
	/// Validates the <see cref="SuiBinding"/> entries across every element
	/// (PRD 18 § 4.10). M1-A scope covers the structural rules — binding Id
	/// uniqueness, non-empty Property, Source resolves to a real Variable, Mode
	/// valid for the (element type, property) pair, and the TwoWay "no converters"
	/// constraint. Deep converter-chain type composition (§ 4.10 rules 4–6) lands
	/// with the converter catalog in M1-C.
	/// </summary>
	private static void ValidateBindings( SuiDocument doc, Result r )
	{
		if ( doc.Elements == null ) return;

		// Variable ids available as binding sources.
		var variableIds = new HashSet<string>();
		if ( doc.Variables != null )
		{
			foreach ( var v in doc.Variables )
				if ( v != null && !string.IsNullOrEmpty( v.Id ) ) variableIds.Add( v.Id );
		}

		var seenBindingIds = new HashSet<string>();

		foreach ( var el in doc.Elements )
		{
			if ( el?.Bindings == null || el.Bindings.Count == 0 ) continue;

			foreach ( var b in el.Bindings )
			{
				if ( b == null )
				{
					r.Errors.Add( $"element '{el.Id}' has a null binding entry" );
					continue;
				}

				var where = $"binding on '{el.Id}'.{b.Property ?? "?"}";

				if ( string.IsNullOrEmpty( b.Id ) )
					r.Errors.Add( $"{where} has no id" );
				else if ( !seenBindingIds.Add( b.Id ) )
					r.Errors.Add( $"duplicate binding id: {b.Id}" );

				if ( string.IsNullOrEmpty( b.Property ) )
				{
					r.Errors.Add( $"binding '{b.Id}' on '{el.Id}' has no target property" );
					continue;
				}

				// Source must resolve to a Variable on this document.
				var sourceVarId = b.Source?.VariableId;
				if ( string.IsNullOrEmpty( sourceVarId ) )
					r.Errors.Add( $"{where} has no source Variable" );
				else if ( !variableIds.Contains( sourceVarId ) )
					r.Errors.Add( $"{where} references unknown Variable id '{sourceVarId}'" );

				// Mode must be valid for this (element type, property) pair.
				if ( !SuiBindingModeMatrix.IsBindable( el.Type, b.Property ) )
				{
					r.Errors.Add( $"{where}: property '{b.Property}' is not bindable on a {el.Type} element" );
				}
				else if ( !SuiBindingModeMatrix.IsModeAllowed( el.Type, b.Property, b.Mode ) )
				{
					r.Errors.Add( $"{where}: mode '{b.Mode}' is not allowed for {el.Type}.{b.Property}" );
				}

				// TwoWay bindings cannot carry a converter chain (PRD 18 § 4.6).
				if ( b.Mode == SuiBindingMode.TwoWay && b.Converters != null && b.Converters.Count > 0 )
					r.Errors.Add( $"{where}: TwoWay bindings cannot have converters" );

				// Each converter step must name a converter, and each converter
				// ref must resolve via the catalog. Cross-step type composition
				// (PRD 18 § 4.10 rules 4–6) follows.
				if ( b.Converters != null )
				{
					ValidateConverterChain( where, b, sourceVarId, doc, r );
				}
			}
		}
	}

	/// <summary>
	/// Validate a binding's converter chain — every step must reference a real
	/// converter, and the return type of step N must be compatible with the first
	/// input type of step N+1. The very first step's first input must accept the
	/// source Variable's type. Generic placeholders (<c>T</c>) match anything.
	/// </summary>
	private static void ValidateConverterChain( string where, SuiBinding b, string sourceVarId, SuiDocument doc, Result r )
	{
		SuiConverterMetadata prevMeta = null;
		string prevReturnType = null;

		// Source variable type feeds the first step's chain input.
		if ( !string.IsNullOrEmpty( sourceVarId ) )
		{
			var sourceVar = FindVariableById( doc, sourceVarId );
			if ( sourceVar != null )
				prevReturnType = NormalizeTypeName( sourceVar.Type );
		}

		for ( int i = 0; i < b.Converters.Count; i++ )
		{
			var step = b.Converters[i];
			if ( step == null )
			{
				r.Errors.Add( $"{where}: converter step {i} is null" );
				continue;
			}

			if ( string.IsNullOrEmpty( step.ConverterRef ) )
			{
				r.Errors.Add( $"{where}: converter step {i} has no ConverterRef" );
				prevMeta = null;
				prevReturnType = null;
				continue;
			}

			var meta = SuiConverterCatalog.Find( step.ConverterRef );
			if ( meta == null )
			{
				r.Errors.Add( $"{where}: converter step {i} references unknown converter '{step.ConverterRef}'" );
				prevMeta = null;
				prevReturnType = null;
				continue;
			}

			// Type compatibility: meta.Inputs[0] consumes the chain input
			// (either source variable for step 0, or previous step's return).
			if ( meta.Inputs != null && meta.Inputs.Length > 0 && prevReturnType != null )
			{
				var expected = NormalizeTypeName( meta.Inputs[0]?.Type );
				if ( !AreTypesCompatible( prevReturnType, expected ) )
				{
					var producer = i == 0
						? "source variable"
						: $"step #{i} ({prevMeta?.DisplayName ?? prevMeta?.Ref ?? "?"})";
					r.Errors.Add( $"{where}: step #{i + 1} ('{meta.DisplayName ?? meta.Ref}') expects input type '{expected}', but {producer} returns '{prevReturnType}'" );
				}
			}

			prevMeta = meta;
			prevReturnType = NormalizeTypeName( meta.ReturnType );
		}
	}

	private static SuiVariable FindVariableById( SuiDocument doc, string id )
	{
		if ( doc?.Variables == null || string.IsNullOrEmpty( id ) ) return null;
		foreach ( var v in doc.Variables )
			if ( v?.Id == id ) return v;
		return null;
	}

	/// <summary>
	/// Normalize a type name so SUI TypeRef ("float", "int", "string") and
	/// C# type names ("Single", "Int32", "String") compare equal. Anything
	/// unknown is returned as-is so user-defined types still match by name.
	/// </summary>
	private static string NormalizeTypeName( string t )
	{
		if ( string.IsNullOrEmpty( t ) ) return null;
		return t switch
		{
			"Single"  => "float",
			"Double"  => "double",
			"Int32"   => "int",
			"Int64"   => "long",
			"Boolean" => "bool",
			"String"  => "string",
			"Byte"    => "byte",
			"Char"    => "char",
			_         => t,
		};
	}

	/// <summary>
	/// True if a value of type <paramref name="produced"/> can flow into a
	/// parameter of type <paramref name="expected"/>. Identity, generic
	/// placeholder ('T'), and the narrow set of obvious numeric widenings
	/// (int→float, int→double, long→double, float→double) are allowed.
	/// </summary>
	private static bool AreTypesCompatible( string produced, string expected )
	{
		if ( string.IsNullOrEmpty( produced ) || string.IsNullOrEmpty( expected ) ) return true;
		if ( produced == expected ) return true;
		// Generic placeholders accept anything.
		if ( expected == "T" || produced == "T" ) return true;
		if ( expected == "object" || expected == "object[]" ) return true;

		// Numeric widenings are silent and unambiguous.
		bool Widen( string from, string to )
			=> (from == "int"   && (to == "float" || to == "double" || to == "long"))
			|| (from == "long"  && (to == "float" || to == "double"))
			|| (from == "float" && to == "double");

		return Widen( produced, expected );
	}

	// ---------- Sanitizers ----------

	/// <summary>
	/// Convert an arbitrary string into a CSS-safe class name.
	/// Lowercases, replaces invalid chars with hyphens, strips leading non-alpha.
	/// </summary>
	public static string SanitizeClassName( string raw )
	{
		if ( string.IsNullOrEmpty( raw ) ) return "_";
		var sb = new StringBuilder( raw.Length );
		foreach ( var ch in raw )
		{
			if ( char.IsLetterOrDigit( ch ) ) sb.Append( char.ToLowerInvariant( ch ) );
			else if ( ch == '-' || ch == '_' ) sb.Append( '-' );
			else if ( char.IsWhiteSpace( ch ) ) sb.Append( '-' );
			// drop everything else
		}
		var s = sb.ToString().Trim( '-' );
		// Ensure leading char is a letter (CSS allows starting with letter/underscore;
		// digits at the start would require escaping).
		if ( s.Length == 0 ) return "_";
		if ( !char.IsLetter( s[0] ) ) s = "x" + s;
		return s;
	}

	/// <summary>
	/// V1.5 M3.5 — element types that surface interactive states (Button + slots
	/// + icon). Other element types ignore <see cref="SuiElementProps.HoverStyle"/>
	/// etc., but having them present is a soft warning, not an error — keeps
	/// migration lossless.
	/// </summary>
	private static bool IsInteractiveType( SuiElementType type )
	{
		return type == SuiElementType.Button
			|| type == SuiElementType.InventorySlot
			|| type == SuiElementType.ItemIcon;
	}

	/// <summary>
	/// Validate interactive-state ranges on an element. Hard errors only on
	/// values that would corrupt the generated SCSS (negative duration, NaN);
	/// out-of-range Scale/Opacity get warnings.
	/// </summary>
	private static void ValidateInteractiveProps( SuiElement el, Result r )
	{
		var p = el.Props;
		var elName = el.Name ?? el.Id;

		if ( p.TransitionEnabled && p.TransitionDuration <= 0f )
			r.Errors.Add( $"element '{elName}' has TransitionEnabled but TransitionDuration={p.TransitionDuration} <= 0" );
		if ( p.TransitionDuration > 5f )
			r.Warnings.Add( $"element '{elName}' transitionDuration={p.TransitionDuration}s is unusually long" );

		// V1.5 M3.5 — Square/Round look correct only with equal width and
		// height. Don't enforce, just warn so the author sees the issue.
		if ( (p.ButtonShape == SuiButtonShape.Square || p.ButtonShape == SuiButtonShape.Round)
			&& el.Layout != null
			&& System.MathF.Abs( el.Layout.Width - el.Layout.Height ) > 0.5f )
		{
			r.Warnings.Add( $"element '{elName}' shape={p.ButtonShape} but width ({el.Layout.Width}) ≠ height ({el.Layout.Height})" );
		}

		ValidateStateStyle( elName, "HoverStyle", p.HoverStyle, r );
		ValidateStateStyle( elName, "PressedStyle", p.PressedStyle, r );
		ValidateStateStyle( elName, "DisabledStyle", p.DisabledStyle, r );
		ValidateStateStyle( elName, "FocusedStyle", p.FocusedStyle, r );
	}

	private static void ValidateStateStyle( string elName, string stateName, SuiInteractiveStateStyle style, Result r )
	{
		if ( style == null ) return;

		// Scale: 0 collapses the element, negatives flip it (probably not intended).
		// Allow > 0 only.
		if ( style.Scale <= 0f )
			r.Errors.Add( $"element '{elName}' {stateName}.Scale={style.Scale} must be > 0" );
		if ( style.Scale > 5f )
			r.Warnings.Add( $"element '{elName}' {stateName}.Scale={style.Scale} is unusually large" );

		// Opacity: -1 is the sentinel "no override"; 0..1 is the legal authored range.
		if ( style.Opacity >= 0f && style.Opacity > 1f )
			r.Errors.Add( $"element '{elName}' {stateName}.Opacity={style.Opacity} out of [0..1]" );

		// BorderWidth / BorderRadius: -1 sentinel = no override; otherwise >= 0.
		if ( style.BorderWidth >= 0f && style.BorderWidth > 64f )
			r.Warnings.Add( $"element '{elName}' {stateName}.BorderWidth={style.BorderWidth} is unusually thick" );
		if ( style.BorderRadius >= 0f && style.BorderRadius > 256f )
			r.Warnings.Add( $"element '{elName}' {stateName}.BorderRadius={style.BorderRadius} is unusually large" );
	}

	/// <summary>
	/// Convert an arbitrary string into a slug suitable for embedding in a documentId
	/// or filename — lowercase, alphanumerics + underscores only, max 24 chars.
	/// </summary>
	public static string SanitizeIdentifierSlug( string raw )
	{
		if ( string.IsNullOrEmpty( raw ) ) return "";
		var sb = new StringBuilder( raw.Length );
		foreach ( var ch in raw )
		{
			if ( char.IsLetterOrDigit( ch ) ) sb.Append( char.ToLowerInvariant( ch ) );
			else if ( sb.Length > 0 && sb[sb.Length - 1] != '_' ) sb.Append( '_' );
		}
		var s = sb.ToString().Trim( '_' );
		if ( s.Length > 24 ) s = s.Substring( 0, 24 ).TrimEnd( '_' );
		return s;
	}
}
