using Sandbox;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// GameResource backing for a .sui document. The Asset Browser sees this as
/// a "Sbox UI Document" under the UI category. Double-clicking it opens the
/// Sbox UI Designer window (registered via [EditorForAssetType("sui")] on
/// SuiDesignerWindow in the Editor assembly).
///
/// The asset wraps a single <see cref="SuiDocument"/>. The document is
/// [Property] for round-tripping but [Hide] so it never shows in the standard
/// inspector — users always edit through the visual designer.
///
/// Extension constraints: 's', 'u', 'i' — 3 lowercase chars, well under
/// the engine's ≤ 8 lowercase char limit.
/// </summary>
[AssetType( Name = "Sbox UI Document", Extension = "sui", Category = "UI" )]
public class SuiAsset : GameResource
{
	[Property, Hide]
	public SuiDocument Document { get; set; } = new();
}
