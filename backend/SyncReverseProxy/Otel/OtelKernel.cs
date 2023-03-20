using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LexSyncReverseProxy.Otel;

public static class OtelKernel
{
    public static void AddOpenTelemetryInstrumentation(this IServiceCollection services)
    {
        var serviceName = "LexBox";
        var appResourceBuilder = ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector()
            .AddService(serviceName);
        var activitySource = new ActivitySource(serviceName);
        services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            tracerProviderBuilder
                .AddConsoleExporter()
                .AddOtlpExporter()
                .AddSource(activitySource.Name)
                .SetResourceBuilder(appResourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
            );
    }
}