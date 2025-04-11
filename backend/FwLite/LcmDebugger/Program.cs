// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using LexCore.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Refit;
using SIL.Harmony;
using SIL.Harmony.Core;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddFwDataBridge();
builder.Services.AddLcmCrdtClient();
builder.Services.AddFwLiteProjectSync();

using var app = builder.Build();

var fieldWorksProjectList = app.Services.GetRequiredService<FieldWorksProjectList>();
var fwDataProject = fieldWorksProjectList.GetProject("petit-test-train-flex") ?? throw new InvalidOperationException("Could not find project");

var importService = app.Services.GetRequiredService<MiniLcmImport>();
await importService.Import(fwDataProject);
await using var scope = app.Services.CreateAsyncScope();

var crdtProjectsService = app.Services.GetRequiredService<CrdtProjectsService>();
var crdtProject = crdtProjectsService.GetProject(fwDataProject.Name);
if (crdtProject is null) throw new NotFoundException("project", "crdt");
var crdtApi = await crdtProjectsService.OpenProject(crdtProject, scope.ServiceProvider);
var dataModel = scope.ServiceProvider.GetRequiredService<DataModel>();
JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
{
    TypeInfoResolver = scope.ServiceProvider.GetRequiredService<IOptions<CrdtConfig>>().Value
        .MakeLcmCrdtExternalJsonTypeResolver()
};
var (commits, _) = await dataModel.GetChanges(new SyncState([]));
await using var f = File.Create("commits.json");
await JsonSerializer.SerializeAsync(f, commits, options);

var fwDataFactory = app.Services.GetRequiredService<FwDataFactory>();
// var miniLcmApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, false);
// var entries = await miniLcmApi.GetEntries().ToArrayAsync();


fwDataFactory.Dispose();
