using System.Text.Json;
using FwDataMiniLcmBridge.Api;
using FwLiteProjectSync.SyncHelpers;
using LcmCrdt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync;

public class CrdtFwdataProjectSyncService(IOptions<LcmCrdtConfig> lcmCrdtConfig, MiniLcmImport miniLcmImport, ILogger<CrdtFwdataProjectSyncService> logger)
{
    public record SyncResult(int CrdtChanges, int FwdataChanges);

    public async Task<SyncResult> Sync(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi, bool dryRun = false)
    {
        var projectSnapshot = await GetProjectSnapshot(fwdataApi.Project.Name);
        SyncResult result = await Sync(crdtApi, fwdataApi, dryRun, fwdataApi.EntryCount, projectSnapshot);

        if (!dryRun)
        {
            await SaveProjectSnapshot(fwdataApi.Project.Name,
                new ProjectSnapshot(await fwdataApi.GetEntries().ToArrayAsync()));
        }
        return result;
    }

    private async Task<SyncResult> Sync(IMiniLcmApi crdtApi, IMiniLcmApi fwdataApi, bool dryRun, int entryCount, ProjectSnapshot? projectSnapshot)
    {
        if (dryRun)
        {
            crdtApi = new DryRunMiniLcmApi(crdtApi);
            fwdataApi = new DryRunMiniLcmApi(fwdataApi);
        }

        if (projectSnapshot is null)
        {
            await miniLcmImport.ImportProject(crdtApi, fwdataApi, entryCount);
            LogDryRun(crdtApi, "crdt");
            return new SyncResult(entryCount, 0);
        }

        //todo sync complex form types, parts of speech, semantic domains, writing systems

        var currentFwDataEntries = await fwdataApi.GetEntries().ToArrayAsync();
        var crdtChanges = await EntrySync.Sync(currentFwDataEntries, projectSnapshot.Entries, crdtApi);
        LogDryRun(crdtApi, "crdt");

        var fwdataChanges = await EntrySync.Sync(await crdtApi.GetEntries().ToArrayAsync(), currentFwDataEntries, fwdataApi);
        LogDryRun(fwdataApi, "fwdata");

        //todo push crdt changes to lexbox

        return new SyncResult(crdtChanges, fwdataChanges);
    }

    private void LogDryRun(IMiniLcmApi api, string type)
    {
        if (api is not DryRunMiniLcmApi dryRunApi) return;
        foreach (var dryRunRecord in dryRunApi.DryRunRecords)
        {
            logger.LogInformation($"Dry run {type} record: {dryRunRecord.Method} {dryRunRecord.Description}");
        }

        logger.LogInformation($"Dry run {type} changes: {dryRunApi.DryRunRecords.Count}");
    }

    public record ProjectSnapshot(Entry[] Entries);

    private async Task<ProjectSnapshot?> GetProjectSnapshot(string projectName)
    {
        var snapshotPath = Path.Combine(lcmCrdtConfig.Value.ProjectPath, $"{projectName}_snapshot.json");
        if (!File.Exists(snapshotPath)) return null;
        await using var file = File.OpenRead(snapshotPath);
        return await JsonSerializer.DeserializeAsync<ProjectSnapshot>(file);
    }

    private async Task SaveProjectSnapshot(string projectName, ProjectSnapshot projectSnapshot)
    {
        var snapshotPath = Path.Combine(lcmCrdtConfig.Value.ProjectPath, $"{projectName}_snapshot.json");
        await using var file = File.Create(snapshotPath);
        await JsonSerializer.SerializeAsync(file, projectSnapshot);
    }

}
