using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using SboxUiDesigner.Generation;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// In-engine self-test for the .sui document model. Pure logic — no scene/editor
/// dependency. Invoke <see cref="RunAll"/> from any Component, console command,
/// or the dedicated <see cref="SuiSelfTestRunner"/> component.
///
/// This isn't a "real" unit test framework (s&box doesn't ship a Test attribute
/// surface for game code), but it asserts the invariants documented in
/// PRD doc 05 + doc 14 and gives a one-liner pass/fail report.
/// </summary>
public static class SuiSelfTest
{
	public sealed class Report
	{
		public List<string> Passed { get; } = new();
		public List<string> Failed { get; } = new();
		public bool Ok => Failed.Count == 0;
		public string Summary => Ok
			? $"SuiSelfTest: {Passed.Count}/{Passed.Count} OK"
			: $"SuiSelfTest: {Failed.Count} failed of {Passed.Count + Failed.Count}";
	}

	public static Report RunAll()
	{
		var r = new Report();

		Run( r, nameof( CreateDefault_HasRootCanvas ), CreateDefault_HasRootCanvas );
		Run( r, nameof( CreateDefault_AssignsStableDocumentId ), CreateDefault_AssignsStableDocumentId );
		Run( r, nameof( ElementApplyTypeDefaults_Button_HasPointerEventsAll ), ElementApplyTypeDefaults_Button_HasPointerEventsAll );
		Run( r, nameof( ElementApplyTypeDefaults_VerticalBox_FlexColumn ), ElementApplyTypeDefaults_VerticalBox_FlexColumn );
		Run( r, nameof( ElementApplyTypeDefaults_Hotbar_FlexRowOneRow ), ElementApplyTypeDefaults_Hotbar_FlexRowOneRow );
		Run( r, nameof( Validator_RejectsNegativeWidth ), Validator_RejectsNegativeWidth );
		Run( r, nameof( Validator_RejectsDuplicateIds ), Validator_RejectsDuplicateIds );
		Run( r, nameof( Validator_RejectsCycle ), Validator_RejectsCycle );
		Run( r, nameof( Validator_RejectsOpacityOutOfRange ), Validator_RejectsOpacityOutOfRange );
		Run( r, nameof( Validator_AcceptsDefaultDocument ), Validator_AcceptsDefaultDocument );
		Run( r, nameof( Sanitizer_LowercasesAndHyphenates ), Sanitizer_LowercasesAndHyphenates );
		Run( r, nameof( Sanitizer_RemovesInvalidChars ), Sanitizer_RemovesInvalidChars );
		Run( r, nameof( Sanitizer_PrefixesLeadingDigit ), Sanitizer_PrefixesLeadingDigit );
		Run( r, nameof( IdentifierSlug_TrimsAndLowercases ), IdentifierSlug_TrimsAndLowercases );
		Run( r, nameof( JsonRoundTrip_PreservesShape ), JsonRoundTrip_PreservesShape );
		Run( r, nameof( Clone_DeepCopiesElements ), Clone_DeepCopiesElements );
		Run( r, nameof( Manifest_FindByPath_IsCaseInsensitive ), Manifest_FindByPath_IsCaseInsensitive );

		// M9 generator tests
		Run( r, nameof( Generator_DefaultDocument_ProducesTwoFiles ), Generator_DefaultDocument_ProducesTwoFiles );
		Run( r, nameof( Generator_RazorHasInheritsPanelComponent ), Generator_RazorHasInheritsPanelComponent );
		Run( r, nameof( Generator_RazorWrapsInRootElement ), Generator_RazorWrapsInRootElement );
		Run( r, nameof( Generator_BothFilesHaveParseableHeaders ), Generator_BothFilesHaveParseableHeaders );
		Run( r, nameof( Generator_HeaderDocumentIdMatchesSource ), Generator_HeaderDocumentIdMatchesSource );
		Run( r, nameof( Generator_NoAtExpressionInRazorBody ), Generator_NoAtExpressionInRazorBody );
		Run( r, nameof( Generator_NoCodeBlockOrBuildHash ), Generator_NoCodeBlockOrBuildHash );
		Run( r, nameof( Generator_NoForbiddenDisplayValues ), Generator_NoForbiddenDisplayValues );
		Run( r, nameof( Generator_NoPositionFixed ), Generator_NoPositionFixed );
		Run( r, nameof( Generator_NoCssGridProperties ), Generator_NoCssGridProperties );
		Run( r, nameof( Generator_OutputIsDeterministic ), Generator_OutputIsDeterministic );
		Run( r, nameof( Generator_TextElementEmitsLabel ), Generator_TextElementEmitsLabel );
		Run( r, nameof( Generator_TextEscapesAtSign ), Generator_TextEscapesAtSign );
		Run( r, nameof( Generator_ScssScopedUnderTypeName ), Generator_ScssScopedUnderTypeName );
		Run( r, nameof( Generator_GridGeneratesWrappedFlex ), Generator_GridGeneratesWrappedFlex );
		Run( r, nameof( AllowedPropertyList_RejectsDisplayGrid ), AllowedPropertyList_RejectsDisplayGrid );
		Run( r, nameof( AllowedPropertyList_RejectsPositionFixed ), AllowedPropertyList_RejectsPositionFixed );
		Run( r, nameof( HashUtility_IsStableAcrossCalls ), HashUtility_IsStableAcrossCalls );

		return r;
	}

