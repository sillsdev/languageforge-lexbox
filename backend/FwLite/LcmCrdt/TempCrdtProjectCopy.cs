using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt;

/// <summary>
/// A throwaway, disposable copy of a CRDT project's db
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
