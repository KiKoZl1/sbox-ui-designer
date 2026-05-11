using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Glue that runs validator → razor generator → scss generator → packs the
/// result. Pure logic; the file-system writer is a separate concern (M12).
///
/// Pipeline order:
///   1. Validate the document via SuiDocumentValidator (schema invariants).
///      Bail with errors if invalid.
///   2. Resolve class name + namespace from context (defaults from doc.Output).
///   3. Emit Razor markup.
///   4. Emit SCSS rules.
///   5. Pack into SuiGenerationResult with kind+path+content+sha256 per file.
///
/// The path strings inside the result are project-relative; M12 will resolve
/// them against the actual filesystem.
/// </summary>
public static class SuiGenerationPipeline
{
	public static SuiGenerationResult Run( SuiGenerationContext ctx )
	{
		var result = new SuiGenerationResult();
		var doc = ctx?.Document;
		if ( doc == null )
		{
			result.Errors.Add( "pipeline: context or document is null" );
			return result;
		}

		// Schema validation — abort if invalid (cycles, dup ids, missing root).
		var schemaReport = SuiDocumentValidator.Validate( doc );
		foreach ( var w in schemaReport.Errors ) result.Errors.Add( $"validator: {w}" );
		foreach ( var w in schemaReport.Warnings ) result.Warnings.Add( $"validator: {w}" );
		if ( !schemaReport.IsValid ) return result;

		// Resolve class/namespace defaults.
		var rawClass = !string.IsNullOrEmpty( ctx.ClassName ) ? ctx.ClassName
			: !string.IsNullOrEmpty( doc.Output?.ClassName ) ? doc.Output.ClassName
			: doc.Name;
		ctx.ClassName = SuiNameSanitizer.ToCSharpIdentifier( rawClass );
		ctx.Namespace = !string.IsNullOrEmpty( ctx.Namespace ) ? ctx.Namespace
			: !string.IsNullOrEmpty( doc.Output?.Namespace ) ? doc.Output.Namespace
			: "Game.UI";

		// Preview mode emits into a sentinel sub-namespace so the preview cache
		// type never collides with the final compile output. Without this, a
		// document that has been compiled to disk (final mode → namespace X)
		// AND has a live preview cache (Code/_sui_preview/.../*.razor → also
		// namespace X) yields TWO `partial class <Name>` declarations and
		// the engine errors out with CS0111 (duplicate render-tree members).
		if ( ctx.Mode == SuiGenerationMode.Preview && !ctx.Namespace.EndsWith( ".SuiPreview" ) )
		{
			ctx.Namespace = ctx.Namespace + ".SuiPreview";
		}

		var folder = (ctx.OutputFolder ?? "").Trim( '/', '\\', ' ' );
		string Combine( string filename ) => string.IsNullOrEmpty( folder ) ? filename : folder + "/" + filename;

		// Razor
		var razor = new SuiRazorGenerator().Generate( ctx, result );
		if ( !string.IsNullOrEmpty( razor ) )
			result.AddFile( SuiGeneratedFileKind.Razor, Combine( $"{ctx.ClassName}.razor" ), razor );

		// SCSS
		var scss = new SuiScssGenerator().Generate( ctx, result );
		if ( !string.IsNullOrEmpty( scss ) )
			result.AddFile( SuiGeneratedFileKind.Scss, Combine( $"{ctx.ClassName}.razor.scss" ), scss );

		return result;
	}
}
