﻿using System.Text.Json;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using LexCore.Sync;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync;

public class CrdtFwdataProjectSyncService(MiniLcmImport miniLcmImport, ILogger<CrdtFwdataProjectSyncService> logger)
{
    public record DryRunSyncResult(
        int CrdtChanges,
        int FwdataChanges,
        List<DryRunMiniLcmApi.DryRunRecord> CrdtDryRunRecords,
        List<DryRunMiniLcmApi.DryRunRecord> FwDataDryRunRecords) : SyncResult(CrdtChanges, FwdataChanges);

    public async Task<DryRunSyncResult> SyncDryRun(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi)
    {
        return (DryRunSyncResult) await Sync(crdtApi, fwdataApi, true);
    }

    public async Task<SyncResult> Sync(IMiniLcmApi crdtApi, FwDataMiniLcmApi fwdataApi, bool dryRun = false)
    {
        if (crdtApi is CrdtMiniLcmApi crdt && crdt.ProjectData.FwProjectId != fwdataApi.ProjectId)
        {
            throw new InvalidOperationException($"Project id mismatch, CRDT Id: {crdt.ProjectData.FwProjectId}, FWData Id: {fwdataApi.ProjectId}");
        }
        var projectSnapshot = await GetProjectSnapshot(fwdataApi.Project);
        SyncResult result = await Sync(crdtApi, fwdataApi, dryRun, fwdataApi.EntryCount, projectSnapshot);
        fwdataApi.Save();

        if (!dryRun)
        {
            await SaveProjectSnapshot(fwdataApi.Project,
                new ProjectSnapshot(
                    await fwdataApi.GetAllEntries().ToArrayAsync(),
                    await fwdataApi.GetPartsOfSpeech().ToArrayAsync(),
                    await fwdataApi.GetSemanticDomains().ToArrayAsync(),
                    await fwdataApi.GetComplexFormTypes().ToArrayAsync(),
                    await fwdataApi.GetWritingSystems()));
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
            if (dryRun) return new DryRunSyncResult(entryCount, 0, GetDryRunRecords(crdtApi), []);
            return new SyncResult(entryCount, 0);
        }

        var currentFwDataWritingSystems = await fwdataApi.GetWritingSystems();
        var crdtChanges = await WritingSystemSync.Sync(currentFwDataWritingSystems, projectSnapshot.WritingSystems, crdtApi);
        var fwdataChanges = await WritingSystemSync.Sync(await crdtApi.GetWritingSystems(), currentFwDataWritingSystems, fwdataApi);

        var currentFwDataPartsOfSpeech = await fwdataApi.GetPartsOfSpeech().ToArrayAsync();
        crdtChanges += await PartOfSpeechSync.Sync(currentFwDataPartsOfSpeech, projectSnapshot.PartsOfSpeech, crdtApi);
        fwdataChanges += await PartOfSpeechSync.Sync(await crdtApi.GetPartsOfSpeech().ToArrayAsync(), currentFwDataPartsOfSpeech, fwdataApi);

        var currentFwDataSemanticDomains = await fwdataApi.GetSemanticDomains().ToArrayAsync();
        crdtChanges += await SemanticDomainSync.Sync(currentFwDataSemanticDomains, projectSnapshot.SemanticDomains, crdtApi);
        fwdataChanges += await SemanticDomainSync.Sync(await crdtApi.GetSemanticDomains().ToArrayAsync(), currentFwDataSemanticDomains, fwdataApi);

        var currentFwDataComplexFormTypes = await fwdataApi.GetComplexFormTypes().ToArrayAsync();
        crdtChanges += await ComplexFormTypeSync.Sync(currentFwDataComplexFormTypes, projectSnapshot.ComplexFormTypes, crdtApi);
        fwdataChanges += await ComplexFormTypeSync.Sync(await crdtApi.GetComplexFormTypes().ToArrayAsync(), currentFwDataComplexFormTypes, fwdataApi);

        var currentFwDataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtChanges += await EntrySync.Sync(currentFwDataEntries, projectSnapshot.Entries, crdtApi);
        LogDryRun(crdtApi, "crdt");

        fwdataChanges += await EntrySync.Sync(await crdtApi.GetAllEntries().ToArrayAsync(), currentFwDataEntries, fwdataApi);
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

    public record ProjectSnapshot(
        Entry[] Entries,
        PartOfSpeech[] PartsOfSpeech,
        SemanticDomain[] SemanticDomains,
        ComplexFormType[] ComplexFormTypes,
        WritingSystems WritingSystems)
    {
        internal static ProjectSnapshot Empty { get; } = new([], [], [], [], new WritingSystems());
    }

    private async Task<ProjectSnapshot?> GetProjectSnapshot(FwDataProject project)
    {
        var snapshotPath = SnapshotPath(project);
        if (!File.Exists(snapshotPath)) return null;
        await using var file = File.OpenRead(snapshotPath);
        return await JsonSerializer.DeserializeAsync<ProjectSnapshot>(file);
    }

    internal async Task SaveProjectSnapshot(FwDataProject project, ProjectSnapshot projectSnapshot)
    {
        var snapshotPath = SnapshotPath(project);
        await using var file = File.Create(snapshotPath);
        await JsonSerializer.SerializeAsync(file, projectSnapshot);
    }

    internal static string SnapshotPath(FwDataProject project)
    {
        var projectPath = project.ProjectsPath;
        var snapshotPath = Path.Combine(projectPath, $"{project.Name}_snapshot.json");
        return snapshotPath;
    }
}
