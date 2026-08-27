using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.Json;
using FwLiteShared;
using FwLiteShared.Analytics;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FwLiteShared.Tests.Analytics;

public class AnalyticsServiceTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void SelectToken_UsesDebugTokenInDevelopment(bool isDevelopment, bool expectDebug)
    {
        var token = MixpanelAnalytics.SelectToken(isDevelopment, new AnalyticsConfig());
        if (expectDebug)
            token.Should().Be(MixpanelAnalytics.DebugProjectToken);
        else
            token.Should().BeNull();
    }

    [Fact]
    public void SelectToken_ReleaseTokenUsedWhenNotDev()
    {
        MixpanelAnalytics.SelectToken(false, new AnalyticsConfig { ProductionToken = "prod-token" }).Should().Be("prod-token");
        MixpanelAnalytics.SelectToken(false, new AnalyticsConfig { ProductionToken = "  " }).Should().BeNull();
        MixpanelAnalytics.SelectToken(false, new AnalyticsConfig()).Should().BeNull();
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

        var props = AnalyticsService.BuildProperties("tok",
            config,
            host: null,
            deviceId: "dev-1",
            userId: null,
            time: FixedTime,
            insertId: "insert-1");

        props["token"].Should().Be("tok");
        props["$device_id"].Should().Be("dev-1");
        props["$app_version_string"].Should().Be("1.2.3");
        props["$os"].Should().Be("Windows");
        props["$os_version"].Should().Be(RuntimeInformation.OSDescription);
        props["edition"].Should().Be("Windows");
        props["time"].Should().Be(FixedTime.ToUnixTimeSeconds());
        props["$insert_id"].Should().Be("insert-1");
        props.Should().NotContainKey("host");
        props.Should().NotContainKey("$user_id");
        props.Should().NotContainKey("distinct_id");
        props.Should().NotContainKey("app_version");
        props.Should().NotContainKey("os");
    }

    [Fact]
    public void BuildProperties_IncludesHostAndUserId()
    {
        var config = new FwLiteConfig { Os = FwLitePlatform.Windows };
        var props = AnalyticsService.BuildProperties("tok",
            config,
            host: "maui",
            deviceId: "dev-1",
            userId: "user-9",
            time: FixedTime,
            insertId: "insert-1");
        props["host"].Should().Be("maui");
        props["$user_id"].Should().Be("user-9");
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
        prefs.Get(nameof(PreferenceKey.AnalyticsUserId)).Should().BeNull();
    }

    [Fact]
    public void Identify_PersistsUserId()
    {
        var prefs = new MemoryPreferences();
        var service = CreateService(new CaptureHandler(), prefs: prefs);

        service.Identify("user-1");

        prefs.Get(nameof(PreferenceKey.AnalyticsUserId)).Should().Be("user-1");
        service.CurrentUserId.Should().Be("user-1");
    }

    [Fact]
    public async Task Relaunch_LoadsPersistedUserIdWithoutIdentify()
    {
        var handler = new CaptureHandler();
        var prefs = new MemoryPreferences();
        prefs.Set(nameof(PreferenceKey.AnalyticsDeviceId), "persisted-device");
        prefs.Set(nameof(PreferenceKey.AnalyticsUserId), "user-1");
        var service = CreateService(handler, isDevelopment: true, prefs: prefs);

        await service.TrackAsync(MixpanelAnalytics.AppLaunchedEvent);

        handler.LastBody.Should().Contain("\"$device_id\":\"persisted-device\"");
        handler.LastBody.Should().Contain("\"$user_id\":\"user-1\"");
    }

    [Fact]
    public void Identify_SameUser_KeepsDeviceId()
    {
        var prefs = new MemoryPreferences();
        var service = CreateService(new CaptureHandler(), prefs: prefs);

        service.Identify("user-1");
        var device = service.GetOrCreateDeviceId();
        service.Identify("user-1");

        service.GetOrCreateDeviceId().Should().Be(device);
        prefs.Get(nameof(PreferenceKey.AnalyticsUserId)).Should().Be("user-1");
    }

    [Fact]
    public void Identify_DifferentUser_RotatesDeviceId()
    {
        var prefs = new MemoryPreferences();
        var service = CreateService(new CaptureHandler(), prefs: prefs);

        service.Identify("user-1");
        var originalDevice = service.GetOrCreateDeviceId();
        service.Identify("user-2");

        var rotated = service.GetOrCreateDeviceId();
        rotated.Should().NotBe(originalDevice);
        prefs.Get(nameof(PreferenceKey.AnalyticsDeviceId)).Should().Be(rotated);
        prefs.Get(nameof(PreferenceKey.AnalyticsUserId)).Should().Be("user-2");
        service.CurrentUserId.Should().Be("user-2");
    }

    [Fact]
    public void SessionExpired_ThenLoginAsDifferentUser_RotatesDevice()
    {
        var prefs = new MemoryPreferences();
        var service = CreateService(new CaptureHandler(), prefs: prefs);
        var server = new LexboxServer(new Uri("https://lexbox.org"), "Lexbox");

        MixpanelAnalytics.ApplyAuthChange(service, server, AuthenticationChangeCause.Login, new LexboxUser("Ada", "user-1"));
        var originalDevice = service.GetOrCreateDeviceId();
        MixpanelAnalytics.ApplyAuthChange(service, server, AuthenticationChangeCause.SessionExpired, user: null);
        service.CurrentUserId.Should().Be("user-1");
        service.GetOrCreateDeviceId().Should().Be(originalDevice);

        MixpanelAnalytics.ApplyAuthChange(service, server, AuthenticationChangeCause.Login, new LexboxUser("Bob", "user-2"));

        service.CurrentUserId.Should().Be("user-2");
        service.GetOrCreateDeviceId().Should().NotBe(originalDevice);
        prefs.Get(nameof(PreferenceKey.AnalyticsUserId)).Should().Be("user-2");
    }

    [Fact]
    public void ApplyAuthChange_LexboxLogin_Identifies()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://lexbox.org"), "Lexbox");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, AuthenticationChangeCause.Login, new LexboxUser("Ada", "user-1"));

        analytics.Verify(a => a.Identify("user-1"), Times.Once);
        analytics.Verify(a => a.Reset(), Times.Never);
    }

    [Fact]
    public void ApplyAuthChange_LexboxLogout_Resets()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://lexbox.org"), "Lexbox");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, AuthenticationChangeCause.Logout, user: null);

        analytics.Verify(a => a.Reset(), Times.Once);
        analytics.Verify(a => a.Identify(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ApplyAuthChange_SessionExpired_DoesNotReset()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://lexbox.org"), "Lexbox");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, AuthenticationChangeCause.SessionExpired, user: null);

        analytics.Verify(a => a.Reset(), Times.Never);
        analytics.Verify(a => a.Identify(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ApplyAuthChange_Refresh_DoesNotIdentifyOrReset()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://lexbox.org"), "Lexbox");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, AuthenticationChangeCause.Refresh, new LexboxUser("Ada", "user-1"));

        analytics.Verify(a => a.Identify(It.IsAny<string>()), Times.Never);
        analytics.Verify(a => a.Reset(), Times.Never);
    }

    [Fact]
    public void ApplyAuthChange_EmptyCurrentUser_DoesNotIdentifyOrReset()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://lexbox.org"), "Lexbox");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, AuthenticationChangeCause.Login, new LexboxUser("Ada", ""));

        analytics.Verify(a => a.Identify(It.IsAny<string>()), Times.Never);
        analytics.Verify(a => a.Reset(), Times.Never);
    }

    [Fact]
    public void ApplyAuthChange_NonLexboxLogin_DoesNotSetUserId()
    {
        var analytics = new Mock<IAnalyticsService>();
        var server = new LexboxServer(new Uri("https://staging.languagedepot.org"), "Lexbox Staging");

        MixpanelAnalytics.ApplyAuthChange(analytics.Object, server, AuthenticationChangeCause.Login, new LexboxUser("Ada", "user-1"));

        analytics.Verify(a => a.Identify(It.IsAny<string>()), Times.Never);
        analytics.Verify(a => a.Reset(), Times.Never);
    }

    [Fact]
    public async Task TrackAsync_DoesNotSend_WhenReleaseTokenEmpty()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, isDevelopment: false);

        await service.TrackAsync("app_launched");

        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task TrackAsync_PostsJsonToMixpanel_InDevelopment()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, isDevelopment: true, host: "web");

        await service.TrackAsync("app_launched", time: FixedTime, insertId: "insert-1");

        handler.RequestCount.Should().Be(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Be(MixpanelAnalytics.TrackUrl + "?ip=1");
        handler.LastBody.Should().Contain("\"event\":\"app_launched\"");
        handler.LastBody.Should().Contain(MixpanelAnalytics.DebugProjectToken);
        handler.LastBody.Should().Contain("\"host\":\"web\"");
        handler.LastBody.Should().Contain("\"$device_id\":");
        handler.LastBody.Should().Contain($"\"time\":{FixedTime.ToUnixTimeSeconds()}");
        handler.LastBody.Should().Contain("\"$insert_id\":\"insert-1\"");
        handler.LastBody.Should().Contain("\"$os\":\"Windows\"");
        handler.LastBody.Should().Contain(JsonSerializer.Serialize(RuntimeInformation.OSDescription));
        handler.LastBody.Should().Contain("\"$os_version\":");
        handler.LastBody.Should().Contain("\"$app_version_string\":\"test\"");
        handler.LastBody.Should().NotContain("distinct_id");
        handler.LastBody.Should().NotContain("\"os\":");
        handler.LastBody.Should().NotContain("\"app_version\":");
    }

    [Fact]
    public async Task TrackAsync_UsesClockWhenTimeOmitted()
    {
        var handler = new CaptureHandler();
        var clock = new FixedTimeProvider(FixedTime);
        var service = CreateService(handler, isDevelopment: true, timeProvider: clock);

        await service.TrackAsync("app_launched");

        handler.LastBody.Should().Contain($"\"time\":{FixedTime.ToUnixTimeSeconds()}");
        handler.LastBody.Should().Contain("\"$insert_id\":");
    }

    [Fact]
    public async Task TrackAsync_EnricherMutatesEventProperties()
    {
        var handler = new CaptureHandler();
        var enricher = new StubEnricher("$android_os_version", "14");
        var service = CreateService(handler, isDevelopment: true, enrichers: [enricher]);

        await service.TrackAsync("app_launched");

        handler.LastBody.Should().Contain("\"$android_os_version\":\"14\"");
    }

    [Fact]
    public async Task TrackAsync_CallerPropertiesOverrideEnricher()
    {
        var handler = new CaptureHandler();
        var enricher = new StubEnricher("extra", 1);
        var service = CreateService(handler, isDevelopment: true, enrichers: [enricher]);

        await service.TrackAsync("app_launched", new Dictionary<string, object?> { ["extra"] = 2 });

        handler.LastBody.Should().Contain("\"extra\":2");
        handler.LastBody.Should().NotContain("\"extra\":1");
    }

    [Fact]
    public async Task TrackAsync_DoesNotThrow_WhenHttpFails()
    {
        var handler = new CaptureHandler { ThrowOnSend = true };
        var service = CreateService(handler, isDevelopment: true);

        var act = async () => await service.TrackAsync("app_launched");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void RecordProcessStart_TracksAppLaunched()
    {
        var analytics = new Mock<IAnalyticsService>();

        MixpanelAnalytics.RecordProcessStart(analytics.Object);

        analytics.Verify(a => a.Track(
            MixpanelAnalytics.AppLaunchedEvent,
            It.IsAny<IReadOnlyDictionary<string, object?>>(),
            It.IsAny<DateTimeOffset?>()), Times.Once);
    }

    [Fact]
    public async Task AppLaunchTracker_FiresOnceOnStart_NotOnStop()
    {
        var analytics = new Mock<IAnalyticsService>();
        var tracker = new AppLaunchTracker(analytics.Object);

        await tracker.StartAsync(CancellationToken.None);
        await tracker.StopAsync(CancellationToken.None);

        analytics.Verify(a => a.Track(
            MixpanelAnalytics.AppLaunchedEvent,
            It.IsAny<IReadOnlyDictionary<string, object?>>(),
            It.IsAny<DateTimeOffset?>()), Times.Once);
    }

    private static AnalyticsService CreateService(
        CaptureHandler handler,
        bool isDevelopment = true,
        string? host = null,
        IPreferencesService? prefs = null,
        IEnumerable<IAnalyticsEventEnricher>? enrichers = null,
        TimeProvider? timeProvider = null)
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
            AppVersion = "test",
            Os = FwLitePlatform.Windows,
        };

        var analytics = new AnalyticsConfig { Host = host };

        return new AnalyticsService(
            factory.Object,
            Options.Create(config),
            Options.Create(analytics),
            env,
            prefs ?? new MemoryPreferences(),
            Mock.Of<ILogger<AnalyticsService>>(),
            enrichers,
            timeProvider);
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

    private sealed class StubEnricher(string key, object? value) : IAnalyticsEventEnricher
    {
        public void Enrich(Dictionary<string, object?> properties) => properties[key] = value;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
