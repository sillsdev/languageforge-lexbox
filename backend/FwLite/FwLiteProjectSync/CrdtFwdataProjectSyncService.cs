using System.Text.Json;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using LexCore.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync;

public class CrdtFwdataProjectSyncService(IOptions<LcmCrdtConfig> lcmCrdtConfig, MiniLcmImport miniLcmImport, ILogger<CrdtFwdataProjectSyncService> logger)
{
    public async Task<SyncResult> Sync(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi, bool dryRun = false)
    {
        if (crdtApi is CrdtMiniLcmApi crdt && crdt.ProjectData.FwProjectId != fwdataApi.ProjectId)
        {
            throw new InvalidOperationException($"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdataApi.ProjectId}");
        }
        var projectSnapshot = await GetProjectSnapshot(fwdataApi.Project.Name, fwdataApi.Project.ProjectsPath);
        SyncResult result = await Sync(crdtApi, fwdataApi, dryRun, fwdataApi.EntryCount, projectSnapshot);
        fwdataApi.Save();

        if (!dryRun)
        {
            await SaveProjectSnapshot(fwdataApi.Project.Name, fwdataApi.Project.ProjectsPath,
                new ProjectSnapshot(
                    await fwdataApi.GetEntries().ToArrayAsync(),
                    await fwdataApi.GetPartsOfSpeech().ToArrayAsync(),
                    await fwdataApi.GetSemanticDomains().ToArrayAsync()));
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

        //todo sync complex form types, writing systems

        var currentFwDataPartsOfSpeech = await fwdataApi.GetPartsOfSpeech().ToArrayAsync();
        var crdtChanges = await PartOfSpeechSync.Sync(currentFwDataPartsOfSpeech, projectSnapshot.PartsOfSpeech, crdtApi);
        var fwdataChanges = await PartOfSpeechSync.Sync(await crdtApi.GetPartsOfSpeech().ToArrayAsync(), currentFwDataPartsOfSpeech, fwdataApi);

        var currentFwDataSemanticDomains = await fwdataApi.GetSemanticDomains().ToArrayAsync();
        crdtChanges += await SemanticDomainSync.Sync(currentFwDataSemanticDomains, projectSnapshot.SemanticDomains, crdtApi);
        fwdataChanges += await SemanticDomainSync.Sync(await crdtApi.GetSemanticDomains().ToArrayAsync(), currentFwDataSemanticDomains, fwdataApi);

        var currentFwDataEntries = await fwdataApi.GetEntries().ToArrayAsync();
        crdtChanges += await EntrySync.Sync(currentFwDataEntries, projectSnapshot.Entries, crdtApi);
        LogDryRun(crdtApi, "crdt");

        fwdataChanges += await EntrySync.Sync(await crdtApi.GetEntries().ToArrayAsync(), currentFwDataEntries, fwdataApi);
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

    public record ProjectSnapshot(Entry[] Entries, PartOfSpeech[] PartsOfSpeech, SemanticDomain[] SemanticDomains);

    private async Task<ProjectSnapshot?> GetProjectSnapshot(string projectName, string? projectPath)
    {
        projectPath ??= lcmCrdtConfig.Value.ProjectPath;
        var snapshotPath = Path.Combine(projectPath, $"{projectName}_snapshot.json");
        if (!File.Exists(snapshotPath)) return null;
        await using var file = File.OpenRead(snapshotPath);
        return await JsonSerializer.DeserializeAsync<ProjectSnapshot>(file);
    }

    private async Task SaveProjectSnapshot(string projectName, string? projectPath, ProjectSnapshot projectSnapshot)
    {
        projectPath ??= lcmCrdtConfig.Value.ProjectPath;
        var snapshotPath = Path.Combine(projectPath, $"{projectName}_snapshot.json");
        await using var file = File.Create(snapshotPath);
        await JsonSerializer.SerializeAsync(file, projectSnapshot);
    }

}
