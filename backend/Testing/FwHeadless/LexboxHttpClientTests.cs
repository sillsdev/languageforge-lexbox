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

    private static int CountResilienceHandlers(HttpMessageHandler handler)
    {
        var count = 0;
        while (handler is DelegatingHandler delegating)
        {
            if (delegating is ResilienceHandler) count++;
            handler = delegating.InnerHandler!;
        }
        return count;
    }

    [Fact]
    public void SyncClient_ReplacesTheDefaultResilienceHandler()
    {
        using var services = BuildServices();
        var handler = services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(FwHeadlessKernel.LexboxSyncHttpClientName);

        CountResilienceHandlers(handler).Should().Be(1);
        var options = services.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get($"{FwHeadlessKernel.LexboxSyncHttpClientName}-standard");
        options.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(90));
    }

    [Fact]
    public void DefaultClient_KeepsTheStandardTimeouts()
    {
        using var services = BuildServices();
        var options = services.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get($"{FwHeadlessKernel.LexboxHttpClientName}-standard");
        options.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }
}
