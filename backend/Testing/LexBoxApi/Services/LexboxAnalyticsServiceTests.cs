using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore.Analytics;
using LexCore.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Testing.LexBoxApi.Services;

public class LexboxAnalyticsServiceTests
{
    [Fact]
    public async Task TrackSendReceiveCompleted_SendsEventWithCurrentUserIdAndProduct()
    {
        var handler = new CaptureHandler();
        var userId = Guid.NewGuid();
        var service = CreateService(handler, userId: userId);

        await service.TrackSendReceiveCompleted();

        handler.RequestCount.Should().Be(1);
        handler.LastBody.Should().Contain("\"event\":\"send_receive_completed\"");
        handler.LastBody.Should().Contain($"\"$user_id\":\"{userId}\"");
        handler.LastBody.Should().Contain("\"product\":\"lexbox\"");
        handler.LastBody.Should().Contain($"\"$app_version_string\":{JsonSerializer.Serialize(AppVersionService.Version)}");
        handler.LastBody.Should().Contain(MixpanelTokens.DebugProjectToken);
    }

    [Fact]
    public async Task TrackSendReceiveCompleted_SkipsWhenNoAuthenticatedUser()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, userId: null);

        await service.TrackSendReceiveCompleted();

        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task TrackSendReceiveCompleted_SkipsWhenUserOptedOut()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, userId: Guid.NewGuid(), optedOutOfAnalytics: true);

        await service.TrackSendReceiveCompleted();

        handler.RequestCount.Should().Be(0);
    }

    [Theory]
    [InlineData(ILexboxAnalyticsService.PasswordLoginType)]
    [InlineData(ILexboxAnalyticsService.GoogleLoginType)]
    public async Task TrackLoginCompleted_SendsEventWithLoginTypeAndUserId(string loginType)
    {
        var handler = new CaptureHandler();
        // No ambient user: login events pass the authenticated user explicitly.
        var service = CreateService(handler, userId: null);
        var userId = Guid.NewGuid();

        await service.TrackLoginCompleted(BuildUser(userId), loginType);

        handler.RequestCount.Should().Be(1);
        handler.LastBody.Should().Contain("\"event\":\"login_completed\"");
        handler.LastBody.Should().Contain($"\"$user_id\":\"{userId}\"");
        handler.LastBody.Should().Contain($"\"login_type\":\"{loginType}\"");
        handler.LastBody.Should().Contain("\"product\":\"lexbox\"");
        handler.LastBody.Should().Contain($"\"$app_version_string\":{JsonSerializer.Serialize(AppVersionService.Version)}");
        handler.LastBody.Should().Contain(MixpanelTokens.DebugProjectToken);
    }

    [Fact]
    public async Task TrackLoginCompleted_SkipsWhenUserOptedOut()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, userId: null);

        await service.TrackLoginCompleted(BuildUser(Guid.NewGuid(), optedOutOfAnalytics: true),
            ILexboxAnalyticsService.PasswordLoginType);

        handler.RequestCount.Should().Be(0);
    }

    [Theory]
    [InlineData(AccountCreatedVia.Registration, "registration")]
    [InlineData(AccountCreatedVia.Invitation, "invitation")]
    [InlineData(AccountCreatedVia.Admin, "admin")]
    [InlineData(AccountCreatedVia.ProjectInvite, "project_invite")]
    public async Task TrackAccountCreated_SendsEventWithCreatedViaAndUserId(AccountCreatedVia createdVia, string expectedValue)
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, userId: null);
        var userId = Guid.NewGuid();

        await service.TrackAccountCreated(userId, createdVia);

        handler.RequestCount.Should().Be(1);
        handler.LastBody.Should().Contain("\"event\":\"account_created\"");
        handler.LastBody.Should().Contain($"\"$user_id\":\"{userId}\"");
        handler.LastBody.Should().Contain($"\"created_via\":\"{expectedValue}\"");
        handler.LastBody.Should().Contain("\"product\":\"lexbox\"");
        handler.LastBody.Should().Contain($"\"$app_version_string\":{JsonSerializer.Serialize(AppVersionService.Version)}");
        handler.LastBody.Should().Contain(MixpanelTokens.DebugProjectToken);
    }

    [Fact]
    public async Task TrackAccountCreated_SkipsWhenDisabled()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, userId: null, enabled: false);

        await service.TrackAccountCreated(Guid.NewGuid(), AccountCreatedVia.Registration);

        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task Track_StampsClientIpAndDoesNotGeolocateFromRequest()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, userId: Guid.NewGuid(), clientIp: "203.0.113.7");

        await service.TrackSendReceiveCompleted();

        handler.RequestCount.Should().Be(1);
        // The end user's IP is sent as the reserved "ip" property so Mixpanel geolocates them...
        handler.LastBody.Should().Contain("\"ip\":\"203.0.113.7\"");
        // ...and we must NOT ask Mixpanel to geolocate from the request (server) IP.
        handler.LastRequestUri.Should().NotContain("ip=1");
    }

    [Fact]
    public async Task TrackSendReceiveCompleted_SkipsWhenDisabled()
    {
        var handler = new CaptureHandler();
        var service = CreateService(handler, userId: Guid.NewGuid(), enabled: false);

        await service.TrackSendReceiveCompleted();

        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task TrackSendReceiveCompleted_SkipsWhenProductionTokenEmpty()
    {
        var handler = new CaptureHandler();
        // Production environment with no release token configured => nothing sent.
        var service = CreateService(handler, userId: Guid.NewGuid(), isDevelopment: false, productionToken: "");

        await service.TrackSendReceiveCompleted();

        handler.RequestCount.Should().Be(0);
    }

    private static LexboxAnalyticsService CreateService(
        CaptureHandler handler,
        Guid? userId,
        bool enabled = true,
        bool isDevelopment = true,
        string? productionToken = null,
        bool optedOutOfAnalytics = false,
        string? clientIp = null)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(MixpanelClient.HttpClientName))
            .Returns(() => new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });
        var mixpanelClient = new MixpanelClient(factory.Object, NullLogger<MixpanelClient>.Instance);

        var config = new AnalyticsConfigBase { Enabled = enabled, Product = MixpanelProducts.Lexbox };
        if (productionToken is not null)
            config.ProductionToken = productionToken;

        var environment = new HostingEnvironment
        {
            EnvironmentName = isDevelopment ? Environments.Development : Environments.Production
        };

        var accessor = BuildHttpContextAccessor(userId, optedOutOfAnalytics, clientIp);
        var loggedInContext = new LoggedInContext(accessor, NullLogger<LoggedInContext>.Instance);

        return new LexboxAnalyticsService(
            mixpanelClient,
            Options.Create(config),
            environment,
            loggedInContext,
            accessor);
    }

    private static LexAuthUser BuildUser(Guid userId, bool optedOutOfAnalytics = false) => new()
    {
        Id = userId,
        Name = "Test User",
        Email = "test@example.com",
        Role = UserRole.user,
        Locale = "en",
        OptedOutOfAnalytics = optedOutOfAnalytics ? true : null,
    };

    private static HttpContextAccessor BuildHttpContextAccessor(Guid? userId, bool optedOutOfAnalytics, string? clientIp)
    {
        var httpContext = new DefaultHttpContext();
        if (userId is not null)
        {
            httpContext.User = BuildUser(userId.Value, optedOutOfAnalytics).GetPrincipal("Testing");
        }
        if (!string.IsNullOrEmpty(clientIp))
        {
            // Middleware resolves the real visitor into RemoteIpAddress; the service only reads that.
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
        }
        return new HttpContextAccessor { HttpContext = httpContext };
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string LastBody { get; private set; } = "";
        public string LastRequestUri { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUri = request.RequestUri?.ToString() ?? "";
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("1", Encoding.UTF8)
            };
        }
    }
}
