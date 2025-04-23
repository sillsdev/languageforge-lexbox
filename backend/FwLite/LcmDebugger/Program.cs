// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using LcmDebugger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm.Exceptions;
using Refit;
using SIL.Harmony;
using SIL.Harmony.Core;

var builder = Host.CreateApplicationBuilder();
//slows down import to log all sql.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Services.AddFwDataBridge();
builder.Services.AddLcmCrdtClient();
builder.Services.AddFwLiteProjectSync();

using var app = builder.Build();

await using var scope = app.Services.CreateAsyncScope();

var crdtProjectsService = app.Services.GetRequiredService<CrdtProjectsService>();
var crdtApi = await crdtProjectsService.OpenProject(
    new CrdtProject("test", "sbe-flex - Copy.sqlite"),
    scope.ServiceProvider);
var dbContext = scope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    TypeInfoResolver = scope.ServiceProvider.GetRequiredService<IOptions<CrdtConfig>>().Value
        .MakeLcmCrdtExternalJsonTypeResolver()
};
await using var transaction = await dbContext.Database.BeginTransactionAsync();
var dataModel = scope.ServiceProvider.GetRequiredService<DataModel>();
await dataModel.SyncWith(FakeSyncSource.FromJsonFile("changes.json", jsonOptions));
await transaction.RollbackAsync();
