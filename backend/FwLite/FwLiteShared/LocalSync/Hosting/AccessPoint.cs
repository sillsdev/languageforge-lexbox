using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

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

public record CommandResult(int ExitCode, string StandardOutput, string StandardError);

public interface ICommandRunner
{
    /// <exception cref="Win32Exception">The program isn't installed.</exception>
    Task<CommandResult> Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default);
}

internal sealed class ProcessCommandRunner : ICommandRunner
{
    public async Task<CommandResult> Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo) ?? throw new Win32Exception($"Couldn't start {fileName}");
        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var error = process.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // otherwise it carries on, e.g. creating a hotspot nobody will take down
            process.Kill(entireProcessTree: true);
            throw;
        }
        return new CommandResult(process.ExitCode, await output, await error);
    }
}

/// <summary>
/// A hotspot made with NetworkManager: <c>nmcli device wifi hotspot</c> uses AP mode with <c>ipv4.method shared</c>,
/// so NetworkManager hands out addresses with no upstream connection. On most laptops the adapter can't be a hotspot
/// and stay connected to another network at the same time, so the current Wi-Fi connection drops while hosting.
/// </summary>
public sealed class LinuxAccessPoint(ICommandRunner commands, ILogger<LinuxAccessPoint> logger) : IAccessPoint
{
    private const string ConnectionName = "fwlite-sync";
    private string? _device;

    public TimeSpan AddressPollInterval { get; init; } = TimeSpan.FromMilliseconds(250);

    public bool IsSupported => true;

    public async Task<AccessPointInfo> Start(AccessPointCredentials network, CancellationToken cancellationToken = default)
    {
        try
        {
            var device = await WifiDevice(cancellationToken);
            // a session that ended with the app crashing can leave its hotspot behind
            await commands.Run("nmcli", ["connection", "delete", ConnectionName], cancellationToken);
            var hotspot = await commands.Run("nmcli",
                ["device", "wifi", "hotspot", "ifname", device, "con-name", ConnectionName, "ssid", network.Ssid, "password", network.Passphrase],
                cancellationToken);
            if (hotspot.ExitCode != 0)
                throw new AccessPointUnavailableException(HostingProblem.CantCreateNetwork, $"Couldn't create the Wi-Fi network: {Message(hotspot)}");
            _device = device;

            IPAddress? address;
            try
            {
                address = await WaitForAddress(device, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await Stop();
                throw;
            }
            if (address is null)
            {
                await Stop();
                throw new AccessPointUnavailableException(HostingProblem.CantCreateNetwork, "The Wi-Fi network was created but never got an address");
            }
            return new AccessPointInfo(network, address);
        }
        catch (Win32Exception e)
        {
            throw new AccessPointUnavailableException(HostingProblem.CantCreateNetwork, "NetworkManager (nmcli) isn't available on this computer", e);
        }
    }

    public async Task Stop()
    {
        if (_device is null) return;
        _device = null;
        try
        {
            await commands.Run("nmcli", ["connection", "down", ConnectionName]);
            await commands.Run("nmcli", ["connection", "delete", ConnectionName]);
        }
        catch (Win32Exception e)
        {
            logger.LogWarning(e, "Couldn't take down the local sync Wi-Fi network");
        }
    }

    private async Task<string> WifiDevice(CancellationToken cancellationToken)
    {
        var devices = await commands.Run("nmcli", ["-t", "-f", "DEVICE,TYPE", "device"], cancellationToken);
        var wifi = devices.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(':'))
            .FirstOrDefault(fields => fields is [_, "wifi"])?[0];
        return wifi ?? throw new AccessPointUnavailableException(HostingProblem.CantCreateNetwork, "This computer has no Wi-Fi adapter NetworkManager can use");
    }

    private async Task<IPAddress?> WaitForAddress(string device, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var result = await commands.Run("nmcli", ["-g", "IP4.ADDRESS", "device", "show", device], cancellationToken);
            var first = result.StandardOutput.Split(['\n', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (first is not null && IPAddress.TryParse(first.Split('/')[0], out var address)) return address;
            await Task.Delay(AddressPollInterval, cancellationToken);
        }
        return null;
    }

    private static string Message(CommandResult result) =>
        (string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError).Trim();
}
