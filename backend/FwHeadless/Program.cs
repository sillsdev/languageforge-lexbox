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

// Chorus discovers its file-type handler plugins with `new DirectoryCatalog(".", "*-ChorusPlugin.dll")`,
// which scans the *current working directory* -- not the app base dir -- and WebApplication.CreateBuilder
// does not change CWD. When launched from anywhere other than the folder holding the co-published
// LibFLExBridge-ChorusPlugin.dll (e.g. `dotnet run`, whose CWD is the project dir), that plugin isn't
// found. Without it, FieldWorks extensions like `.list` fall back to Chorus's 1 MB LargeFileFilter
// default, so the >1 MB SemanticDomainList.list is silently filtered out of a new project's first push
// (and later merges lose their FieldWorks handlers too). Pin CWD to the base dir so the plugin is always
// discovered. Safe here: hg runs with explicit working dirs and TemplatesFolder resolves against
// AppContext.BaseDirectory already.
Environment.CurrentDirectory = AppContext.BaseDirectory;

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
app.MapProjectRoutes();

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
