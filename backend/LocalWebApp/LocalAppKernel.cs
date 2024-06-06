﻿using System.Text.Json;
using Crdt;
using FwDataMiniLcmBridge;
using LcmCrdt;
using LocalWebApp.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Refit;

namespace LocalWebApp;

public static class LocalAppKernel
{
    public static IServiceCollection AddLocalAppServices(this IServiceCollection services)
    {
        services.AddSingleton<BackgroundSyncService>();
        services.AddHttpContextAccessor();
        services.AddScoped<SyncService>();
        services.AddSingleton<ImportFwdataService>();
        services.AddSingleton<IHostedService>(s => s.GetRequiredService<BackgroundSyncService>());
        services.AddLcmCrdtClient();
        services.AddFwDataBridge();

        services.AddOptions<JsonOptions>().PostConfigure<IOptions<CrdtConfig>>((jsonOptions, crdtConfig) =>
        {
            jsonOptions.SerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeJsonTypeResolver();
        });

        services.AddOptions<JsonHubProtocolOptions>().PostConfigure<IOptions<CrdtConfig>>(
            (jsonOptions, crdtConfig) =>
            {
                jsonOptions.PayloadSerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeJsonTypeResolver();
            });
        services.AddHttpClient();
        services.AddSingleton<RefitSettings>(provider => new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = provider.GetRequiredService<IOptions<CrdtConfig>>().Value
                    .MakeJsonTypeResolver()
            })
        });
        services.AddSingleton<CrdtHttpSyncService>();
        return services;
    }
}
