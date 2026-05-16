using System;
using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Root .sui document. Source of truth for a single UI document — the visual
/// designer reads/writes this; the generator emits Razor/SCSS from it.
///
/// The full document is persisted via <see cref="SboxUiDesigner.Runtime.SuiAsset"/>
/// (a GameResource — see Code/Runtime/SuiAsset.cs).
/// </summary>
public sealed class SuiDocument
{
	// ---------- Identity ----------

	/// <summary>Schema version for migration. <see cref="SuiSchemaVersion.Current"/> at save time.</summary>
	public int SchemaVersion { get; set; } = SuiSchemaVersion.Current;

	/// <summary>Stable unique id for file ownership and bindings. Never changes on rename.</summary>
	public string DocumentId { get; set; }

	/// <summary>User-facing document name (matches the .sui asset filename without extension).</summary>
	public string Name { get; set; }

	public string CreatedWith { get; set; } = SuiSchemaVersion.CreatedWithTag;

	public string DesignerVersion { get; set; } = SuiSchemaVersion.DesignerVersion;

	// ---------- Settings & canvas ----------

	public SuiCanvasSettings Canvas { get; set; } = new();
	public SuiDocumentSettings Settings { get; set; } = new();

	// ---------- Tree ----------

	public List<SuiElement> Elements { get; set; } = new();

	// ---------- V1.5 ----------

	/// <summary>
	/// Typed UI-local state declared on this document (PRD 18 § 3). The generator
	/// emits one <c>[Property]</c> per Variable; UI element properties bind to these.
	/// Empty on V1 documents (added by the V1 → V2 migration).
	/// </summary>
	public List<SuiVariable> Variables { get; set; } = new();

	/// <summary>
	/// V1.5-M2-K — DEPRECATED. AcceptedProps merged into <see cref="SuiVariable.IsPublic"/>
	/// (DEVIATIONS D-005). Kept on the schema only so JSON deserialise still works
	/// for documents authored before the merge; <see cref="MigrateAcceptedPropsToPublicVariables"/>
	/// converts each entry into a public Variable on load and clears this list.
	/// Never written back to disk after migration runs.
	/// </summary>
	public List<SuiAcceptedProp> AcceptedProps { get; set; } = new();

	/// <summary>
	/// Design-time preview values for Variables and AcceptedProps (PRD 19 § 3.6).
	/// Read by the canvas renderer only; never emitted to generated code. Null
	/// when the user hasn't authored any overrides.
	/// </summary>
	public SuiPreviewData PreviewData { get; set; }

	// ---------- Reserved (V1.5+) ----------

	public List<SuiEventBinding> Events { get; set; } = new();
	public List<SuiAnimationData> Animations { get; set; } = new();

	/// <summary>Property bindings (target.Property → Source.Path) displayed in the Bindings tab.</summary>
	public List<SuiPropertyBinding> Bindings { get; set; } = new();

	// ---------- Output & manifest ----------

	public SuiOutputSettings Output { get; set; } = new();
	public SuiGeneratedFileManifest Manifest { get; set; } = new();

	// ---------- Lookup helpers ----------

	/// <summary>Returns the root element (parentId == null) or null if document is empty/invalid.</summary>
	public SuiElement GetRoot()
	{
		foreach ( var el in Elements )
		{
			if ( string.IsNullOrEmpty( el.ParentId ) )
				return el;
		}
		return null;
	}

	public SuiElement GetElement( string id )
	{
		if ( string.IsNullOrEmpty( id ) ) return null;
		foreach ( var el in Elements )
		{
			if ( el.Id == id ) return el;
		}
		return null;
	}

