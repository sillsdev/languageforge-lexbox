﻿#if !DISABLE_FW_BRIDGE
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
#endif
using LcmCrdt;
using LocalWebApp;
using LocalWebApp.Hubs;
using LocalWebApp.Auth;
using LocalWebApp.Routes;
using LocalWebApp.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;

namespace LocalWebApp;

public static class LocalWebAppServer
{
    public static WebApplication SetupAppServer(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        if (!builder.Environment.IsDevelopment())
            builder.WebHost.UseUrls("http://127.0.0.1:0");
        if (builder.Environment.IsDevelopment())
        {
#if !DISABLE_FW_BRIDGE
    //do this early so we catch bugs on startup
    ProjectLoader.Init();
#endif
        }

        builder.ConfigureDev<AuthConfig>(config =>
            config.DefaultAuthority = new("https://lexbox.dev.languagetechnology.org"));
//for now prod builds will also use lt dev until we deploy oauth to prod
        builder.ConfigureProd<AuthConfig>(config =>
            config.DefaultAuthority = new("https://lexbox.dev.languagetechnology.org"));
        builder.Services.Configure<AuthConfig>(c => c.ClientId = "becf2856-0690-434b-b192-a4032b72067f");

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
// Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

//configure dotnet to serve static files from the embedded resources
        var sharedOptions =
            new SharedOptions() { FileProvider = new ManifestEmbeddedFileProvider(typeof(Program).Assembly) };
        app.UseDefaultFiles(new DefaultFilesOptions(sharedOptions));
        var staticFileOptions = new StaticFileOptions(sharedOptions);
        app.UseStaticFiles(staticFileOptions);

        app.Use(async (context, next) =>
        {
            var projectName = context.GetProjectName();
            if (!string.IsNullOrWhiteSpace(projectName))
            {
                var projectsService = context.RequestServices.GetRequiredService<ProjectsService>();
                projectsService.SetProjectScope(projectsService.GetProject(projectName) ??
                                                throw new InvalidOperationException(
                                                    $"Project {projectName} not found"));
                await context.RequestServices.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
            }
#if !DISABLE_FW_BRIDGE
    var fwData = context.GetFwDataName();
    if (!string.IsNullOrWhiteSpace(fwData))
    {
        var fwDataProjectContext = context.RequestServices.GetRequiredService<FwDataProjectContext>();
        fwDataProjectContext.Project =
 FieldWorksProjectList.GetProject(fwData) ?? throw new InvalidOperationException($"FwData {fwData} not found");
    }
#endif

            await next(context);
        });
        app.MapHub<CrdtMiniLcmApiHub>($"/api/hub/{{{CrdtMiniLcmApiHub.ProjectRouteKey}}}/lexbox");
#if !DISABLE_FW_BRIDGE
app.MapHub<FwDataMiniLcmHub>($"/api/hub/{{{FwDataMiniLcmHub.ProjectRouteKey}}}/fwdata");
#endif
        app.MapHistoryRoutes();
        app.MapActivities();
        app.MapProjectRoutes();
        app.MapFwIntegrationRoutes();
        app.MapTest();
        app.MapImport();
        app.MapAuthRoutes();
        app.MapFallbackToFile("index.html", staticFileOptions);
        return app;
    }
}
