using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using FwLiteShared;
using FwLiteShared.AppUpdate;
using FwLiteShared.Events;
using LexCore.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FwLiteShared.Tests.AppUpdate;

public class UpdateCheckerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IPlatformUpdateService> _platformUpdateServiceMock = new();

    // Empty preferences => never checked => the shared throttle allows a check. This lets the tests below
    // isolate ShouldCheckReleaseFeed's platform-skip logic from the interval gate (covered by
    // UpdateCheckThrottleTests).
    private readonly InMemoryPreferencesService _preferences = new();

    private UpdateCheckThrottle _throttle = null!;

    private UpdateChecker CreateUpdateChecker(FwLiteConfig? config = null)
    {
        var options = Options.Create(config ?? new FwLiteConfig());
        _throttle = new UpdateCheckThrottle(_preferences, options, Mock.Of<ILogger<UpdateCheckThrottle>>());
        var throttle = _throttle;
        return new UpdateChecker(
            _httpClientFactoryMock.Object,
            Mock.Of<ILogger<UpdateChecker>>(),
            options,
            new GlobalEventBus(Mock.Of<ILogger<GlobalEventBus>>()),
            _platformUpdateServiceMock.Object,
            throttle,
            new MemoryCache(new MemoryCacheOptions()));
    }

    [Theory]
    [InlineData(FwLitePlatform.iOS)]
    [InlineData(FwLitePlatform.Mac)]
    [InlineData(FwLitePlatform.Android)]
    public void ShouldCheckReleaseFeed_WhenStoreDistributedPlatform_ReturnsFalse(FwLitePlatform os)
    {
        //iOS/Mac have no GitHub release feed and Android is Play-driven; the feed round-trip is pointless.
        //Preferences are empty (never checked), so the throttle would otherwise allow the check - this
        //proves the platform skip wins for the default OnInterval condition.
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval, Os = os };
        var checker = CreateUpdateChecker(config);

        checker.ShouldCheckReleaseFeed().Should().BeFalse();
    }

    [Theory]
    [InlineData(FwLitePlatform.iOS)]
    [InlineData(FwLitePlatform.Mac)]
    [InlineData(FwLitePlatform.Android)]
    public void ShouldCheckReleaseFeed_WhenAlwaysConfigured_OverridesStoreDistributedSkip(FwLitePlatform os)
    {
        //The explicit Always override still forces a feed check (used for testing the endpoint on device).
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.Always, Os = os };
        var checker = CreateUpdateChecker(config);

        checker.ShouldCheckReleaseFeed().Should().BeTrue();
    }

    [Fact]
    public void ShouldCheckReleaseFeed_WhenNonStorePlatform_DelegatesToThrottle()
    {
        //Windows uses the feed; with an empty last-check the throttle allows the check.
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval, Os = FwLitePlatform.Windows };
        var checker = CreateUpdateChecker(config);

        checker.ShouldCheckReleaseFeed().Should().BeTrue();
    }

    [Fact]
    public void ShouldCheckReleaseFeed_WhenConfigSetToNever_ReturnsFalse()
    {
        var config = new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.Never, Os = FwLitePlatform.Windows };
        var checker = CreateUpdateChecker(config);

        checker.ShouldCheckReleaseFeed().Should().BeFalse();
    }

    /// <summary>Routes the Lexbox client through <paramref name="send"/> and counts the requests.</summary>
    private StubHandler UseHttpHandler(Func<HttpResponseMessage> send)
    {
        var handler = new StubHandler(send);
        _httpClientFactoryMock.Setup(f => f.CreateClient(UpdateChecker.HttpClientName))
            .Returns(() => new HttpClient(handler, false));
        return handler;
    }

    private class StubHandler(Func<HttpResponseMessage> send) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(send());
        }
    }

    private static HttpRequestException DnsFailure() =>
        new("No such host is known. (lexbox.org:443)", new SocketException((int)SocketError.HostNotFound));

    [Fact]
    public async Task CheckForUpdate_WhenRequestNeverReachesServer_DoesNotRecordCheckAndRetries()
    {
        //the scenario from the field: app launched while a VPN was still connecting, so DNS was dead.
        //That must not count as a check, otherwise the next retry is UpdateCheckInterval (8h) away.
        var handler = UseHttpHandler(() => throw DnsFailure());
        var checker = CreateUpdateChecker(new FwLiteConfig { Os = FwLitePlatform.Windows });

        (await checker.CheckForUpdate()).Should().BeNull();

        _throttle.LastUpdateCheck.Should().Be(DateTime.MinValue);
        _throttle.ShouldCheckForUpdate().Should().BeTrue();

        //a second call (e.g. connectivity regained a minute later) must hit the server again rather than
        //the manual-check cache
        await checker.CheckForUpdate();
        handler.Requests.Should().Be(2);
    }

    [Fact]
    public async Task CheckForUpdate_WhenServerResponds_RecordsCheckAndCachesResult()
    {
        var handler = UseHttpHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ShouldUpdateResponse(null))
        });
        var checker = CreateUpdateChecker(new FwLiteConfig { Os = FwLitePlatform.Windows });

        (await checker.CheckForUpdate()).Should().BeNull();

        _throttle.LastUpdateCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        _throttle.ShouldCheckForUpdate().Should().BeFalse();

        await checker.CheckForUpdate();
        handler.Requests.Should().Be(1, "a recent answer is served from the manual-check cache");
    }

    [Fact]
    public async Task CheckForUpdate_WhenServerReturnsError_StillRecordsCheck()
    {
        //the server was reachable and answered, so hammering it again on every launch gains nothing
        UseHttpHandler(() => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var checker = CreateUpdateChecker(new FwLiteConfig { Os = FwLitePlatform.Windows });

        (await checker.CheckForUpdate()).Should().BeNull();

        _throttle.ShouldCheckForUpdate().Should().BeFalse();
    }
}
