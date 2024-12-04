using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteShared.Auth;
using LcmCrdt;
using LocalWebApp;
using LocalWebApp.Components;
using LocalWebApp.Hubs;
using LocalWebApp.Routes;
using LocalWebApp.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NReco.Logging.File;

namespace LocalWebApp;

public static class LocalWebAppServer
{
    public static WebApplication SetupAppServer(WebApplicationOptions options, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(options);
        if (!builder.Environment.IsDevelopment() && options.Args?.Contains("--urls") != true)
            builder.WebHost.UseUrls("http://127.0.0.1:0");
        if (builder.Environment.IsDevelopment())
        {
            //do this early so we catch bugs on startup
            ProjectLoader.Init();
        }

        builder.ConfigureDev<AuthConfig>(config =>
            config.LexboxServers = [
                new (new("https://lexbox.dev.languagetechnology.org"), "Lexbox Dev"),
                new (new("https://localhost:3000"), "Lexbox Local"),
                new (new("https://staging.languagedepot.org"), "Lexbox Staging")
            ]);
        builder.ConfigureProd<AuthConfig>(config =>
            config.LexboxServers = [new(new("https://staging.languagedepot.org"), "Lexbox Staging")]);
        builder.Services.Configure<AuthConfig>(c => c.ClientId = "becf2856-0690-434b-b192-a4032b72067f");
        builder.Logging.AddDebug();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        if (builder.Configuration.GetValue<string>("LocalWebApp:LogFileName") is { Length: > 0 } logFileName)
        {
            builder.Logging.AddFile(logFileName);
        }
        builder.Services.AddCors();
        builder.Services.AddLocalAppServices(builder.Environment);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSignalR(options =>
        {
            options.AddFilter(new LockedProjectFilter());
            options.EnableDetailedErrors = true;
        }).AddJsonProtocol();

        configure?.Invoke(builder);
        var app = builder.Build();
        app.Logger.LogInformation("FwLite LocalWebApp startup");
// Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDirectoryBrowser();
        }

        if (app.Services.GetRequiredService<IOptions<LocalWebAppConfig>>().Value.CorsAllowAny)
        {
            app.UseCors(policyBuilder =>
            {
                policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        }

        app.UseAntiforgery();

        app.Use(async (context, next) =>
        {
            var projectName = context.GetProjectName();
            if (!string.IsNullOrWhiteSpace(projectName))
            {
                var projectsService = context.RequestServices.GetRequiredService<CrdtProjectsService>();
                projectsService.SetProjectScope(projectsService.GetProject(projectName) ??
                                                throw new InvalidOperationException(
                                                    $"Project {projectName} not found"));
                await context.RequestServices.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
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

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(FwLiteShared._Imports).Assembly);
        return app;
    }
}
