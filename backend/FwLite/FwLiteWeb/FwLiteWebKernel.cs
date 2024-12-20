﻿using SIL.Harmony;
using FwLiteShared;
using FwLiteShared.Auth;
using LcmCrdt;
using FwLiteWeb.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace FwLiteWeb;

public static class FwLiteWebKernel
{
    public static IServiceCollection AddFwLiteWebServices(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<UrlContext>();
        services.AddSingleton<IRedirectUrlProvider, ServerRedirectUrlProvider>();
        services.AddFwLiteShared(environment);

        services.AddOptions<FwLiteWebConfig>().BindConfiguration("FwLiteWeb");

        services.AddOptions<JsonOptions>().PostConfigure<IOptions<CrdtConfig>>((jsonOptions, crdtConfig) =>
        {
            jsonOptions.SerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeLcmCrdtExternalJsonTypeResolver();
        });

        services.AddOptions<JsonHubProtocolOptions>().PostConfigure<IOptions<CrdtConfig>>(
            (jsonOptions, crdtConfig) =>
            {
                jsonOptions.PayloadSerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeLcmCrdtExternalJsonTypeResolver();
            });
        return services;
    }
}
