using System;
using System.Collections.Concurrent;
using System.IO;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Wraps a <see cref="FileSystemWatcher"/> over the project root, filtered to
/// <c>*.sui</c> (PRD 22 § 3.3.3). FileSystemWatcher events fire on background
/// threads, so this class only ENQUEUES raw changes (thread-safe via
/// <see cref="ConcurrentQueue{T}"/>); <c>SuiAssetRegistryService.PumpPendingChanges</c>
/// drains and applies them on the main thread (PRD 22 § 7A.5).
///
/// Editor-only — <c>System.IO</c> is not available to the sandboxed runtime
/// assembly, which is why the registry/service split exists.
/// </summary>
public sealed class SuiAssetRegistryWatcher : IDisposable
{
	public enum ChangeKind { Created, Changed, Deleted, Renamed }

	public readonly struct Change
	{
		public Change( ChangeKind kind, string fullPath, string oldFullPath = null )
		{
			Kind = kind;
			FullPath = fullPath;
			OldFullPath = oldFullPath;
		}

		public ChangeKind Kind { get; }
		public string FullPath { get; }
		/// <summary>For <see cref="ChangeKind.Renamed"/> — the pre-rename path. Null otherwise.</summary>
		public string OldFullPath { get; }
	}

	private readonly FileSystemWatcher _fsw;
	private readonly ConcurrentQueue<Change> _queue = new();

	public SuiAssetRegistryWatcher( string projectRoot )
	{
		_fsw = new FileSystemWatcher( projectRoot, "*.sui" )
		{
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
		};

		_fsw.Created += ( _, e ) => _queue.Enqueue( new Change( ChangeKind.Created, e.FullPath ) );
		_fsw.Changed += ( _, e ) => _queue.Enqueue( new Change( ChangeKind.Changed, e.FullPath ) );
		_fsw.Deleted += ( _, e ) => _queue.Enqueue( new Change( ChangeKind.Deleted, e.FullPath ) );
		_fsw.Renamed += ( _, e ) => _queue.Enqueue( new Change( ChangeKind.Renamed, e.FullPath, e.OldFullPath ) );

		_fsw.EnableRaisingEvents = true;
	}

	/// <summary>Dequeue the next pending change. Returns false when the queue is empty.</summary>
	public bool TryDequeue( out Change change ) => _queue.TryDequeue( out change );

	public void Dispose()
	{
		try
		{
			if ( _fsw != null )
			{
				_fsw.EnableRaisingEvents = false;
				_fsw.Dispose();
			}
		}
		catch { /* watcher teardown is best-effort */ }
	}
}