	// ---------- Tests ----------

	private static void CreateDefault_HasRootCanvas()
	{
		var doc = SuiDocument.CreateDefault( "InventoryUI" );
		Assert( doc.Elements.Count == 1, "default doc should have exactly 1 element" );
		var root = doc.GetRoot();
		Assert( root != null, "default doc should have a root" );
		Assert( root.Type == SuiElementType.Canvas, "root should be a Canvas" );
		Assert( string.IsNullOrEmpty( root.ParentId ), "root.parentId should be null" );
	}

	private static void CreateDefault_AssignsStableDocumentId()
	{
		var doc = SuiDocument.CreateDefault( "InventoryUI" );
		Assert( !string.IsNullOrEmpty( doc.DocumentId ), "documentId should be assigned" );
		Assert( doc.DocumentId.StartsWith( "sui_" ), "documentId should start with sui_" );
		Assert( doc.DocumentId.Contains( "inventoryui" ), "documentId should contain a slug of the name" );
	}

	private static void ElementApplyTypeDefaults_Button_HasPointerEventsAll()
	{
		var el = new SuiElement { Type = SuiElementType.Button };
		el.ApplyTypeDefaults();
		Assert( el.Style.PointerEvents == SuiPointerEvents.All, "Button default pointer-events should be All" );
	}

	private static void ElementApplyTypeDefaults_VerticalBox_FlexColumn()
	{
		var el = new SuiElement { Type = SuiElementType.VerticalBox };
		el.ApplyTypeDefaults();
		Assert( el.Layout.Mode == SuiLayoutMode.Flex, "VerticalBox should default to Flex layout" );
		Assert( el.Layout.FlexDirection == SuiFlexDirection.Column, "VerticalBox should default to flex-column" );
	}

	private static void ElementApplyTypeDefaults_Hotbar_FlexRowOneRow()
	{
		var el = new SuiElement { Type = SuiElementType.Hotbar };
		el.ApplyTypeDefaults();
		Assert( el.Layout.Mode == SuiLayoutMode.Flex, "Hotbar should default to Flex layout" );
		Assert( el.Layout.FlexDirection == SuiFlexDirection.Row, "Hotbar should default to flex-row" );
		Assert( el.Layout.FlexWrap == SuiFlexWrap.NoWrap, "Hotbar should default to no-wrap" );
		Assert( el.Props.Rows == 1, "Hotbar should default to 1 row" );
	}

	private static void Validator_RejectsNegativeWidth()
	{
		var doc = SuiDocument.CreateDefault( "X" );
		doc.GetRoot().Layout.Width = -10f;
		var r = SuiDocumentValidator.Validate( doc );
		Assert( !r.IsValid, "validator should reject negative width" );
	}

