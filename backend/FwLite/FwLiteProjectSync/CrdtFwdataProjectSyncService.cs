using System.Diagnostics;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using LexCore.Sync;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Models;
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
            : await SyncInternal(crdt, crdtApi, fwdataApi, projectSnapshot);

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

    private async Task<SyncResult> SyncInternal(CrdtMiniLcmApi crdt, IMiniLcmApi crdtApi, IMiniLcmApi fwdataApi, ProjectSnapshot projectSnapshot)
    {
        // Load FwData state once — used for both sync directions to ensure consistency
        var currentFwDataWritingSystems = await fwdataApi.GetWritingSystems();
        var currentFwDataPublications = await fwdataApi.GetPublications().ToArrayAsync();
        var currentFwDataPartsOfSpeech = await fwdataApi.GetPartsOfSpeech().ToArrayAsync();
        var currentFwDataSemanticDomains = await fwdataApi.GetSemanticDomains().ToArrayAsync();
        var currentFwDataComplexFormTypes = await fwdataApi.GetComplexFormTypes().ToArrayAsync();
        var currentFwDataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();

        // Sync metadata into CRDT (small number of changes — not worth deferring)
        var crdtChanges = await WritingSystemSync.Sync(projectSnapshot.WritingSystems, currentFwDataWritingSystems, crdtApi);
        crdtChanges += await PublicationSync.Sync(projectSnapshot.Publications, currentFwDataPublications, crdtApi);
        crdtChanges += await PartOfSpeechSync.Sync(projectSnapshot.PartsOfSpeech, currentFwDataPartsOfSpeech, crdtApi);
        crdtChanges += await SemanticDomainSync.Sync(projectSnapshot.SemanticDomains, currentFwDataSemanticDomains, crdtApi);
        crdtChanges += await ComplexFormTypeSync.Sync(projectSnapshot.ComplexFormTypes, currentFwDataComplexFormTypes, crdtApi);

        // Sync entries into CRDT (batched — bulk of changes, this is where the perf win is)
        {
            await using var batch = crdt.BeginBulkChangeBatch();
            crdtChanges += await EntrySync.SyncFull(projectSnapshot.Entries, currentFwDataEntries, crdtApi);
            logger.LogInformation("Flushing {Count} batched entry CRDT changes", batch.QueuedChangeCount);
        }

        // Phase 2: Sync crdt→fwdata changes into FwData
        // Uses the same FwData data loaded above for the "before" side,
        // and reads fresh CRDT state (now committed from Phase 1) for the "after" side.
        var fwdataChanges = 0;
        fwdataChanges += await WritingSystemSync.Sync(currentFwDataWritingSystems, await crdtApi.GetWritingSystems(), fwdataApi);
        fwdataChanges += await PublicationSync.Sync(currentFwDataPublications, await crdtApi.GetPublications().ToArrayAsync(), fwdataApi);
        fwdataChanges += await PartOfSpeechSync.Sync(currentFwDataPartsOfSpeech, await crdtApi.GetPartsOfSpeech().ToArrayAsync(), fwdataApi);
        fwdataChanges += await SemanticDomainSync.Sync(currentFwDataSemanticDomains, await crdtApi.GetSemanticDomains().ToArrayAsync(), fwdataApi);
        fwdataChanges += await ComplexFormTypeSync.Sync(currentFwDataComplexFormTypes, await crdtApi.GetComplexFormTypes().ToArrayAsync(), fwdataApi);
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
