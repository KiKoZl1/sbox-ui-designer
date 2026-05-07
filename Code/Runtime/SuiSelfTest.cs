using System;
using System.Collections.Generic;
using System.Text.Json;

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
