using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy;

namespace LexBoxApi;

public static class LexBoxKernel
{
    public static void AddLexBoxApi(this IServiceCollection services,
        ConfigurationManager configuration,
        IWebHostEnvironment environment)
    {
        //todo config
        services.AddAuthentication();
        services.AddAuthorization();
        
        services.AddScoped<IProxyAuthService, ProxyAuthService>();
        services.AddSyncProxy(configuration, environment);
        AuthKernel.AddLexBoxAuth(services, configuration, environment);
    }
}