	private static void Validator_RejectsDuplicateIds()
	{
		var doc = SuiDocument.CreateDefault( "X" );
		var root = doc.GetRoot();
		var dup = new SuiElement { Id = root.Id, Name = "Dup", Type = SuiElementType.Panel, ParentId = root.Id };
		doc.Elements.Add( dup );
		var r = SuiDocumentValidator.Validate( doc );
		Assert( !r.IsValid, "validator should reject duplicate ids" );
	}

	private static void Validator_RejectsCycle()
	{
		var doc = SuiDocument.CreateDefault( "X" );
		var root = doc.GetRoot();
		var a = new SuiElement { Id = "a", Name = "A", Type = SuiElementType.Panel, ParentId = "b" };
		var b = new SuiElement { Id = "b", Name = "B", Type = SuiElementType.Panel, ParentId = "a" };
		doc.Elements.Add( a );
		doc.Elements.Add( b );
		var r = SuiDocumentValidator.Validate( doc );
		Assert( !r.IsValid, "validator should reject a parent-id cycle" );
	}

	private static void Validator_RejectsOpacityOutOfRange()
	{
		var doc = SuiDocument.CreateDefault( "X" );
		doc.GetRoot().Style.Opacity = 1.5f;
		var r = SuiDocumentValidator.Validate( doc );
		Assert( !r.IsValid, "validator should reject opacity > 1" );
	}

	private static void Validator_AcceptsDefaultDocument()
	{
		var doc = SuiDocument.CreateDefault( "Hud" );
		var r = SuiDocumentValidator.Validate( doc );
		AssertEq( r.Errors.Count, 0, "default document should validate clean (no errors): " + string.Join( "; ", r.Errors ) );
	}

	private static void Sanitizer_LowercasesAndHyphenates()
	{
		AssertEq( SuiDocumentValidator.SanitizeClassName( "Inventory Panel" ), "inventory-panel", "spaces -> hyphens, lowercased" );
	}

	private static void Sanitizer_RemovesInvalidChars()
	{
		AssertEq( SuiDocumentValidator.SanitizeClassName( "Slot#1!" ), "slot1", "drops # and !" );
	}

	private static void Sanitizer_PrefixesLeadingDigit()
	{
		AssertEq( SuiDocumentValidator.SanitizeClassName( "1stSlot" ), "x1stslot", "leading digit prefixed with x" );
	}

	private static void IdentifierSlug_TrimsAndLowercases()
	{
		AssertEq( SuiDocumentValidator.SanitizeIdentifierSlug( "Inventory UI" ), "inventory_ui", "spaces become _" );
		AssertEq( SuiDocumentValidator.SanitizeIdentifierSlug( "  Hud!!  " ), "hud", "trims junk" );
	}

	private static void JsonRoundTrip_PreservesShape()
	{
		var doc = SuiDocument.CreateDefault( "RoundTrip" );
		var json = JsonSerializer.Serialize( doc );
		var back = JsonSerializer.Deserialize<SuiDocument>( json );
		Assert( back != null, "deserialize should not return null" );
		AssertEq( back.SchemaVersion, doc.SchemaVersion, "schemaVersion preserved" );
		AssertEq( back.Name, doc.Name, "name preserved" );
		AssertEq( back.DocumentId, doc.DocumentId, "documentId preserved" );
		AssertEq( back.Elements.Count, doc.Elements.Count, "element count preserved" );
		AssertEq( back.GetRoot().Type, SuiElementType.Canvas, "root type preserved" );
	}

	private static void Clone_DeepCopiesElements()
	{
		var doc = SuiDocument.CreateDefault( "Clone" );
		var clone = doc.Clone();
		clone.GetRoot().Style.BackgroundColor = "#ff00ff";
		Assert( doc.GetRoot().Style.BackgroundColor != "#ff00ff", "clone modification must not bleed into original" );
	}

