// See https://aka.ms/new-console-template for more information

using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using LcmDebugger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniLcm.Project;
using Moq;

var builder = Host.CreateApplicationBuilder();
//slows down import to log all sql.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Services.AddFwDataBridge();
builder.Services.AddLcmCrdtClient();
builder.Services.AddFwLiteProjectSync();
builder.Services.AddScoped((_services) => new Mock<IServerHttpClientProvider>().Object);

using var app = builder.Build();

await using var scope = app.Services.CreateAsyncScope();
var services = scope.ServiceProvider;

// await services.SyncDownloadedProject("uzb-flex-20251021141736/uzb-flex-64ae5ccc-97fb-4e0a-ba8c-7140b51393d8", dryRun: false);
// await services.PrintAllEntries("sena-3");
