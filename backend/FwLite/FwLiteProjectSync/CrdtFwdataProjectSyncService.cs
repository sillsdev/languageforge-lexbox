using System.Diagnostics;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using LexCore.Sync;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Normalization;
using MiniLcm.SyncHelpers;
using MiniLcm.Validators;

namespace FwLiteProjectSync;

public class CrdtFwdataProjectSyncService(MiniLcmImport miniLcmImport,
    ILogger<CrdtFwdataProjectSyncService> logger,
    MiniLcmApiValidationWrapperFactory validationWrapperFactory,
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
        return await SyncOrImportInternal(crdtApi, fwdataApi, dryRun, projectSnapshot);
    }

    public async Task<DryRunSyncResult> ImportDryRun(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi)
    {
        return (DryRunSyncResult)await Import(crdtApi, fwdataApi, true);
    }

    public virtual async Task<SyncResult> Import(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi, bool dryRun = false)
    {
        return await SyncOrImportInternal(crdtApi, fwdataApi, dryRun, projectSnapshot: null);
    }

    private async Task<SyncResult> SyncOrImportInternal(IMiniLcmApi crdtApi, IMiniLcmApi fwdataApi, bool dryRun, ProjectSnapshot? projectSnapshot)
    {
        using var activity = FwLiteProjectSyncActivitySource.Value.StartActivity();
        if (crdtApi is not CrdtMiniLcmApi crdt) // maybe the argument type should be changed?
            throw new InvalidOperationException("CrdtApi must be of type CrdtMiniLcmApi to sync.");
        if (fwdataApi is not FwDataMiniLcmApi fwdata) // maybe the argument type should be changed?
            throw new InvalidOperationException("FwdataApi must be of type FwDataMiniLcmApi to sync.");
        if (crdt.ProjectData.FwProjectId != fwdata.ProjectId)
        {
            activity?.SetStatus(ActivityStatusCode.Error, $"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdata.ProjectId}");
            throw new InvalidOperationException($"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdata.ProjectId}");
        }

        // Project snapshot logic/handling is done outside of this class so that Sync vs Import is explicit.
        // We still choose to explicitly verify a consistent state to avoid accidental misuse.
        var hasSyncedSuccessfully = ProjectSnapshotService.HasSyncedSuccessfully(fwdata.Project);
        if (hasSyncedSuccessfully != (projectSnapshot is not null))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Project sync state does not match presence of snapshot.");
            throw new InvalidOperationException("Project sync state does not match presence of snapshot.");
        }

        crdtApi = normalizationWrapperFactory.Create(validationWrapperFactory.Create(crdtApi));
        fwdataApi = normalizationWrapperFactory.Create(validationWrapperFactory.Create(fwdataApi));

        if (dryRun)
        {
            crdtApi = new DryRunMiniLcmApi(crdtApi);
            fwdataApi = new DryRunMiniLcmApi(fwdataApi);
        }

        if (projectSnapshot is not null)
        {
            // Repair any missing translation IDs before doing the full sync, so the sync doesn't have to deal with them
            var syncedIdCount = await CrdtRepairs.SyncMissingTranslationIds(projectSnapshot.Entries, fwdata, crdt, dryRun);
        }

        var syncResult = projectSnapshot is null
            ? await ImportInternal(crdtApi, fwdataApi, fwdata.EntryCount)
            : await SyncInternal(crdtApi, fwdataApi, projectSnapshot);

        if (!dryRun)
        {
            fwdata.Save();
            return syncResult;
        }

        LogDryRun(crdtApi, "crdt");
        LogDryRun(fwdataApi, "fwdata");
        return new DryRunSyncResult(syncResult.CrdtChanges, syncResult.FwdataChanges,
            GetDryRunRecords(crdtApi), GetDryRunRecords(fwdataApi));
    }

    private async Task<SyncResult> ImportInternal(IMiniLcmApi crdtApi, IMiniLcmApi fwdataApi, int entryCount)
    {
        await miniLcmImport.ImportProject(crdtApi, fwdataApi, entryCount);
        return new SyncResult(entryCount, 0);
    }

    private async Task<SyncResult> SyncInternal(IMiniLcmApi crdtApi, IMiniLcmApi fwdataApi, ProjectSnapshot projectSnapshot)
    {
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
        fwdataChanges += await EntrySync.SyncFull(currentFwDataEntries, await crdtApi.GetAllEntries().ToArrayAsync(), fwdataApi);

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
}
