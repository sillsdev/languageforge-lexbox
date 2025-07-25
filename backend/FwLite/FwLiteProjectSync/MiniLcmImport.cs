using System.Diagnostics;
using FwDataMiniLcmBridge;
using FwLiteProjectSync.Import;
using Humanizer;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.Project;

namespace FwLiteProjectSync;

public class MiniLcmImport(
    ILogger<MiniLcmImport> logger,
    FwDataFactory fwDataFactory,
    CrdtProjectsService crdtProjectsService
    ) : IProjectImport
{
    public async Task<IProjectIdentifier> Import(IProjectIdentifier project)
    {
        if (project is not FwDataProject fwDataProject) throw new ArgumentException("Project is not a fwdata project");
        var startTime = Stopwatch.GetTimestamp();
        try
        {
            using var fwDataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, false);
            var harmonyProject = await crdtProjectsService.CreateProject(new(fwDataProject.Name,
                fwDataProject.Name,
                SeedNewProjectData: false,
                FwProjectId: fwDataApi.ProjectId,
                AfterCreate: async (provider, _) =>
                {
                    var crdtApi = provider.GetRequiredService<IMiniLcmApi>();
                    await ImportProject(crdtApi, fwDataApi, fwDataApi.EntryCount);
                }));
            var timeSpent = Stopwatch.GetElapsedTime(startTime);
            logger.LogInformation("Import of {ProjectName} complete, took {TimeSpend}",
                fwDataProject.Name,
                timeSpent.Humanize(2));
            return harmonyProject;
        }
        catch
        {
            logger.LogError("Import of {ProjectName} failed, deleting project", fwDataProject.Name);
            throw;
        }
    }

    public async Task ImportProject(IMiniLcmApi importTo, IMiniLcmApi importFrom, int entryCount)
    {
        using var activity = FwLiteProjectSyncActivitySource.Value.StartActivity();
        importTo = new ResumableImportApi(importTo);
        await ImportWritingSystems(importTo, importFrom);

        await foreach (var partOfSpeech in importFrom.GetPartsOfSpeech())
        {
            await importTo.CreatePartOfSpeech(partOfSpeech);
            logger.LogInformation("Imported part of speech {Id}", partOfSpeech.Id);
        }

        await foreach (var publication in importFrom.GetPublications())
        {
            await importTo.CreatePublication(publication);
            logger.LogInformation("Imported part of speech {Id}", publication.Id);
        }

        await foreach (var complexFormType in importFrom.GetComplexFormTypes())
        {
            await importTo.CreateComplexFormType(complexFormType);
            logger.LogInformation("Imported complex form type {Id}", complexFormType.Id);
        }

        logger.LogInformation("Importing semantic domains");
        await importTo.BulkImportSemanticDomains(importFrom.GetSemanticDomains());
        logger.LogInformation("Importing {Count} entries", entryCount);
        await importTo.BulkCreateEntries(importFrom.GetAllEntries());


        activity?.SetTag("app.import.entries", entryCount);
        logger.LogInformation("Imported {Count} entries", entryCount);
    }

    internal async Task ImportWritingSystems(IMiniLcmApi importTo, IMiniLcmApi importFrom)
    {
        var writingSystems = await importFrom.GetWritingSystems();
        foreach (var ws in writingSystems.Analysis)
        {
            await importTo.CreateWritingSystem(ws);
            logger.LogInformation("Imported ws {WsId}", ws.WsId);
        }

        foreach (var ws in writingSystems.Vernacular)
        {
            await importTo.CreateWritingSystem(ws);
            logger.LogInformation("Imported ws {WsId}", ws.WsId);
        }
    }
}
