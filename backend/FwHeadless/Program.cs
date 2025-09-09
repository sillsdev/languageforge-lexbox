using System.Diagnostics;
using FwHeadless;
using FwHeadless.Routes;
using FwHeadless.Services;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using LexCore.Sync;
using LexData;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using SIL.Harmony.Core;
using SIL.Harmony;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Trace;
using WebServiceDefaults;
using AppVersion = LexCore.AppVersion;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

builder.Services.AddLexData(
    autoApplyMigrations: false,
    useOpenIddict: false,
    useSeeding: false
);

builder.Services.AddFwHeadless();
builder.AddServiceDefaults(AppVersion.Get(typeof(Program))).ConfigureAdditionalOpenTelemetry(telemetryBuilder =>
{
    telemetryBuilder.WithTracing(b => b.AddNpgsql()
        .AddEntityFrameworkCoreInstrumentation(c =>
        {
            //never emit traces for sqlite as there's way too much noise and it'll crash servers and overrun honeycomb
            c.Filter = (provider, command) => provider is not "Microsoft.EntityFrameworkCore.Sqlite";
            c.SetDbStatementForText = true;
        })
        .AddSource(FwHeadlessActivitySource.ActivitySourceName,
            FwLiteProjectSyncActivitySource.ActivitySourceName,
            FwDataMiniLcmBridgeActivitySource.ActivitySourceName,
            LcmCrdtActivitySource.ActivitySourceName));
});

var app = builder.Build();

// Add lexbox-version header to all requests
app.Logger.LogInformation("FwHeadless version: {version}", AppVersionService.Version);
app.Use(async (context, next) =>
{
    context.Response.Headers["lexbox-version"] = AppVersionService.Version;
    await next();
});

// Load project ID from request
app.Use((context, next) =>
{
    var renameThisService = context.RequestServices.GetRequiredService<ProjectContextFromIdService>();
    return renameThisService.PopulateProjectContext(context, next);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    //access at /scalar/v1
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();
app.MapMediaFileRoutes();
app.MapMergeRoutes();

// DELETE endpoint to remove a project if it exists
app.MapDelete("/api/manage/repo/{projectId}", async (Guid projectId,
    ProjectLookupService projectLookupService,
    IOptions<FwHeadlessConfig> config,
    SyncJobStatusService syncJobStatusService,
    ILogger<Program> logger) =>
{
    if (syncJobStatusService.SyncStatus(projectId) is SyncJobStatus.Running)
    {
        return Results.Conflict(new {message = "Sync job is running"});
    }
    var projectCode = await projectLookupService.GetProjectCode(projectId);
    if (projectCode is null)
    {
        logger.LogInformation("DELETE repo request for non-existent project {ProjectId}", projectId);
        return Results.NotFound(new { message = "Project not found" });
    }
    // Delete associated project folder if it exists
    var fwDataProject = config.Value.GetFwDataProject(projectCode, projectId);
    if (Directory.Exists(fwDataProject.ProjectFolder))
    {
        logger.LogInformation("Deleting repository for project {ProjectCode} ({ProjectId})", projectCode, projectId);
        Directory.Delete(fwDataProject.ProjectFolder, true);
    }
    else
    {
        logger.LogInformation("Repository for project {ProjectCode} ({ProjectId}) does not exist", projectCode, projectId);
    }
    return Results.Ok(new { message = "Repo deleted" });
});

app.Run();
