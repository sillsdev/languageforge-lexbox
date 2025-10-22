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

using var project = await services.OpenDownloadedProject("sena-3-aaac6f5f-7fc6-4e58-bc64-5e15a4b7c238_20251022120817", openCopy: true);
await services.SyncFwHeadlessProject(project, dryRun: false);

// await services.PrintAllEntries("sena-3");
