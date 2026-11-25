using System.Net;
using System.Text.Json.Serialization;

namespace FwLiteShared.LocalSync.Hosting;

/// <param name="HostAddress">This device's address on the network it created, which joiners connect to.</param>
public record AccessPointInfo(AccessPointCredentials Network, IPAddress HostAddress);

/// <summary>
/// A Wi-Fi network this device creates for a hosting session, so joiners need no router. Joiners see an ordinary
/// WPA2 network, so any phone or laptop can join it.
/// </summary>
public interface IAccessPoint
{
    /// <summary>Whether this platform can try to create a network at all; starting can still fail on this device.</summary>
    bool IsSupported { get; }

    /// <exception cref="AccessPointUnavailableException">The network couldn't be created (hardware, drivers or permissions).</exception>
    Task<AccessPointInfo> Start(AccessPointCredentials network, CancellationToken cancellationToken = default);

    /// <summary>Takes the network down. Safe to call when it isn't running.</summary>
    Task Stop();
}

/// <summary>Why hosting couldn't start, shown to the host in their language.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HostingProblem
{
    WifiOff,
    /// <summary>The adapter is busy with something else, such as Windows' Mobile hotspot.</summary>
    WifiBusy,
    CantCreateNetwork,
    /// <summary>The server couldn't listen on the chosen network.</summary>
    CantListen,
    /// <summary>Another window of the app is already sharing a project.</summary>
    AlreadyHosting,
}

/// <param name="message">For the log; the UI shows <paramref name="problem"/>.</param>
public class HostingException(HostingProblem problem, string message, Exception? inner = null) : Exception(message, inner)
{
    public HostingProblem Problem => problem;
}

/// <summary>The host can then share over a network the device is already on.</summary>
public class AccessPointUnavailableException(HostingProblem problem, string message, Exception? inner = null)
    : HostingException(problem, message, inner);

/// <summary>For platforms that can't create a network; hosts use a network they're already on.</summary>
internal sealed class NoAccessPoint : IAccessPoint
{
    public bool IsSupported => false;

    public Task<AccessPointInfo> Start(AccessPointCredentials network, CancellationToken cancellationToken = default) =>
        throw new AccessPointUnavailableException(HostingProblem.CantCreateNetwork, "This device can't create its own Wi-Fi network");

    public Task Stop() => Task.CompletedTask;
}
