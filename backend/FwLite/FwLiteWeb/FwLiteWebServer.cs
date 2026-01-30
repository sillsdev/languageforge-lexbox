using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteShared;
using FwLiteShared.Auth;
using FwLiteShared.Services;
using LcmCrdt;
using FwLiteWeb;
using FwLiteWeb.Components;
using FwLiteWeb.Hubs;
using FwLiteWeb.Routes;
using FwLiteWeb.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NReco.Logging.File;

namespace FwLiteWeb;

public static class FwLiteWebServer
{
    public static WebApplication SetupAppServer(WebApplicationOptions options, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(options);
        if (!builder.Environment.IsDevelopment() && options.Args?.Contains("--urls") != true && string.IsNullOrEmpty(builder.Configuration["http_ports"]))
            builder.WebHost.UseUrls("http://127.0.0.1:0");
        if (builder.Environment.IsDevelopment())
        {
            //do this early so we catch bugs on startup
            ProjectLoader.Init();
        }

        builder.ConfigureDev<AuthConfig>(config =>
            config.LexboxServers = [
                ..config.LexboxServers,
                new(new("https://lexbox.org"), "Lexbox"),
                new (new("https://staging.languagedepot.org"), "Lexbox Staging"),
                new (new("https://lexbox.dev.languagetechnology.org"), "Lexbox Dev"),
                new (new("https://localhost:3050"), "Lexbox Local"),
            ]);
        builder.ConfigureProd<AuthConfig>(config =>
            config.LexboxServers = [
                ..config.LexboxServers,
                new(new("https://lexbox.org"), "Lexbox")
            ]);
        builder.Services.Configure<FwLiteConfig>(config =>
        {
            config.AppVersion = VersionHelper.DisplayVersion(typeof(FwLiteWebServer).Assembly);
            //todo os should be web, when the server is running remotely to the client, but linux runs the server locally so we will default using the OS to determine the platform (the default value)
        });
        builder.Logging.AddDebug();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents(circuitOptions => circuitOptions.DetailedErrors = true);
        if (builder.Configuration.GetValue<string>("FwLiteWeb:LogFileName") is { Length: > 0 } logFileName)
        {
            builder.Logging.AddFile(logFileName,
                fileLoggerOptions =>
                {
                    fileLoggerOptions.RollingFilesConvention = FileLoggerOptions.FileRollingConvention.Descending;
                    fileLoggerOptions.FileSizeLimitBytes = 10 * 1024 * 1024;
                    fileLoggerOptions.MaxRollingFiles = 3;
                });
        }
        builder.Services.AddCors();
        builder.Services.AddFwLiteWebServices(builder.Environment);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSignalR(options =>
        {
            options.AddFilter(new LockedProjectFilter());
            options.EnableDetailedErrors = true;
        }).AddJsonProtocol();
        builder.Services.AddHealthChecks();

        configure?.Invoke(builder);
        var app = builder.Build();
        app.Logger.LogInformation("FwLite FwLiteWeb startup");
// Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.ConfigObject.DisplayRequestDuration = true;
            o.EnableTryItOutByDefault();
        });
        if (app.Environment.IsDevelopment())
        {
            app.UseDirectoryBrowser();
        }

        if (app.Services.GetRequiredService<IOptions<FwLiteWebConfig>>().Value.CorsAllowAny)
        {
            app.UseCors(policyBuilder =>
            {
                policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        }

        app.UseAntiforgery();

        app.Use(async (context, next) =>
        {
            var projectCode = context.GetProjectCode();
            if (!string.IsNullOrWhiteSpace(projectCode))
            {
                await context.RequestServices.GetRequiredService<CurrentProjectService>().SetupProjectContext(projectCode);
            }
            var fwData = context.GetFwDataName();
            if (!string.IsNullOrWhiteSpace(fwData))
            {
                var fwDataProjectContext = context.RequestServices.GetRequiredService<FwDataProjectContext>();
                var fwProjectList = context.RequestServices.GetRequiredService<FieldWorksProjectList>();
                fwDataProjectContext.Project = fwProjectList.GetProject(fwData) ?? throw new InvalidOperationException($"FwData {fwData} not found");
            }

            await next(context);
        });
        app.MapHub<CrdtMiniLcmApiHub>($"/api/hub/{{{CrdtMiniLcmApiHub.ProjectRouteKey}}}/lexbox");
        app.MapHub<FwDataMiniLcmHub>($"/api/hub/{{{FwDataMiniLcmHub.ProjectRouteKey}}}/fwdata");
        app.MapHistoryRoutes();
        app.MapActivities();
        app.MapProjectRoutes();
        app.MapFwIntegrationRoutes();
        app.MapFeedbackRoutes();
        app.MapTest();
        app.MapImport();
        app.MapAuthRoutes();
        app.MapMiniLcmRoutes("/api/mini-lcm");
        app.MapHealthChecks("/health");

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode(endpointOptions =>
            {
                endpointOptions.ContentSecurityFrameAncestorsPolicy = "self http://localhost:*";
            })
            .AddAdditionalAssemblies(typeof(FwLiteShared._Imports).Assembly);
        return app;
    }
}
