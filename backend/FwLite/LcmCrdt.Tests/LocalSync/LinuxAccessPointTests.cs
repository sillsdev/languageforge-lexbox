using System.Net;
using FwLiteShared.LocalSync.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace LcmCrdt.Tests.LocalSync;

public class LinuxAccessPointTests
{
    private static readonly AccessPointCredentials Network = new("FWLite-K7QM", "ABCDEFGH2345");

    /// <summary>Plays back canned nmcli output and records what was run.</summary>
    private class FakeNmcli : ICommandRunner
    {
        public List<string> Ran { get; } = [];
        public Dictionary<string, CommandResult> Replies { get; } = [];

        public Task<CommandResult> Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
        {
            var command = string.Join(' ', [fileName, .. arguments]);
            Ran.Add(command);
            var reply = Replies.FirstOrDefault(r => command.StartsWith(r.Key, StringComparison.Ordinal)).Value;
            return Task.FromResult(reply ?? new CommandResult(0, "", ""));
        }
    }

    private static FakeNmcli WorkingNmcli()
    {
        var nmcli = new FakeNmcli();
        nmcli.Replies["nmcli -t -f DEVICE,TYPE device"] = new(0, "lo:loopback\nenp0s31f6:ethernet\nwlp2s0:wifi\np2p-dev-wlp2s0:wifi-p2p\n", "");
        nmcli.Replies["nmcli -g IP4.ADDRESS device show wlp2s0"] = new(0, "10.42.0.1/24\n", "");
        return nmcli;
    }

    private static LinuxAccessPoint AccessPoint(FakeNmcli nmcli) =>
        new(nmcli, NullLogger<LinuxAccessPoint>.Instance) { AddressPollInterval = TimeSpan.Zero };

    [Fact]
    public async Task StartingCreatesAHotspotOnTheWifiAdapter()
    {
        var nmcli = WorkingNmcli();

        var info = await AccessPoint(nmcli).Start(Network);

        nmcli.Ran.Should().Contain("nmcli device wifi hotspot ifname wlp2s0 con-name fwlite-sync ssid FWLite-K7QM password ABCDEFGH2345");
        info.HostAddress.Should().Be(IPAddress.Parse("10.42.0.1"));
        info.Network.Should().Be(Network);
    }

    [Fact]
    public async Task ALeftoverConnectionFromAnEarlierSession_IsRemovedFirst()
    {
        var nmcli = WorkingNmcli();

        await AccessPoint(nmcli).Start(Network);

        nmcli.Ran.IndexOf("nmcli connection delete fwlite-sync").Should()
            .BeLessThan(nmcli.Ran.FindIndex(c => c.StartsWith("nmcli device wifi hotspot")));
    }

    [Fact]
    public async Task StoppingTakesTheHotspotDownAndDeletesIt()
    {
        var nmcli = WorkingNmcli();
        var accessPoint = AccessPoint(nmcli);
        await accessPoint.Start(Network);
        nmcli.Ran.Clear();

        await accessPoint.Stop();

        nmcli.Ran.Should().Equal("nmcli connection down fwlite-sync", "nmcli connection delete fwlite-sync");
    }

    [Fact]
    public async Task WithoutAWifiAdapter_StartingFailsWithAReason()
    {
        var nmcli = new FakeNmcli();
        nmcli.Replies["nmcli -t -f DEVICE,TYPE device"] = new(0, "lo:loopback\nenp0s31f6:ethernet\n", "");

        var act = () => AccessPoint(nmcli).Start(Network);

        (await act.Should().ThrowAsync<AccessPointUnavailableException>()).Which.Message.Should().Contain("Wi-Fi");
    }

    [Fact]
    public async Task WhenNmcliRefuses_StartingFailsWithItsMessage()
    {
        // e.g. the adapter can't do AP mode, or polkit refused
        var nmcli = WorkingNmcli();
        nmcli.Replies["nmcli device wifi hotspot"] = new(4, "", "Error: Device 'wlp2s0' does not support AP mode.");

        var act = () => AccessPoint(nmcli).Start(Network);

        (await act.Should().ThrowAsync<AccessPointUnavailableException>()).Which.Message.Should().Contain("does not support AP mode");
    }

    [Fact]
    public async Task WithoutNmcli_StartingFailsWithAReason()
    {
        var accessPoint = new LinuxAccessPoint(new MissingProgram(), NullLogger<LinuxAccessPoint>.Instance);

        var act = () => accessPoint.Start(Network);

        await act.Should().ThrowAsync<AccessPointUnavailableException>();
    }

    [Fact]
    public async Task IfTheHotspotNeverGetsAnAddress_ItIsTakenDownAndStartingFails()
    {
        var nmcli = WorkingNmcli();
        nmcli.Replies["nmcli -g IP4.ADDRESS device show wlp2s0"] = new(0, "\n", "");

        var act = () => AccessPoint(nmcli).Start(Network);

        await act.Should().ThrowAsync<AccessPointUnavailableException>();
        nmcli.Ran.Should().Contain("nmcli connection delete fwlite-sync");
    }

    private class MissingProgram : ICommandRunner
    {
        public Task<CommandResult> Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default) =>
            throw new System.ComponentModel.Win32Exception(2, "No such file or directory");
    }

    [Fact]
    public async Task CancellingWhileWaitingForAnAddress_TakesTheHotspotDown()
    {
        var nmcli = WorkingNmcli();
        nmcli.Replies["nmcli -g IP4.ADDRESS device show wlp2s0"] = new(0, "", "");
        using var cancel = new CancellationTokenSource();
        var accessPoint = new LinuxAccessPoint(nmcli, NullLogger<LinuxAccessPoint>.Instance) { AddressPollInterval = TimeSpan.FromMilliseconds(50) };

        var start = accessPoint.Start(Network, cancel.Token);
        await cancel.CancelAsync();

        await start.Invoking(async s => await s).Should().ThrowAsync<OperationCanceledException>();
        nmcli.Ran.Should().Contain("nmcli connection down fwlite-sync");
    }
}
