using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt;

/// <summary>
/// A disposable, throwaway copy of a CRDT project's database opened in its own service scope
/// (see <see cref="CrdtProjectsService.OpenProjectCopy"/>). Used by dry-run sync so changes can be
/// really applied and read back without touching the original project. Disposing closes the copy's
/// scope and deletes its temporary database files.
/// </summary>
public sealed class TempCrdtProjectCopy(IMiniLcmApi api, AsyncServiceScope scope, Func<Task> deleteFiles) : IAsyncDisposable
{
    public IMiniLcmApi Api { get; } = api;

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await deleteFiles();
    }
}