	/// <summary>
	/// V1.5-M2-K migration — fold each <see cref="SuiAcceptedProp"/> into a
	/// public <see cref="SuiVariable"/> (IsPublic = true). Called on load. Idempotent:
	/// safe to call multiple times; after the first call <see cref="AcceptedProps"/>
	/// is empty and the method is a no-op.
	///
	/// <para>Existing Variables whose <c>Source.Kind == FromAcceptedProp</c> are
	/// converted to Manual sources (the bridge alias is no longer needed because
	/// the public Variable IS the contract now).</para>
	///
	/// <para>Returns the count of AcceptedProps migrated, so callers can log or
	/// surface a one-time toast.</para>
	/// </summary>
	public int MigrateAcceptedPropsToPublicVariables()
	{
		if ( AcceptedProps == null || AcceptedProps.Count == 0 ) return 0;

		var migrated = 0;
		Variables ??= new List<SuiVariable>();

		foreach ( var prop in AcceptedProps )
		{
			if ( prop == null || string.IsNullOrEmpty( prop.Name ) ) continue;

			// Already mirrored? If a FromAcceptedProp Variable points at this
			// PropId, upgrade it in place rather than creating a duplicate.
			SuiVariable existingBridge = null;
			foreach ( var v in Variables )
			{
				if ( v?.Source?.Kind == SuiVariableSourceKind.FromAcceptedProp
					&& v.Source.PropId == prop.PropId )
				{
					existingBridge = v;
					break;
				}
			}

			if ( existingBridge != null )
			{
				existingBridge.IsPublic = true;
				existingBridge.Type = prop.Type ?? existingBridge.Type;
				existingBridge.Default = prop.Default?.DeepClone() ?? existingBridge.Default;
				existingBridge.Description = string.IsNullOrEmpty( existingBridge.Description ) ? prop.Description : existingBridge.Description;
				existingBridge.Group = string.IsNullOrEmpty( existingBridge.Group ) ? prop.Group : existingBridge.Group;
				existingBridge.Source = new SuiVariableSource { Kind = SuiVariableSourceKind.Manual };
			}
			else
			{
				Variables.Add( new SuiVariable
				{
					Id = SuiVariable.NewVariableId(),
					Name = prop.Name,
					Type = prop.Type ?? "string",
					Default = prop.Default?.DeepClone(),
					Description = prop.Description,
					Group = prop.Group,
					IsPublic = true,
					Source = new SuiVariableSource { Kind = SuiVariableSourceKind.Manual },
				} );
			}
			migrated++;
		}

		AcceptedProps.Clear();

		// Cleanup orphan FromAcceptedProp bridges that referenced PropIds NOT in
		// the migrated list (defensive; shouldn't happen in healthy docs).
		foreach ( var v in Variables )
		{
			if ( v?.Source?.Kind == SuiVariableSourceKind.FromAcceptedProp )
			{
				v.Source = new SuiVariableSource { Kind = SuiVariableSourceKind.Manual };
			}
		}

		return migrated;
	}

	/// <summary>
	/// Generate a stable id like "el_a3f9b21c" derived from a guid.
	/// IDs are short, readable, and cheap to compare.
	/// </summary>
	public static string NewElementId()
	{
		var g = Guid.NewGuid();
		return "el_" + g.ToString( "N" ).Substring( 0, 8 );
	}

	/// <summary>
	/// Generate a stable document id like "sui_inventoryui_8f31_a3b2c1d4" — slug-prefixed
	/// so manifest/log output is human-scannable, suffix from a fresh guid for uniqueness.
	/// </summary>
	public static string NewDocumentId( string nameHint )
	{
		var slug = SuiDocumentValidator.SanitizeIdentifierSlug( nameHint );
		var g = Guid.NewGuid().ToString( "N" ).Substring( 0, 8 );
		return string.IsNullOrEmpty( slug )
			? "sui_" + g
			: $"sui_{slug}_{g}";
	}

	/// <summary>
	/// Build a fresh document with a single Root canvas element. Used by the
	/// "New" asset flow so users always see a non-empty document on first open.
	/// </summary>
	public static SuiDocument CreateDefault( string documentName )
	{
		var doc = new SuiDocument
		{
			SchemaVersion = SuiSchemaVersion.Current,
			DocumentId = NewDocumentId( documentName ),
			Name = documentName,
			CreatedWith = SuiSchemaVersion.CreatedWithTag,
			DesignerVersion = SuiSchemaVersion.DesignerVersion,
		};

		var root = new SuiElement
		{
			Id = "root",
			Name = "Root",
			Type = SuiElementType.Canvas,
			ParentId = null,
		};
		root.Style.ClassName = "root";
		root.Layout.Mode = SuiLayoutMode.Absolute;
		root.Layout.Width = doc.Canvas.BaseWidth;
		root.Layout.Height = doc.Canvas.BaseHeight;
		doc.Elements.Add( root );

		doc.Output.ClassName = SuiDocumentValidator.SanitizeClassName( documentName );

		return doc;
	}

	public SuiDocument Clone()
	{
		var clone = new SuiDocument
		{
			SchemaVersion = SchemaVersion,
			DocumentId = DocumentId,
			Name = Name,
			CreatedWith = CreatedWith,
			DesignerVersion = DesignerVersion,
			Canvas = Canvas?.Clone() ?? new(),
			Settings = Settings?.Clone() ?? new(),
			Output = Output?.Clone() ?? new(),
			Manifest = Manifest?.Clone() ?? new(),
		};
		foreach ( var el in Elements ) clone.Elements.Add( el.Clone() );
		foreach ( var v in Variables ) clone.Variables.Add( v.Clone() );
		foreach ( var ap in AcceptedProps ) clone.AcceptedProps.Add( ap.Clone() );
		foreach ( var ev in Events ) clone.Events.Add( ev.Clone() );
		foreach ( var an in Animations ) clone.Animations.Add( an.Clone() );
		clone.PreviewData = PreviewData?.Clone();
		return clone;
	}
}