	private static void Manifest_FindByPath_IsCaseInsensitive()
	{
		var m = new SuiGeneratedFileManifest();
		m.GeneratedFiles.Add( new SuiGeneratedFileEntry { Path = "Code/UI/Foo.razor", Kind = SuiGeneratedFileKind.Razor } );
		Assert( m.FindByPath( "code/ui/foo.razor" ) != null, "manifest lookup should be case-insensitive" );
		Assert( m.FindByPath( "code/ui/missing.razor" ) == null, "missing path should return null" );
	}

	// ---------- M9 generator tests ----------

	private static SuiGenerationResult RunGenerator( SuiDocument doc, string outputFolder = "test_output" )
	{
		return SuiGenerationPipeline.Run( new SuiGenerationContext
		{
			Document = doc,
			Mode = SuiGenerationMode.Final,
			OutputFolder = outputFolder,
		} );
	}

	private static SuiDocument BuildSampleDoc()
	{
		var doc = SuiDocument.CreateDefault( "Sample" );
		var root = doc.GetRoot();

		// Panel as direct child of root
		var panel = new SuiElement
		{
			Id = "el_panel1", Name = "Inventory", Type = SuiElementType.Panel, ParentId = root.Id,
		};
		panel.ApplyTypeDefaults();
		panel.Style.ClassName = "inventory-panel";
		panel.Style.BackgroundColor = "#151515cc";
		panel.Style.BorderRadius = 16f;
		panel.Layout.Width = 520; panel.Layout.Height = 760;
		root.Children.Add( panel.Id );
		doc.Elements.Add( panel );

		// Text inside the panel
		var title = new SuiElement
		{
			Id = "el_title", Name = "Title", Type = SuiElementType.Text, ParentId = panel.Id,
		};
		title.ApplyTypeDefaults();
		title.Style.ClassName = "title-text";
		title.Props.Text = "Inventory";
		title.Props.FontSize = 32f;
		title.Props.Color = "#ffffff";
		panel.Children.Add( title.Id );
		doc.Elements.Add( title );

		return doc;
	}

	private static void Generator_DefaultDocument_ProducesTwoFiles()
	{
		var doc = BuildSampleDoc();
		var r = RunGenerator( doc );
		Assert( r.Ok, "generator should succeed: " + string.Join( "; ", r.Errors ) );
		AssertEq( r.Files.Count, 2, "expected 2 files (razor + scss)" );
		Assert( r.FindByKind( SuiGeneratedFileKind.Razor ) != null, "razor file present" );
		Assert( r.FindByKind( SuiGeneratedFileKind.Scss ) != null, "scss file present" );
	}

	private static void Generator_RazorHasInheritsPanelComponent()
	{
		var r = RunGenerator( BuildSampleDoc() );
		var razor = r.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";
		Assert( razor.Contains( "@inherits PanelComponent" ),
			"razor must @inherits PanelComponent" );
	}

	private static void Generator_RazorWrapsInRootElement()
	{
		var r = RunGenerator( BuildSampleDoc() );
		var razor = r.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";
		Assert( razor.Contains( "<root>" ) && razor.Contains( "</root>" ),
			"razor must wrap markup in <root>...</root>" );
	}

	private static void Generator_BothFilesHaveParseableHeaders()
	{
		var r = RunGenerator( BuildSampleDoc() );
		var razor = r.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";
		var scss = r.FindByKind( SuiGeneratedFileKind.Scss )?.Content ?? "";
		Assert( SuiHeaderEmitter.Parse( razor ) != null, "razor header must parse" );
		Assert( SuiHeaderEmitter.Parse( scss ) != null, "scss header must parse" );
	}

	private static void Generator_HeaderDocumentIdMatchesSource()
	{
		var doc = BuildSampleDoc();
		var r = RunGenerator( doc );
		var razor = r.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";
		var parsed = SuiHeaderEmitter.Parse( razor );
		Assert( parsed != null, "header parse" );
		AssertEq( parsed.DocumentId, doc.DocumentId, "header DocumentId must match source" );
		Assert( parsed.MatchesDocument( doc ), "header.MatchesDocument should be true" );
	}

