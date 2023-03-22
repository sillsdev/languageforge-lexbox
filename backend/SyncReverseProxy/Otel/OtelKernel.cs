using System.Diagnostics;
using System.Security.Claims;
using LexCore.Auth;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LexSyncReverseProxy.Otel;

public static class OtelKernel
{
    public const string ServiceName = "LexBox-SyncProxy";
    public static void AddOpenTelemetryInstrumentation(this IServiceCollection services)
    {
        var appResourceBuilder = ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector()
            .AddService(ServiceName);
        services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            tracerProviderBuilder
                // Debugging
                // .AddConsoleExporter()
                .AddOtlpExporter()
                .AddSource(ServiceName)
                .SetResourceBuilder(appResourceBuilder)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
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
    }
}