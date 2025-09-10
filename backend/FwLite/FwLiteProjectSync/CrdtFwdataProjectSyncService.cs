using System.Diagnostics;
using System.Text.Json;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using LexCore.Sync;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.SyncHelpers;
using MiniLcm.Validators;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync;

public class CrdtFwdataProjectSyncService(MiniLcmImport miniLcmImport, ILogger<CrdtFwdataProjectSyncService> logger,
    MiniLcmApiValidationWrapperFactory validationWrapperFactory, MiniLcmApiStringNormalizationWrapperFactory normalizationWrapperFactory)
{
    public record DryRunSyncResult(
        int CrdtChanges,
        int FwdataChanges,
        List<DryRunMiniLcmApi.DryRunRecord> CrdtDryRunRecords,
        List<DryRunMiniLcmApi.DryRunRecord> FwDataDryRunRecords) : SyncResult(CrdtChanges, FwdataChanges);

    public async Task<DryRunSyncResult> SyncDryRun(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi)
    {
        return (DryRunSyncResult)await Sync(crdtApi, fwdataApi, true);
    }

    public async Task<SyncResult> Sync(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi, bool dryRun = false)
    {
        using var activity = FwLiteProjectSyncActivitySource.Value.StartActivity();
        if (crdtApi is CrdtMiniLcmApi crdt && crdt.ProjectData.FwProjectId != fwdataApi.ProjectId)
        {
            activity?.SetStatus(ActivityStatusCode.Error, $"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdataApi.ProjectId}");
            throw new InvalidOperationException($"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdataApi.ProjectId}");
        }
        var projectSnapshot = await GetProjectSnapshot(fwdataApi.Project);
        SyncResult result = await Sync(crdtApi, fwdataApi, dryRun, fwdataApi.EntryCount, projectSnapshot);
        fwdataApi.Save();

        if (!dryRun)
        {
            //note we are now using the crdt API, this avoids issues where some data isn't synced yet
            //later when we add the ability to sync that data we need the snapshot to reflect the synced state, not what was in the FW project
            //related to https://github.com/sillsdev/languageforge-lexbox/issues/1912
            await RegenerateProjectSnapshot(crdtApi, fwdataApi.Project);
        }
        return result;
    }

    private async Task<SyncResult> Sync(IMiniLcmApi crdtApi, IMiniLcmApi fwdataApi, bool dryRun, int entryCount, ProjectSnapshot? projectSnapshot)
    {
        crdtApi = normalizationWrapperFactory.Create(validationWrapperFactory.Create(crdtApi));
        fwdataApi = normalizationWrapperFactory.Create(validationWrapperFactory.Create(fwdataApi));

        if (dryRun)
        {
            crdtApi = new DryRunMiniLcmApi(crdtApi);
            fwdataApi = new DryRunMiniLcmApi(fwdataApi);
        }

        if (projectSnapshot is null)
        {
            await miniLcmImport.ImportProject(crdtApi, fwdataApi, entryCount);
            LogDryRun(crdtApi, "crdt");
            if (dryRun) return new DryRunSyncResult(entryCount, 0, GetDryRunRecords(crdtApi), []);
            return new SyncResult(entryCount, 0);
        }

        var currentFwDataWritingSystems = await fwdataApi.GetWritingSystems();
        var crdtChanges = await WritingSystemSync.Sync(projectSnapshot.WritingSystems, currentFwDataWritingSystems, crdtApi);
        var fwdataChanges = await WritingSystemSync.Sync(currentFwDataWritingSystems, await crdtApi.GetWritingSystems(), fwdataApi);

        var currentFwDataPublications = await fwdataApi.GetPublications().ToArrayAsync();
        crdtChanges += await PublicationSync.Sync(projectSnapshot.Publications, currentFwDataPublications, crdtApi);
        fwdataChanges += await PublicationSync.Sync(currentFwDataPublications, await crdtApi.GetPublications().ToArrayAsync(), fwdataApi);

        var currentFwDataPartsOfSpeech = await fwdataApi.GetPartsOfSpeech().ToArrayAsync();
        crdtChanges += await PartOfSpeechSync.Sync(projectSnapshot.PartsOfSpeech, currentFwDataPartsOfSpeech, crdtApi);
        fwdataChanges += await PartOfSpeechSync.Sync(currentFwDataPartsOfSpeech, await crdtApi.GetPartsOfSpeech().ToArrayAsync(), fwdataApi);

        var currentFwDataSemanticDomains = await fwdataApi.GetSemanticDomains().ToArrayAsync();
        crdtChanges += await SemanticDomainSync.Sync(projectSnapshot.SemanticDomains, currentFwDataSemanticDomains, crdtApi);
        fwdataChanges += await SemanticDomainSync.Sync(currentFwDataSemanticDomains, await crdtApi.GetSemanticDomains().ToArrayAsync(), fwdataApi);

        var currentFwDataComplexFormTypes = await fwdataApi.GetComplexFormTypes().ToArrayAsync();
        crdtChanges += await ComplexFormTypeSync.Sync(projectSnapshot.ComplexFormTypes, currentFwDataComplexFormTypes, crdtApi);
        fwdataChanges += await ComplexFormTypeSync.Sync(currentFwDataComplexFormTypes, await crdtApi.GetComplexFormTypes().ToArrayAsync(), fwdataApi);

        var currentFwDataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtChanges += await EntrySync.SyncFull(projectSnapshot.Entries, currentFwDataEntries, crdtApi);
        LogDryRun(crdtApi, "crdt");

        fwdataChanges += await EntrySync.SyncFull(currentFwDataEntries, await crdtApi.GetAllEntries().ToArrayAsync(), fwdataApi);
        LogDryRun(fwdataApi, "fwdata");

        //todo push crdt changes to lexbox
        if (dryRun) return new DryRunSyncResult(crdtChanges, fwdataChanges, GetDryRunRecords(crdtApi), GetDryRunRecords(fwdataApi));
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

    private List<DryRunMiniLcmApi.DryRunRecord> GetDryRunRecords(IMiniLcmApi api)
    {
        return ((DryRunMiniLcmApi)api).DryRunRecords;
    }

    public static async Task<ProjectSnapshot?> GetProjectSnapshot(FwDataProject project)
    {
        var snapshotPath = SnapshotPath(project);
        if (!File.Exists(snapshotPath)) return null;
        await using var file = File.OpenRead(snapshotPath);
        return await JsonSerializer.DeserializeAsync<ProjectSnapshot>(file);
    }

    public async Task RegenerateProjectSnapshot(IMiniLcmApi crdtApi, FwDataProject project)
    {
        if (crdtApi is not CrdtMiniLcmApi)
            throw new InvalidOperationException("CrdtApi must be of type CrdtMiniLcmApi to regenerate project snapshot.");
        await SaveProjectSnapshot(project, await crdtApi.TakeProjectSnapshot());
    }

    internal async Task SaveProjectSnapshot(FwDataProject project, ProjectSnapshot projectSnapshot)
    {
        var snapshotPath = SnapshotPath(project);
        await using var file = File.Create(snapshotPath);
        //not using our serialization options because we don't want to exclude MiniLcmInternal
        await JsonSerializer.SerializeAsync(file, projectSnapshot);
    }

    internal static string SnapshotPath(FwDataProject project)
    {
        var projectPath = project.ProjectsPath;
        var snapshotPath = Path.Combine(projectPath, $"{project.Name}_snapshot.json");
        return snapshotPath;
    }

    public static bool HasSyncedSuccessfully(FwDataProject project)
    {
        var snapshotPath = SnapshotPath(project);
        if (!File.Exists(snapshotPath)) return false;
        return new FileInfo(snapshotPath).Length > 0;
    }
}
