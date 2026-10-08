using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
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
    private StubHandler UseHttpHandler(Func<HttpResponseMessage> send) =>
        UseAsyncHttpHandler(() => Task.FromResult(send()));

    private StubHandler UseAsyncHttpHandler(Func<Task<HttpResponseMessage>> send)
    {
        var handler = new StubHandler(send);
        _httpClientFactoryMock.Setup(f => f.CreateClient(UpdateChecker.HttpClientName))
            .Returns(() => new HttpClient(handler, false));
        return handler;
    }

    private class StubHandler(Func<Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        private int _requests;
        public int Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);
            return send();
        }
    }

    private static HttpResponseMessage NoUpdateResponse() => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new ShouldUpdateResponse(null))
    };

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
    public async Task CheckForUpdate_WhenResponseIsMalformed_StillRecordsCheck()
    {
        //the server answered; a body we can't parse is not fixed by asking again on every launch
        UseHttpHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("this is not json", Encoding.UTF8, "application/json")
        });
        var checker = CreateUpdateChecker(new FwLiteConfig { Os = FwLitePlatform.Windows });

        (await checker.CheckForUpdate()).Should().BeNull();

        _throttle.ShouldCheckForUpdate().Should().BeFalse();
    }

    [Fact]
    public async Task TryUpdate_WhenCalledConcurrently_OnlyOneRequestIsMade()
    {
        //startup check in flight (slow DNS) while connectivity-regained triggers a retry: the second caller
        //must wait for the first and then see the recorded check instead of fetching (and applying) again
        var release = new TaskCompletionSource<HttpResponseMessage>();
#pragma warning disable VSTHRD003 // the test owns this TaskCompletionSource and completes it below
        var handler = UseAsyncHttpHandler(() => release.Task);
#pragma warning restore VSTHRD003
        var checker = CreateUpdateChecker(new FwLiteConfig { Os = FwLitePlatform.Windows });

        var first = checker.TryUpdate();
        var second = checker.TryUpdate();
        //let both callers get as far as they can before the server answers
        await Task.Delay(50);
        handler.Requests.Should().Be(1);

        release.SetResult(NoUpdateResponse());
        await Task.WhenAll(first, second);

        handler.Requests.Should().Be(1);
        _throttle.ShouldCheckForUpdate().Should().BeFalse();
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
