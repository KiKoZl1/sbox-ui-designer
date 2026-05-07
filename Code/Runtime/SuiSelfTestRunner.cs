using Sandbox;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Drop this Component on any GameObject in a scene to run the .sui self-tests
/// on play. Logs the result to the engine console (Log.Info on pass, Log.Error
/// on fail). Disable in production scenes.
///
/// You can also invoke <see cref="SuiSelfTest.RunAll"/> directly from any other
/// component or console command — this wrapper exists as a convenience for users
/// who want to verify the model layer in-editor.
/// </summary>
public sealed class SuiSelfTestRunner : Component
{
	[Property] public bool RunOnAwake { get; set; } = true;

	[Property] public bool LogEachTest { get; set; } = false;

	protected override void OnAwake()
	{
		if ( RunOnAwake )
			Run();
	}

	[Button( "Run Now" )]
	public void Run()
	{
		var report = SuiSelfTest.RunAll();

		if ( LogEachTest )
		{
			foreach ( var p in report.Passed ) Log.Info( $"[Sui] PASS {p}" );
			foreach ( var f in report.Failed ) Log.Error( $"[Sui] FAIL {f}" );
		}

		if ( report.Ok )
			Log.Info( $"[Sui] {report.Summary}" );
		else
			Log.Error( $"[Sui] {report.Summary}" );
	}
}