	private static void Generator_NoAtExpressionInRazorBody()
	{
		var r = RunGenerator( BuildSampleDoc() );
		var razor = r.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";

		// Strip header (@* ... *@) and the @using / @inherits / @namespace directives
		// before scanning. What's left must contain ZERO `@` characters — else the
		// MVP no-data-binding invariant is violated and BuildHash() must be added.
		var stripped = Regex.Replace( razor, @"@\*[\s\S]*?\*@", "" );
		stripped = Regex.Replace( stripped, @"^\s*@(using|inherits|namespace)[^\r\n]*", "", RegexOptions.Multiline );
		stripped = Regex.Replace( stripped, @"@code\s*\{[\s\S]*?\}", "" );
		Assert( !stripped.Contains( "@" ),
			"MVP markup must contain zero @expression — found `@` after stripping directives:\n" + stripped );
	}

	private static void Generator_NoCodeBlockOrBuildHash()
	{
		var r = RunGenerator( BuildSampleDoc() );
		var razor = r.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";
		Assert( !razor.Contains( "@code" ),
			"MVP must NOT emit an @code block (BuildHash override comes in V1.5)" );
		Assert( !razor.Contains( "BuildHash" ),
			"MVP must NOT emit a BuildHash override" );
	}

	private static void Generator_NoForbiddenDisplayValues()
	{
		var r = RunGenerator( BuildSampleDoc() );
		var scss = r.FindByKind( SuiGeneratedFileKind.Scss )?.Content ?? "";
		Assert( !Regex.IsMatch( scss, @"\bdisplay\s*:\s*grid\b" ), "scss must not emit display: grid" );
		Assert( !Regex.IsMatch( scss, @"\bdisplay\s*:\s*block\b" ), "scss must not emit display: block" );
		Assert( !Regex.IsMatch( scss, @"\bdisplay\s*:\s*contents\b" ), "scss must not emit display: contents" );
		Assert( !Regex.IsMatch( scss, @"\bdisplay\s*:\s*inline" ), "scss must not emit display: inline*" );
	}

	private static void Generator_NoPositionFixed()
	{
		var r = RunGenerator( BuildSampleDoc() );
		var scss = r.FindByKind( SuiGeneratedFileKind.Scss )?.Content ?? "";
		Assert( !Regex.IsMatch( scss, @"\bposition\s*:\s*fixed\b" ), "scss must not emit position: fixed" );
		Assert( !Regex.IsMatch( scss, @"\bposition\s*:\s*sticky\b" ), "scss must not emit position: sticky" );
	}

	private static void Generator_NoCssGridProperties()
	{
		var r = RunGenerator( BuildSampleDoc() );
		var scss = r.FindByKind( SuiGeneratedFileKind.Scss )?.Content ?? "";
		Assert( !scss.Contains( "grid-template" ), "scss must not contain grid-template-*" );
		Assert( !scss.Contains( "grid-area" ), "scss must not contain grid-area" );
		Assert( !scss.Contains( "grid-column:" ), "scss must not contain grid-column:" );
		Assert( !scss.Contains( "grid-row:" ), "scss must not contain grid-row:" );
	}

	private static void Generator_OutputIsDeterministic()
	{
		var doc = BuildSampleDoc();
		var r1 = RunGenerator( doc.Clone() );
		var r2 = RunGenerator( doc.Clone() );

		var razor1 = r1.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";
		var razor2 = r2.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";
		AssertEq( razor1, razor2, "razor output must be deterministic for the same document" );

		var scss1 = r1.FindByKind( SuiGeneratedFileKind.Scss )?.Content ?? "";
		var scss2 = r2.FindByKind( SuiGeneratedFileKind.Scss )?.Content ?? "";
		AssertEq( scss1, scss2, "scss output must be deterministic for the same document" );

		AssertEq(
			r1.FindByKind( SuiGeneratedFileKind.Razor ).Sha256,
			r2.FindByKind( SuiGeneratedFileKind.Razor ).Sha256,
			"razor sha-256 must be deterministic" );
	}

	private static void Generator_TextElementEmitsLabel()
	{
		var r = RunGenerator( BuildSampleDoc() );
		var razor = r.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";
		Assert( razor.Contains( "<label class=\"title-text\">Inventory</label>" ),
			"text element should emit <label> with class + content" );
	}

