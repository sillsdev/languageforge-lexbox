using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WebServiceDefaults;

// Adds common .NET services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder, string appVersion) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry(appVersion);

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder, string appVersion)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder =>
            {
                resourceBuilder.AddAttributes([
                    new("service.version", appVersion),
                    new("service.instance.id", Guid.NewGuid().ToString())
                ]);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation().AddProcessInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        // don't log health checks
                        options.Filter = context => context.Request.Path.Value != "/api/healthz";
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {

                            if (request.HttpContext.RequestAborted.IsCancellationRequested)
                                activity.SetTag("http.abort", true);
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            if (response.HttpContext.RequestAborted.IsCancellationRequested)
                                activity.SetTag("http.abort", true);
                        };

                    })
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    public static TBuilder ConfigureAdditionalOpenTelemetry<TBuilder>(this TBuilder builder, Action<OpenTelemetryBuilder> configure) where TBuilder : IHostApplicationBuilder
    {
        configure(builder.Services.AddOpenTelemetry());
        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddRequestTimeouts(
            configure: static timeouts =>
                timeouts.AddPolicy("HealthChecks", TimeSpan.FromSeconds(5)));

        builder.Services.AddOutputCache(
            configureOptions: static caching =>
                caching.AddPolicy("HealthChecks",
                    build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));

        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Configured per https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/health-checks#non-development-environments
        var healthChecks = app.MapGroup("/api");
        healthChecks
            .CacheOutput("HealthChecks")
            .WithRequestTimeout("HealthChecks");


        // All health checks must pass for app to be considered ready to accept traffic after starting
        healthChecks.MapHealthChecks("/healthz");

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        healthChecks.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });


        return app;
    }
}
