using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using LexBoxApi.Services;
using LexCore.Auth;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quartz;

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
                .AddProcessor<UserEnricher>()
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
                        var contentLength = request.Headers.ContentLength;
                        if (contentLength.HasValue)
                        {
                            activity.SetTag("http.request.body.size", contentLength.Value);
                        }
                        activity.EnrichWithUser(request.HttpContext);
                    };
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        var contentLength = response.Headers.ContentLength;
                        if (contentLength.HasValue)
                        {
                            activity.SetTag("http.response.body.size", contentLength.Value);
                        }
                        activity.EnrichWithUser(response.HttpContext);
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        var contentLength = request.Content?.Headers.ContentLength;
                        if (contentLength.HasValue)
                        {
                            activity.SetTag("http.request.body.size", contentLength.Value);
                        }

                        if (request.RequestUri is not null)
                        {
                            activity.SetTag("url.path", request.RequestUri.AbsolutePath);
                            if (!string.IsNullOrEmpty(request.RequestUri.Query))
                                activity.SetTag("url.query", request.RequestUri.Query);
                        }
                    };
                    options.EnrichWithHttpResponseMessage = (activity, response) =>
                    {
                        var contentLength = response.Content.Headers.ContentLength;
                        if (contentLength.HasValue)
                        {
                            activity.SetTag("http.response.body.size", contentLength.Value);
                        }
                    };
                })
                .AddEntityFrameworkCoreInstrumentation()
                .AddNpgsql()
                .AddQuartzInstrumentation(options =>
                {
                    options.RecordException = true;
                })
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
        if (httpContext.RequestAborted.IsCancellationRequested)
            activity.SetTag("http.abort", true);
    }

    private class UserEnricher(IHttpContextAccessor contextAccessor) : BaseProcessor<Activity>
    {
        public override void OnStart(Activity data)
        {
            if (contextAccessor.HttpContext is {} context) data.EnrichWithUser(context);
        }
    }
}
