using System.Net.Sockets;

namespace FwLiteShared.LocalSync.Hosting;

/// <summary>
/// What a device needs while it uses a local sync network. On Android the host's Wi-Fi network usually has no
/// internet, so Android sends traffic over mobile data instead: local sync's sockets are bound to the Wi-Fi network,
/// and multicast (mDNS) is let in. Desktop operating systems already route local addresses correctly.
/// </summary>
public interface ILocalNetworkAccess
{
    /// <summary>Holds access until the returned lease is disposed. Leases can overlap (browsing while joined).</summary>
    IDisposable Acquire();

    /// <summary>
    /// Like <see cref="Acquire"/>, and also routes all of the app's traffic over the local network while held, because
    /// the mDNS library's sockets can't be bound one by one. Lexbox may be out of reach meanwhile, so hold it briefly.
    /// </summary>
    IDisposable AcquireForDiscovery();

    /// <summary>Binds a socket that hasn't connected yet to the local network, if there is one.</summary>
    void Bind(Socket socket);
}

internal sealed class NoLocalNetworkAccess : ILocalNetworkAccess
{
    public IDisposable Acquire() => new Lease();
    public IDisposable AcquireForDiscovery() => new Lease();

    public void Bind(Socket socket)
    {
    }

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
    private int _discoveryLeases;

    public IDisposable Acquire() => Take(forDiscovery: false);
    public IDisposable AcquireForDiscovery() => Take(forDiscovery: true);
    public abstract void Bind(Socket socket);

    protected abstract void Start();
    protected abstract void Stop();
    protected abstract void RouteApp(bool overLocalNetwork);

    private Lease Take(bool forDiscovery)
    {
        lock (_lock)
        {
            if (_leases == 0) Start();
            _leases++;
            if (forDiscovery && _discoveryLeases++ == 0)
            {
                try
                {
                    RouteApp(true);
                }
                catch
                {
                    _discoveryLeases--;
                    Release(forDiscovery: false);
                    throw;
                }
            }
        }
        return new Lease(this, forDiscovery);
    }

    private void Release(bool forDiscovery)
    {
        lock (_lock)
        {
            try
            {
                if (forDiscovery && --_discoveryLeases == 0) RouteApp(false);
            }
            finally
            {
                if (--_leases == 0) Stop();
            }
        }
    }

    private sealed class Lease(RefCountedLocalNetworkAccess owner, bool forDiscovery) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) owner.Release(forDiscovery);
        }
    }
}
