using FwHeadless;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
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

    [Theory]
    [InlineData(FwHeadlessKernel.LexboxHttpClientName)]
    [InlineData(FwHeadlessKernel.LexboxSyncHttpClientName)]
    public void EachRetryRunsAuth(string clientName)
    {
        using var services = BuildServices();
        HandlerChain(services, clientName).Should().Equal(typeof(ResilienceHandler), typeof(HttpClientAuthHandler));
    }
}