	private static void Generator_TextEscapesAtSign()
	{
		var doc = SuiDocument.CreateDefault( "Esc" );
		var root = doc.GetRoot();
		var t = new SuiElement
		{
			Id = "t1", Name = "T", Type = SuiElementType.Text, ParentId = root.Id,
		};
		t.ApplyTypeDefaults();
		t.Style.ClassName = "lab";
		t.Props.Text = "use @ to mention";
		root.Children.Add( t.Id );
		doc.Elements.Add( t );

		var r = RunGenerator( doc );
		var razor = r.FindByKind( SuiGeneratedFileKind.Razor )?.Content ?? "";
		Assert( !razor.Contains( "use @ to mention" ),
			"raw @ must be escaped (else Razor will treat it as a directive)" );
		Assert( razor.Contains( "use &#64; to mention" ),
			"@ should be escaped to &#64; in text content" );
	}

	private static void Generator_ScssScopedUnderTypeName()
	{
		var doc = SuiDocument.CreateDefault( "InventoryUI" );
		doc.Output.ClassName = "InventoryUI";
		var r = RunGenerator( doc );
		var scss = r.FindByKind( SuiGeneratedFileKind.Scss )?.Content ?? "";
		Assert( scss.Contains( "InventoryUI {" ),
			"scss must use the panel type name as the outermost selector" );
	}

	private static void Generator_GridGeneratesWrappedFlex()
	{
		var doc = SuiDocument.CreateDefault( "Hud" );
		var root = doc.GetRoot();
		var grid = new SuiElement
		{
			Id = "grid1", Name = "Slots", Type = SuiElementType.InventoryGrid, ParentId = root.Id,
		};
		grid.ApplyTypeDefaults();
		grid.Style.ClassName = "slot-grid";
		grid.Props.Columns = 6;
		grid.Props.Rows = 5;
		grid.Props.CellWidth = 72f;
		grid.Props.CellHeight = 72f;
		grid.Props.GridGap = 8f;
		root.Children.Add( grid.Id );
		doc.Elements.Add( grid );

		var r = RunGenerator( doc );
		var scss = r.FindByKind( SuiGeneratedFileKind.Scss )?.Content ?? "";

		Assert( scss.Contains( "display: flex" ), "grid should emit display: flex" );
		Assert( scss.Contains( "flex-wrap: wrap" ), "InventoryGrid should wrap" );
		Assert( !scss.Contains( "display: grid" ), "must NEVER emit display: grid" );
	}

	private static void AllowedPropertyList_RejectsDisplayGrid()
	{
		var err = SuiAllowedPropertyList.Validate( "display", "grid" );
		Assert( err != null, "Validate must reject display: grid" );
		Assert( err.Contains( "grid" ), "error should mention grid" );
	}

	private static void AllowedPropertyList_RejectsPositionFixed()
	{
		var err = SuiAllowedPropertyList.Validate( "position", "fixed" );
		Assert( err != null, "Validate must reject position: fixed" );
	}

	private static void HashUtility_IsStableAcrossCalls()
	{
		var a = SuiHashUtility.Sha256( "hello world" );
		var b = SuiHashUtility.Sha256( "hello world" );
		AssertEq( a, b, "sha-256 must be stable for the same input" );
		Assert( a.Length == 64, "sha-256 hex string must be 64 chars" );
	}

	// ---------- Helpers ----------

	private static void Run( Report r, string name, Action body )
	{
		try
		{
			body();
			r.Passed.Add( name );
		}
		catch ( Exception ex )
		{
			r.Failed.Add( $"{name}: {ex.Message}" );
		}
	}

	private static void Assert( bool cond, string message )
	{
		if ( !cond ) throw new InvalidOperationException( message );
	}

	private static void AssertEq<T>( T actual, T expected, string message )
	{
		if ( !EqualityComparer<T>.Default.Equals( actual, expected ) )
			throw new InvalidOperationException( $"{message} (expected: {expected}, got: {actual})" );
	}
}
