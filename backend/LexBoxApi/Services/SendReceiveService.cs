using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexCore.Utils;
using Nito.AsyncEx;

namespace LexBoxApi.Services;

public class SendReceiveService : ISendReceiveService
{
    private readonly ConcurrentWeakDictionary<string, AsyncReaderWriterLock> _projectLocks = new();
    private readonly CancellationToken _cancelled = new(true);

    /// <summary>
    /// Notify that a project is being sent & received. This will block migration from starting. It will return null if a migration has started indicating that send receive should be blocked
    /// </summary>
    /// <returns>null if you shouldn't start, a disposable to dispose when you're done</returns>
    public async ValueTask<IDisposable?> BeginSendReceive(string projectCode)
    {
        var projectLock = _projectLocks.GetOrAdd(projectCode, _ => new AsyncReaderWriterLock());
        var result = projectLock.ReaderLockAsync(_cancelled);
        //task will be cancelled if the lock is already held
        if (result.AsTask().IsCanceled) return null;
        return await result;
    }

    /// <summary>
    /// blocks S&R until the returned IDisposable is disposed of, works across async calls.
    /// Note, not currently used anywhere
    /// </summary>
    /// <param name="projectCode"></param>
    /// <returns>null if the block failed</returns>
    public async Task<IDisposable?> BlockSendReceive(string projectCode)
    {
        var projectLock = _projectLocks.GetOrAdd(projectCode, _ => new AsyncReaderWriterLock());
        var result = projectLock.WriterLockAsync(_cancelled);
        if (result.AsTask().IsCanceled) return null;
        return await result;
    }
}
