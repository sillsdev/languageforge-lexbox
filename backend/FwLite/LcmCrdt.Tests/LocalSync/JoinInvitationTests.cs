using System.Net;
using FwLiteShared.LocalSync.Hosting;

namespace LcmCrdt.Tests.LocalSync;

public class JoinInvitationTests
{
    private static readonly JoinInvitation Invitation = new(
        new Uri("http://192.168.137.1:5139"),
        Guid.Parse("6d8f0a2c-4e5b-4c7d-9e1f-3a5c7e9b1d2f"),
        "Sena 3 & friends",
        new AccessPointCredentials("FWLite-K7QM", "pass word&=#?"));

    [Fact]
    public void AnInvitationSurvivesTheRoundTripThroughItsUri()
    {
        var parsed = JoinInvitation.TryParse(Invitation.ToUri());

        parsed.Should().BeEquivalentTo(Invitation);
    }

    [Fact]
    public void TheUriUsesTheFwliteSyncScheme()
    {
        Invitation.ToUri().Should().StartWith("fwlite-sync://join?");
    }

    [Fact]
    public void AnInvitationOnAnExistingNetwork_HasNoWifiCredentials()
    {
        var onExistingNetwork = Invitation with { Network = null };

        var parsed = JoinInvitation.TryParse(onExistingNetwork.ToUri());

        parsed!.Network.Should().BeNull();
        parsed.Host.Should().Be(Invitation.Host);
    }

    [Theory]
    [InlineData("https://example.org/join?host=http://1.2.3.4:5139")]
    [InlineData("fwlite-sync://join?project=6d8f0a2c-4e5b-4c7d-9e1f-3a5c7e9b1d2f")]
    [InlineData("fwlite-sync://join?host=not-a-uri&project=6d8f0a2c-4e5b-4c7d-9e1f-3a5c7e9b1d2f")]
    [InlineData("fwlite-sync://join?host=http%3A%2F%2F1.2.3.4%3A5139&project=nope")]
    [InlineData("not a uri at all")]
    public void SomethingThatIsNotAnInvitation_IsNotParsed(string text)
    {
        JoinInvitation.TryParse(text).Should().BeNull();
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("https://192.168.137.1:5139")]
    [InlineData("http://evil.example.org:5139")]
    [InlineData("http://203.0.113.9:5139")]
    [InlineData("http://0.0.0.0:5139")]
    [InlineData("http://224.0.0.251:5139")]
    public void AnInvitationToSomethingOtherThanAHostOnTheLocalNetwork_IsNotParsed(string host)
    {
        // only plain http to an IP address is something to join
        var text = $"fwlite-sync://join?host={Uri.EscapeDataString(host)}&project=6d8f0a2c-4e5b-4c7d-9e1f-3a5c7e9b1d2f";

        JoinInvitation.TryParse(text).Should().BeNull();
    }

    [Theory]
    [InlineData("http://10.42.0.1:5139")]
    [InlineData("http://172.20.1.5:5139")]
    [InlineData("http://192.168.137.1:5139")]
    [InlineData("http://169.254.3.4:5139")]
    public void AnInvitationToAPrivateAddress_IsParsed(string host)
    {
        var text = $"fwlite-sync://join?host={Uri.EscapeDataString(host)}&project=6d8f0a2c-4e5b-4c7d-9e1f-3a5c7e9b1d2f";

        JoinInvitation.TryParse(text).Should().NotBeNull();
    }

    [Fact]
    public void NewCredentials_AreRandomAndValidForWpa2()
    {
        var first = AccessPointCredentials.Generate();
        var second = AccessPointCredentials.Generate();

        first.Should().NotBe(second);
        first.Ssid.Should().StartWith("FWLite-");
        first.Passphrase.Length.Should().BeInRange(8, 63);
        IPAddress.TryParse(first.Ssid, out _).Should().BeFalse();
    }
}
