using System.Net;
using System.Text;
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
        string? productionToken = null)
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

        return new LexboxAnalyticsService(
            mixpanelClient,
            Options.Create(config),
            environment,
            BuildLoggedInContext(userId));
    }

    private static LoggedInContext BuildLoggedInContext(Guid? userId)
    {
        var httpContext = new DefaultHttpContext();
        if (userId is not null)
        {
            var user = new LexAuthUser
            {
                Id = userId.Value,
                Name = "Test User",
                Email = "test@example.com",
                Role = UserRole.user,
                Locale = "en",
            };
            httpContext.User = user.GetPrincipal("Testing");
        }
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        return new LoggedInContext(accessor, NullLogger<LoggedInContext>.Instance);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string LastBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("1", Encoding.UTF8)
            };
        }
    }
}
