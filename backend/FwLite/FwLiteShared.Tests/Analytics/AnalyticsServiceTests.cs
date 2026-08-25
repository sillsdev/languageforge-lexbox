using System.Net;
using System.Text;
using FwLiteShared;
using FwLiteShared.Analytics;
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
    public void BuildProperties_IncludesSuperPropertiesAndOmitsEmptyHost()
    {
        var config = new FwLiteConfig
        {
            AppVersion = "1.2.3",
            Os = FwLitePlatform.Windows,
            Edition = LexCore.Entities.FwLiteEdition.Windows,
        };

        var props = AnalyticsService.BuildProperties("tok", config, host: null, extra: null);

        props["token"].Should().Be("tok");
        props["app_version"].Should().Be("1.2.3");
        props["os"].Should().Be("Windows");
        props["edition"].Should().Be("Windows");
        props.Should().NotContainKey("host");
        props.Should().NotContainKey("distinct_id");
    }

    [Fact]
    public void BuildProperties_IncludesHostWhenSet()
    {
        var config = new FwLiteConfig { Os = FwLitePlatform.Windows };
        var props = AnalyticsService.BuildProperties("tok",
            config,
            host: "maui",
            extra: new Dictionary<string, object?> { ["extra"] = 1 });
        props["host"].Should().Be("maui");
        props["extra"].Should().Be(1);
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

    private static AnalyticsService CreateService(
        CaptureHandler handler,
        bool isDevelopment,
        bool useDevAssets,
        string? host = null)
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
            Mock.Of<ILogger<AnalyticsService>>())
        {
            Host = host
        };
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
