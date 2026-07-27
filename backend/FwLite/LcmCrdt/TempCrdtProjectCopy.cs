using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt;

/// <summary>
/// A throwaway copy of a CRDT project's db in its own scope (see
/// <see cref="CrdtProjectsService.OpenTemporaryProjectCopy"/>), so a dry run can apply and read back changes
/// without touching the original. Disposing closes the scope and deletes the temp files.
/// </summary>
public sealed class TempCrdtProjectCopy(IMiniLcmApi api, AsyncServiceScope scope, Func<Task> cleanup) : IAsyncDisposable
{
    public IMiniLcmApi Api { get; } = api;

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await cleanup();
    }
}
