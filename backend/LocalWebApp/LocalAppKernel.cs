using System.Text.Json;
using CrdtLib;
using LcmCrdt;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Refit;

namespace LocalWebApp;

public static class LocalAppKernel
{
    public static void AddLocalAppServices(this IServiceCollection services)
    {
        services.AddSingleton<BackgroundSyncService>();
        services.AddScoped<SyncService>();
        services.AddSingleton<IHostedService>(s => s.GetRequiredService<BackgroundSyncService>());
        services.AddLcmCrdtClient("tmp.sqlite",
            services.BuildServiceProvider().GetService<ILoggerFactory>());

        services.AddOptions<JsonHubProtocolOptions>().PostConfigure<IOptions<CrdtConfig>>(
            (jsonOptions, crdtConfig) =>
            {
                jsonOptions.PayloadSerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeJsonTypeResolver();
            });
        services.AddRefitClient<ISyncHttp>(provider => new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new(JsonSerializerDefaults.Web)
                {
                    TypeInfoResolver = provider.GetRequiredService<IOptions<CrdtConfig>>().Value.MakeJsonTypeResolver()
                })
            })
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:5158/"));
        services.AddSingleton<CrdtHttpSync>();
    }
}
