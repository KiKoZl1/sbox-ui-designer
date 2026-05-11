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
		foreach ( var ev in Events ) clone.Events.Add( ev.Clone() );
		foreach ( var an in Animations ) clone.Animations.Add( an.Clone() );
		return clone;
	}
}
