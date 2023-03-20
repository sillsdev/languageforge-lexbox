using System.Diagnostics;
using System.Diagnostics.Metrics;
using LexCore.Auth;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LexBoxApi.Otel;

public static class OtelKernel
{
    public const string ServiceName = "LexBox";
    public static void AddOpenTelemetryInstrumentation(this IServiceCollection services)
    {
        var appResourceBuilder = ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector()
            .AddService(ServiceName);
        services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            tracerProviderBuilder
                .AddConsoleExporter()
                .AddOtlpExporter()
                .AddSource(ServiceName)
                .SetResourceBuilder(appResourceBuilder)
                // could potentially add baggage to the trace as done in
                // https://github.com/honeycombio/honeycomb-opentelemetry-dotnet/blob/main/src/Honeycomb.OpenTelemetry.Instrumentation.AspNetCore/TracerProviderBuilderExtensions.cs
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.EnrichWithUserId(request.HttpContext);
                    };
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.EnrichWithUserId(response.HttpContext);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddNpgsql()
                .AddHotChocolateInstrumentation()
            );

        var meter = new Meter(ServiceName);
        var counter = meter.CreateCounter<long>("api.login-attempts");
        services.AddOpenTelemetry().WithMetrics(metricProviderBuilder =>
            metricProviderBuilder
                .AddOtlpExporter()
                .AddMeter(meter.Name)
                .SetResourceBuilder(appResourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
        );
    }

    private static void EnrichWithUserId(this Activity activity, HttpContext httpContext)
    {
        var claimsPrincipal = httpContext.User;
        var user = LexAuthUser.FromClaimsPrincipal(claimsPrincipal);
        if (user != null)
        {
            activity.SetTag("app.user.id", user.Id);
        }
    }
}