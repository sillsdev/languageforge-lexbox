using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using LexBoxApi.Services;
using LexCore.Auth;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LexBoxApi.Otel;

public static class OtelKernel
{
    public const string ServiceName = "LexBox-Api";
    public static void AddOpenTelemetryInstrumentation(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        var appResourceBuilder = ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector()
            .AddService(ServiceName, serviceVersion: AppVersionService.Version);
        services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            tracerProviderBuilder
                // Debugging
                // .AddConsoleExporter()
                .AddOtlpExporter(options =>
                {
                    configuration.Bind("Otel", options);
                })
                .AddSource(ServiceName)
                .SetResourceBuilder(appResourceBuilder)
                // could potentially add baggage to the trace as done in
                // https://github.com/honeycombio/honeycomb-opentelemetry-dotnet/blob/main/src/Honeycomb.OpenTelemetry.Instrumentation.AspNetCore/TracerProviderBuilderExtensions.cs
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    // don't log health checks
                    options.Filter = context => context.Request.Path.Value != "/api/healthz";
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.EnrichWithUser(request.HttpContext);
                    };
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.EnrichWithUser(response.HttpContext);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddNpgsql()
                .AddHotChocolateInstrumentation()
            );

        var meter = new Meter(ServiceName, AppVersionService.Version);
        var counter = meter.CreateCounter<long>("api.login-attempts");
        services.AddOpenTelemetry().WithMetrics(metricProviderBuilder =>
            metricProviderBuilder
                .AddOtlpExporter(options =>
                {
                    configuration.Bind("Otel", options);
                })
                .AddMeter(meter.Name)
                .SetResourceBuilder(appResourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddHttpClientInstrumentation()
        );
    }

    private static void EnrichWithUser(this Activity activity, HttpContext httpContext)
    {
        var claimsPrincipal = httpContext.User;
        var userId = claimsPrincipal?.FindFirstValue(LexAuthConstants.IdClaimType);
        if (userId != null)
        {
            activity.SetTag("app.user.id", userId);
        }
        var userRole = claimsPrincipal?.FindFirstValue(LexAuthConstants.RoleClaimType);
        if (userRole != null)
        {
            activity.SetTag("app.user.role", userRole);
        }
        activity.SetTag("http.abort", httpContext.RequestAborted.IsCancellationRequested);
    }
}
