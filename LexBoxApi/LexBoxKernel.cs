using LexBoxApi.Services;
using LexCore.ServiceInterfaces;
using WebApi;
using WebApi.Auth;

namespace LexBoxApi;

public static class LexBoxKernel
{
    public static void AddLexBoxApi(this IServiceCollection services, IConfigurationRoot configuration)
    {
        //todo config
        services.AddAuthentication();
        services.AddAuthorization();
        
        services.AddScoped<IProxyAuthService, ProxyAuthService>();
        services.AddSyncProxy(configuration);
    }
}