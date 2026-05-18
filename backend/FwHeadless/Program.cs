using FwHeadless;
using FwHeadless.Routes;
using FwHeadless.Services;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using LexCore.Exceptions;
using LexData;
using Scalar.AspNetCore;
using Npgsql;
using OpenTelemetry.Trace;
using WebServiceDefaults;
using AppVersion = LexCore.AppVersion;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler(options =>
{
    options.StatusCodeSelector = (exception) =>
    {
        if (exception is ProjectSyncInProgressException)
            return StatusCodes.Status409Conflict;
        return StatusCodes.Status500InternalServerError;
    };
});

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

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapDefaultEndpoints();
app.MapMediaFileRoutes();
app.MapMergeRoutes();

// DELETE endpoint to delete the FieldWorks repo/project (and nothing else)
app.MapDelete("/api/manage/repo/{projectId}", async (Guid projectId, ProjectDeletionService deletionService) =>
{
    return await deletionService.DeleteRepo(projectId)
        ? Results.Ok(new { message = "Repo deleted" })
        : Results.NotFound(new { message = "Project not found" });
});

// DELETE endpoint to delete the entire fw-headless project (FieldWorks repo/project, CRDT DB and project snapshot)
app.MapDelete("/api/manage/project/{projectId}", async (Guid projectId, ProjectDeletionService deletionService) =>
{
    return await deletionService.DeleteProject(projectId)
        ? Results.Ok(new { message = "Project deleted" })
        : Results.NotFound(new { message = "Project not found" });
});

app.Run();
