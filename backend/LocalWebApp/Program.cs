using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using LcmCrdt;
using LocalWebApp;
using LocalWebApp.Hubs;
using LocalWebApp.Routes;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    builder.WebHost.UseUrls("http://127.0.0.1:0");

builder.Services.AddLocalAppServices();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR().AddJsonProtocol();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var sharedOptions = new SharedOptions() { FileProvider = new ManifestEmbeddedFileProvider(typeof(Program).Assembly) };
app.UseDefaultFiles(new DefaultFilesOptions(sharedOptions));
app.UseStaticFiles(new StaticFileOptions(sharedOptions));
app.Use(async (context, next) =>
{
    var projectName = context.GetProjectName();
    if (!string.IsNullOrWhiteSpace(projectName))
    {
        var projectsService = context.RequestServices.GetRequiredService<ProjectsService>();
        projectsService.SetProjectScope(projectsService.GetProject(projectName) ??
                                        throw new InvalidOperationException($"Project {projectName} not found"));
    }

    var fwData = context.GetFwDataName();
    if (!string.IsNullOrWhiteSpace(fwData))
    {
        var fwDataProjectContext = context.RequestServices.GetRequiredService<FwDataProjectContext>();
        fwDataProjectContext.Project = FieldWorksProjectList.GetProject(fwData) ?? throw new InvalidOperationException($"FwData {fwData} not found");
    }

    await next(context);
});
app.MapHub<CrdtMiniLcmApiHub>($"/api/hub/{{{CrdtMiniLcmApiHub.ProjectRouteKey}}}/lexbox");
app.MapHub<FwDataMiniLcmHub>($"/api/hub/{{{FwDataMiniLcmHub.ProjectRouteKey}}}/fwdata");
app.MapHistoryRoutes();
app.MapActivities();
app.MapProjectRoutes();
app.MapTest();
app.MapImport();

await using (app)
{
    await app.StartAsync();

    if (!app.Environment.IsDevelopment())
    {
        var url = app.Urls.First();
        LocalAppLauncher.LaunchBrowser(url);
    }

    await app.WaitForShutdownAsync();
}
