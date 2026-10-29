namespace FwLiteShared.LocalSync.Hosting;

/// <summary>
/// What a device needs while it uses a local sync network. On Android: route the app over the Wi-Fi network (which
/// has no internet, so Android would otherwise send traffic over mobile data) and accept multicast for mDNS. Desktop
/// operating systems already route local addresses correctly.
/// </summary>
public interface ILocalNetworkAccess
{
    /// <summary>Holds access until the returned lease is disposed. Leases can overlap (browsing while joined).</summary>
    IDisposable Acquire();
}

internal sealed class NoLocalNetworkAccess : ILocalNetworkAccess
{
    public IDisposable Acquire() => new Lease();

    private sealed class Lease : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

/// <summary>Counts overlapping leases so the platform's setup runs once and is undone when the last one ends.</summary>
public abstract class RefCountedLocalNetworkAccess : ILocalNetworkAccess
{
    private readonly Lock _lock = new();
    private int _leases;

    public IDisposable Acquire()
    {
        lock (_lock)
        {
            if (_leases++ == 0) Start();
        }
        return new Lease(this);
    }

    protected abstract void Start();
    protected abstract void Stop();

    private void Release()
    {
        lock (_lock)
        {
            if (--_leases == 0) Stop();
        }
    }

    private sealed class Lease(RefCountedLocalNetworkAccess owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) owner.Release();
        }
    }
}
