// Performance profiling for FwLite sync
using System.Diagnostics;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using LcmDebugger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniLcm.Project;
using Moq;

var logFile = Path.Combine(Path.GetTempPath(), "fwlite-sync-profile.log");
await using var logWriter = new StreamWriter(logFile, append: false) { AutoFlush = true };

void Log(string msg)
{
    var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
    Console.WriteLine(line);
    logWriter.WriteLine(line);
}

Log($"=== FwLite Sync Performance Profile ===");
Log($"Log file: {logFile}");

var totalSw = Stopwatch.StartNew();

var builder = Host.CreateApplicationBuilder();
builder.Logging.AddFilter("LinqToDB", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("FwDataMiniLcmBridge", LogLevel.Warning);
builder.Logging.AddFilter("LcmCrdt", LogLevel.Warning);
builder.Services.AddFwDataBridge();
builder.Services.AddLcmCrdtClient();
builder.Services.AddFwLiteProjectSync();
builder.Services.AddScoped((_services) => new Mock<IServerHttpClientProvider>().Object);

using var app = builder.Build();
await using var scope = app.Services.CreateAsyncScope();
var services = scope.ServiceProvider;

// Phase 1: Open project
var sw = Stopwatch.StartNew();
using var project = await services.OpenDownloadedProject("orc-flex-cbfc6f5f-7fc6-4e58-bc64-5e15a4b7c238_20260416041936", openCopy: true);
Log($"Phase 1 - Open project: {sw.Elapsed}");

var crdtApi = project.CrdtApi;
var fwdataApi = project.FwApi;

// Phase 2: Load snapshot
sw.Restart();
var snapshotService = services.GetRequiredService<ProjectSnapshotService>();
var projectSnapshot = await snapshotService.GetProjectSnapshot(fwdataApi.Project);
Log($"Phase 2 - Load snapshot from disk: {sw.Elapsed}");
if (projectSnapshot is null)
{
    Log("ERROR: No snapshot found - this would trigger Import path, not Sync. Exiting.");
    return;
}
Log($"  Snapshot entries: {projectSnapshot.Entries.Length}");
Log($"  Snapshot senses: {projectSnapshot.Entries.Sum(e => e.Senses.Count)}");

// Use the real sync service to test the full batched path
sw.Restart();
var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
var result = await syncService.Sync(crdtApi, fwdataApi, projectSnapshot, dryRun: false);
Log($"Phase 3 - Full sync: {sw.Elapsed}");
Log($"  CRDT changes: {result.CrdtChanges}, FwData changes: {result.FwdataChanges}");

// Phase 4: Save FwData
sw.Restart();
fwdataApi.Save();
Log($"Phase 4 - FwData Save: {sw.Elapsed}");

// Phase 5: Regenerate snapshot
sw.Restart();
await snapshotService.RegenerateProjectSnapshot(crdtApi, fwdataApi.Project, keepBackup: false);
Log($"Phase 5 - Regenerate snapshot: {sw.Elapsed}");

Log($"=== TOTAL: {totalSw.Elapsed} ===");
Log($"Log written to: {logFile}");
