using FwHeadless;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using WebServiceDefaults;

namespace Testing.FwHeadless;

public class LexboxHttpClientTests
{
    private static ServiceProvider BuildServices()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults("test");
        builder.Services.AddFwHeadless();
        return builder.Services.BuildServiceProvider();
    }

    private static List<Type> HandlerChain(ServiceProvider services, string clientName)
    {
        var handler = services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(clientName);
        var chain = new List<Type>();
        while (handler is DelegatingHandler delegating)
        {
            if (delegating is ResilienceHandler or HttpClientAuthHandler) chain.Add(delegating.GetType());
            handler = delegating.InnerHandler!;
        }
        return chain;
    }

    private static HttpStandardResilienceOptions StandardOptions(ServiceProvider services, string clientName)
    {
        return services.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>().Get($"{clientName}-standard");
    }

    [Theory]
    [InlineData(FwHeadlessKernel.LexboxHttpClientName)]
    [InlineData(FwHeadlessKernel.LexboxSyncHttpClientName)]
    public void EachRetryRunsAuth(string clientName)
    {
        using var services = BuildServices();
        HandlerChain(services, clientName).Should().Equal(typeof(ResilienceHandler), typeof(HttpClientAuthHandler));
    }

    [Fact]
    public void SyncClient_UsesLongerTimeouts()
    {
        using var services = BuildServices();
        var options = StandardOptions(services, FwHeadlessKernel.LexboxSyncHttpClientName);
        options.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(90));
    }

    [Fact]
    public void DefaultClient_KeepsTheStandardTimeouts()
    {
        using var services = BuildServices();
        var options = StandardOptions(services, FwHeadlessKernel.LexboxHttpClientName);
        options.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(10));
        options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }
}
