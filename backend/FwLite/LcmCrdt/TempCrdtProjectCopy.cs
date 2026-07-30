using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt;

/// <summary>
/// A throwaway, disposable copy of a CRDT project's db
/// </summary>
public sealed class TempCrdtProjectCopy(CrdtMiniLcmApi api, AsyncServiceScope scope, string dbPath, Func<Task> cleanup) : IAsyncDisposable
{
    private bool _scopeClosed;
    private bool _keepFile;

    public CrdtMiniLcmApi Api { get; } = api;

    /// <summary>Resolve services bound to the copy rather than to the project it was copied from.</summary>
    public IServiceProvider Services => scope.ServiceProvider;

    public string DbPath { get; } = dbPath;

    /// <summary>
    /// Closes the copy's scope and hands the file over to the caller, which is how the sync staging area keeps a
    /// copy it is about to move into place. Disposal afterwards is a no-op; nothing may touch <see cref="Api"/>.
    /// </summary>
    public async Task CloseWithoutDeleting()
    {
        _keepFile = true;
        await CloseScope();
    }

    public async ValueTask DisposeAsync()
    {
        // finally so the temp files are deleted even if scope disposal throws.
        try
        {
            await CloseScope();
        }
        finally
        {
            if (!_keepFile) await cleanup();
        }
    }

    private async Task CloseScope()
    {
        if (_scopeClosed) return;
        _scopeClosed = true;
        await scope.DisposeAsync();
    }
}
