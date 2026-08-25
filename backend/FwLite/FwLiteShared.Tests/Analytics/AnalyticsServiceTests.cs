using System.Net;
using System.Text;
using FwLiteShared;
using FwLiteShared.Analytics;
using FwLiteShared.Auth;
using FwLiteShared.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FwLiteShared.Tests.Analytics;

public class AnalyticsServiceTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    public void SelectToken_UsesDebugTokenInDevOrDevAssets(bool isDevelopment, bool useDevAssets, bool expectDebug)
    {
        var token = MixpanelAnalytics.SelectToken(isDevelopment, useDevAssets);
        if (expectDebug)
            token.Should().Be(MixpanelAnalytics.DebugProjectToken);
        else
            token.Should().BeNull();
    }

    [Fact]
    public void SelectToken_ReleaseTokenUsedWhenNotDev()
    {
        MixpanelAnalytics.SelectToken(false, false, "prod-token").Should().Be("prod-token");
        MixpanelAnalytics.SelectToken(false, false, "  ").Should().BeNull();
    }

    [Fact]
    public void BuildProperties_IncludesSuperPropertiesDeviceIdAndOmitsEmptyHost()
    {
        var config = new FwLiteConfig
        {
            AppVersion = "1.2.3",
            Os = FwLitePlatform.Windows,
            Edition = LexCore.Entities.FwLiteEdition.Windows,
        };

        var props = AnalyticsService.BuildProperties("tok", config, host: null, deviceId: "dev-1", userId: null, extra: null);

        props["token"].Should().Be("tok");
        props["$device_id"].Should().Be("dev-1");
        props["app_version"].Should().Be("1.2.3");
        props["os"].Should().Be("Windows");
        props["edition"].Should().Be("Windows");
        props.Should().NotContainKey("host");
        props.Should().NotContainKey("$user_id");
        props.Should().NotContainKey("distinct_id");
    }

    [Fact]
    public void BuildProperties_IncludesHostAndUserIdWhenSet()
    {
        var config = new FwLiteConfig { Os = FwLitePlatform.Windows };
        var props = AnalyticsService.BuildProperties("tok",
            config,
            host: "maui",
            deviceId: "dev-1",
            userId: "user-9",
            extra: new Dictionary<string, object?> { ["extra"] = 1 });
        props["host"].Should().Be("maui");
        props["$user_id"].Should().Be("user-9");
        props["extra"].Should().Be(1);
    }

    [Fact]
    public void FirstLaunch_GeneratesAndPersistsDeviceId()
    {
        var prefs = new MemoryPreferences();
        var service = CreateService(new CaptureHandler(), prefs: prefs);

        var id = service.GetOrCreateDeviceId();

        Guid.TryParse(id, out _).Should().BeTrue();
        prefs.Get(nameof(PreferenceKey.AnalyticsDeviceId)).Should().Be(id);
        service.GetOrCreateDeviceId().Should().Be(id);
    }

    [Fact]
    public void Relaunch_ReusesPersistedDeviceId()
    {
        var prefs = new MemoryPreferences();
        prefs.Set(nameof(PreferenceKey.AnalyticsDeviceId), "persisted-device");

        var first = CreateService(new CaptureHandler(), prefs: prefs);
        var second = CreateService(new CaptureHandler(), prefs: prefs);

        first.GetOrCreateDeviceId().Should().Be("persisted-device");
        second.GetOrCreateDeviceId().Should().Be("persisted-device");
    }

    [Fact]
    public async Task Identify_AttachesDeviceAndUserIdsOnTrack()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, isDevelopment: true);

        service.Identify("lexbox-user-id");
        await service.TrackAsync("app_launched");

        handler.LastBody.Should().Contain("\"$device_id\":");
        handler.LastBody.Should().Contain("\"$user_id\":\"lexbox-user-id\"");
        handler.LastBody.Should().NotContain("distinct_id");
    }

    [Fact]
    public async Task Identify_EmptyId_DoesNotSetUserId()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, isDevelopment: true);

        service.Identify(" ");
        await service.TrackAsync("app_launched");

        handler.LastBody.Should().Contain("\"$device_id\":");
        handler.LastBody.Should().NotContain("$user_id");
    }

    [Fact]
    public async Task Reset_RotatesDeviceIdAndDropsUserId()
    {
        var handler = new CaptureHandler();
        var prefs = new MemoryPreferences();
        var service = CreateService(handler, isDevelopment: true, prefs: prefs);

        var original = service.GetOrCreateDeviceId();
        service.Identify("lexbox-user-id");
        service.Reset();
        await service.TrackAsync("app_launched");

        var rotated = prefs.Get(nameof(PreferenceKey.AnalyticsDeviceId));
        rotated.Should().NotBeNullOrEmpty();
        rotated.Should().NotBe(original);
        handler.LastBody.Should().Contain($"\"$device_id\":\"{rotated}\"");
        handler.LastBody.Should().NotContain("$user_id");
    }

    [Fact]
    public void ApplyAuthChange_LexboxLogin_Identifies()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://lexbox.org"), "Lexbox");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, new LexboxUser("Ada", "user-1"));

        analytics.Verify(a => a.Identify("user-1"), Times.Once);
        analytics.Verify(a => a.Reset(), Times.Never);
    }

    [Fact]
    public void ApplyAuthChange_LexboxLogout_Resets()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://lexbox.org"), "Lexbox");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, user: null);

        analytics.Verify(a => a.Reset(), Times.Once);
        analytics.Verify(a => a.Identify(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ApplyAuthChange_EmptyCurrentUser_DoesNotIdentifyOrReset()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://lexbox.org"), "Lexbox");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, new LexboxUser("Ada", ""));

        analytics.Verify(a => a.Identify(It.IsAny<string>()), Times.Never);
        analytics.Verify(a => a.Reset(), Times.Never);
    }

    [Fact]
    public void ApplyAuthChange_NonLexboxLogin_DoesNotSetUserId()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://staging.languagedepot.org"), "Lexbox Staging");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, new LexboxUser("Ada", "user-1"));

        analytics.Verify(a => a.Identify(It.IsAny<string>()), Times.Never);
        analytics.Verify(a => a.Reset(), Times.Never);
    }

    [Fact]
    public async Task TrackAsync_DoesNotSend_WhenReleaseTokenEmpty()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, isDevelopment: false, useDevAssets: false);

        await service.TrackAsync("app_launched");

        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task TrackAsync_PostsJsonToMixpanel_InDevelopment()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, isDevelopment: true, useDevAssets: false, host: "web");

        await service.TrackAsync("app_launched");

        handler.RequestCount.Should().Be(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Be(MixpanelAnalytics.TrackUrl);
        handler.LastBody.Should().Contain("\"event\":\"app_launched\"");
        handler.LastBody.Should().Contain(MixpanelAnalytics.DebugProjectToken);
        handler.LastBody.Should().Contain("\"host\":\"web\"");
        handler.LastBody.Should().Contain("\"$device_id\":");
        handler.LastBody.Should().NotContain("distinct_id");
    }

    [Fact]
    public async Task TrackAsync_DoesNotThrow_WhenHttpFails()
    {
        var handler = new CaptureHandler { ThrowOnSend = true };
        var service = CreateService(handler, isDevelopment: true, useDevAssets: false);

        var act = async () => await service.TrackAsync("app_launched");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void RecordProcessStart_SetsHostAndTracksAppLaunched()
    {
        var analytics = new Mock<IAnalyticsService>();

        MixpanelAnalytics.RecordProcessStart(analytics.Object, MixpanelAnalytics.MauiHost);

        analytics.VerifySet(a => a.Host = MixpanelAnalytics.MauiHost, Times.Once);
        analytics.Verify(a => a.Track(MixpanelAnalytics.AppLaunchedEvent, It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Once);
    }

    [Fact]
    public async Task AppLaunchTracker_FiresOnceOnStart_NotOnStop()
    {
        var analytics = new Mock<IAnalyticsService>();
        var tracker = new AppLaunchTracker(analytics.Object, MixpanelAnalytics.WebHost);

        await tracker.StartAsync(CancellationToken.None);
        await tracker.StopAsync(CancellationToken.None);

        analytics.VerifySet(a => a.Host = MixpanelAnalytics.WebHost, Times.Once);
        analytics.Verify(a => a.Track(MixpanelAnalytics.AppLaunchedEvent, It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Once);
    }

    private static AnalyticsService CreateService(
        CaptureHandler handler,
        bool isDevelopment = true,
        bool useDevAssets = false,
        string? host = null,
        IPreferencesService? prefs = null)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(MixpanelAnalytics.HttpClientName))
            .Returns(() => new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });

        var env = new HostingEnvironment
        {
            EnvironmentName = isDevelopment ? Environments.Development : Environments.Production
        };

        var config = new FwLiteConfig
        {
            UseDevAssets = useDevAssets,
            AppVersion = "test",
            Os = FwLitePlatform.Windows,
        };

        return new AnalyticsService(
            factory.Object,
            Options.Create(config),
            env,
            prefs ?? new MemoryPreferences(),
            Mock.Of<ILogger<AnalyticsService>>())
        {
            Host = host
        };
    }

    private sealed class MemoryPreferences : IPreferencesService
    {
        private readonly Dictionary<string, string> _values = new();

        public string? Get(string key) => _values.GetValueOrDefault(key);
        public void Set(string key, string value) => _values[key] = value;
        public void Remove(string key) => _values.Remove(key);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastBody { get; private set; } = "";
        public bool ThrowOnSend { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequest = request;
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            if (ThrowOnSend)
                throw new HttpRequestException("simulated Mixpanel outage");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("1", Encoding.UTF8)
            };
        }
    }
}
