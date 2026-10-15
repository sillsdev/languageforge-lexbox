using System.Net;
using System.Net.Sockets;
using FwLiteShared.KeepAwake;
using LcmCrdt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIL.Harmony.Config;

namespace FwLiteShared.LocalSync.Hosting;

public record HostingInfo(HostSession Session, int Port);

/// <summary>
/// Starts and stops sharing one project for local sync: the session, its HTTP server, and keeping the machine
/// awake for as long as it runs. At most one project is hosted at a time.
/// </summary>
public class LocalSyncHosting(
    IServiceProvider services,
    IKeepAwake keepAwake,
    IOptions<HarmonyConfig> harmonyConfig,
    ILogger<LocalSyncHosting> logger) : IAsyncDisposable
{
    /// <summary>Tried first, so a joiner typing an address by hand usually sees the same port; any free port otherwise.</summary>
    public const int DefaultPort = 5139;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private Running? _running;

    private sealed record Running(HostingInfo Info, LocalSyncHttpServer Server, TaskCompletionSource Ended, Task KeepAwake);

    public HostingInfo? Current => _running?.Info;

    /// <param name="bindTo">The network interface joiners reach the host on (the access point's address); all interfaces by default.</param>
    public async Task<HostingInfo> StartHosting(CrdtProject project, IPAddress? bindTo = null)
    {
        await _lock.WaitAsync();
        try
        {
            if (_running is not null) throw new InvalidOperationException("Already hosting a project; stop hosting it first");
            bindTo ??= IPAddress.Any;

            var session = new HostSession(await HostedProjectReader.Read(services, project), AppVersion.Version);
            var port = IsFree(bindTo, DefaultPort) ? DefaultPort : FreePort(bindTo);
            var server = new LocalSyncHttpServer(session, project, services,
                harmonyConfig.Value.MakeLcmCrdtExternalJsonOptions(), logger, bindTo, port);
            server.Start();

            var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var awake = keepAwake.RunAsync(new KeepAwakeWork($"Sharing {project.Data?.Name ?? project.Name} for local sync"),
                async () => await ended.Task);
            _running = new Running(new HostingInfo(session, port), server, ended, awake);
            logger.LogInformation("Hosting {Project} for local sync on {Address}:{Port}", project.Name, bindTo, port);
            return _running.Info;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Ends the session (revoking every joiner), stops the server and lets the machine sleep again.</summary>
    public async Task StopHosting()
    {
        await _lock.WaitAsync();
        try
        {
            if (_running is not { } running) return;
            _running = null;
            running.Info.Session.End();
            running.Server.Dispose();
            running.Ended.TrySetResult();
            await running.KeepAwake;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static bool IsFree(IPAddress address, int port)
    {
        try
        {
            using var listener = new TcpListener(address, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static int FreePort(IPAddress address)
    {
        using var listener = new TcpListener(address, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public async ValueTask DisposeAsync() => await StopHosting();
}
