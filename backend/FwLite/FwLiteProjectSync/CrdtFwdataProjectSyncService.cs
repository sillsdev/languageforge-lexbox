using System.Diagnostics;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using LexCore.Sync;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Normalization;
using MiniLcm.SyncHelpers;
using MiniLcm.Validators;

namespace FwLiteProjectSync;

public class CrdtFwdataProjectSyncService(MiniLcmImport miniLcmImport, ProjectSnapshotService projectSnapshotService,
    ILogger<CrdtFwdataProjectSyncService> logger, MiniLcmApiValidationWrapperFactory validationWrapperFactory,
    MiniLcmApiStringNormalizationWrapperFactory normalizationWrapperFactory)
{
    public record DryRunSyncResult(
        int CrdtChanges,
        int FwdataChanges,
        List<DryRunMiniLcmApi.DryRunRecord> CrdtDryRunRecords,
        List<DryRunMiniLcmApi.DryRunRecord> FwDataDryRunRecords) : SyncResult(CrdtChanges, FwdataChanges);

    public async Task<DryRunSyncResult> SyncDryRun(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi, ProjectSnapshot projectSnapshot)
    {
        return (DryRunSyncResult)await Sync(crdtApi, fwdataApi, projectSnapshot, true);
    }

    public virtual async Task<SyncResult> Sync(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi, ProjectSnapshot projectSnapshot, bool dryRun = false)
    {
        using var activity = FwLiteProjectSyncActivitySource.Value.StartActivity();
        if (crdtApi is not CrdtMiniLcmApi crdt) // maybe the argument type should be changed?
            throw new InvalidOperationException("CrdtApi must be of type CrdtMiniLcmApi to sync.");
        if (crdt.ProjectData.FwProjectId != fwdataApi.ProjectId)
        {
            activity?.SetStatus(ActivityStatusCode.Error, $"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdataApi.ProjectId}");
            throw new InvalidOperationException($"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdataApi.ProjectId}");
        }

        // Repair any missing translation IDs before doing the full sync, so the sync doesn't have to deal with them
        var syncedIdCount = await CrdtRepairs.SyncMissingTranslationIds(projectSnapshot.Entries, fwdataApi, crdt, dryRun);

        SyncResult result = await Sync(crdtApi, fwdataApi, dryRun, projectSnapshot);
        fwdataApi.Save();
        return result;
    }

    public virtual async Task<SyncResult> Import(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi, bool dryRun = false, bool keepSnapshotBackup = false)
    {
        using var activity = FwLiteProjectSyncActivitySource.Value.StartActivity();
        if (crdtApi is not CrdtMiniLcmApi crdt) // maybe the argument type should be changed?
            throw new InvalidOperationException("CrdtApi must be of type CrdtMiniLcmApi to import.");
        if (crdt.ProjectData.FwProjectId != fwdataApi.ProjectId)
        {
            activity?.SetStatus(ActivityStatusCode.Error, $"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdataApi.ProjectId}");
            throw new InvalidOperationException($"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdataApi.ProjectId}");
        }

        var result = await ImportInternal(crdtApi, fwdataApi, dryRun, fwdataApi.EntryCount);
        fwdataApi.Save();
        if (!dryRun)
        {
            await projectSnapshotService.RegenerateProjectSnapshot(crdtApi, fwdataApi.Project, keepSnapshotBackup);
        }
        return result;
    }

    private async Task<SyncResult> Sync(IMiniLcmApi crdtApi, IMiniLcmApi fwdataApi, bool dryRun, ProjectSnapshot projectSnapshot)
    {
        crdtApi = normalizationWrapperFactory.Create(validationWrapperFactory.Create(crdtApi));
        fwdataApi = normalizationWrapperFactory.Create(validationWrapperFactory.Create(fwdataApi));

        if (dryRun)
        {
            crdtApi = new DryRunMiniLcmApi(crdtApi);
            fwdataApi = new DryRunMiniLcmApi(fwdataApi);
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

    private async Task<SyncResult> ImportInternal(IMiniLcmApi crdtApi, IMiniLcmApi fwdataApi, bool dryRun, int entryCount)
    {
        crdtApi = normalizationWrapperFactory.Create(validationWrapperFactory.Create(crdtApi));
        fwdataApi = normalizationWrapperFactory.Create(validationWrapperFactory.Create(fwdataApi));

        if (dryRun)
        {
            crdtApi = new DryRunMiniLcmApi(crdtApi);
            fwdataApi = new DryRunMiniLcmApi(fwdataApi);
        }

        await miniLcmImport.ImportProject(crdtApi, fwdataApi, entryCount);
        LogDryRun(crdtApi, "crdt");
        if (dryRun) return new DryRunSyncResult(entryCount, 0, GetDryRunRecords(crdtApi), []);
        return new SyncResult(entryCount, 0);
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

    public static bool HasSyncedSuccessfully(FwDataProject project)
        => ProjectSnapshotService.HasSyncedSuccessfully(project);
}
