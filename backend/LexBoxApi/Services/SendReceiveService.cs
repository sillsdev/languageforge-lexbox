using LexCore.Config;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexCore.Utils;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace LexBoxApi.Services;

public class SendReceiveService(IOptions<SendReceiveConfig> config) : ISendReceiveService
{
    private readonly ConcurrentWeakDictionary<string, AsyncReaderWriterLock> _projectLocks = new();
    private readonly ConcurrentWeakDictionary<string, SemaphoreSlim> _concurrencyLimiters = new();
    private readonly CancellationToken _cancelled = new(true);

    /// <summary>
    /// Notify that a project is being sent &amp; received. This will block migration from starting
    /// (per-project reader/writer lock) and additionally caps concurrent in-flight S/R for a single
    /// (user, project) so one client can't pile up requests against one repo.
    /// </summary>
    public async ValueTask<SendReceiveGate> BeginSendReceive(string projectCode, string userId)
    {
        var projectLock = _projectLocks.GetOrAdd(projectCode, _ => new AsyncReaderWriterLock());
        var result = projectLock.ReaderLockAsync(_cancelled);
        //task will be cancelled if the lock is already held (a migration holds the writer lock)
        if (result.AsTask().IsCanceled) return SendReceiveGate.MigrationInProgress;
        var migrationTicket = await result;

        // Additional per-(user, project) concurrency gate layered on top of the migration lock.
        // Zero-timeout TryAcquire (no queueing): if no slot is free, shed this request immediately.
        var maxConcurrent = config.Value.MaxConcurrentPerUserProject;
        var limiter = _concurrencyLimiters.GetOrAdd(ConcurrencyKey(userId, projectCode),
            _ => new SemaphoreSlim(maxConcurrent, maxConcurrent));
        if (!limiter.Wait(0))
        {
            migrationTicket.Dispose();
            return SendReceiveGate.ConcurrencyLimitReached;
        }

        return SendReceiveGate.Allow(new SendReceiveTicket(limiter, migrationTicket));
    }

    /// <summary>
    /// blocks S&amp;R until the returned IDisposable is disposed of, works across async calls.
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

    private static string ConcurrencyKey(string userId, string projectCode) => $"{userId}|{projectCode}";

    /// <summary>
    /// Releases both gates when the request completes: the concurrency slot first, then the
    /// migration reader lock. Holds a strong reference to the limiter so the weak dictionary can't
    /// collect it while a request is in flight.
    /// </summary>
    private sealed class SendReceiveTicket(SemaphoreSlim limiter, IDisposable migrationLock) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            limiter.Release();
            migrationLock.Dispose();
        }
    }
}
