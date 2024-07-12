using System.Diagnostics;
using FwDataMiniLcmBridge;
using Humanizer;
using LcmCrdt;
using MiniLcm;

namespace LocalWebApp.Services;

public class ImportFwdataService(ProjectsService projectsService, ILogger<ImportFwdataService> logger, FwDataFactory fwDataFactory)
{
    public async Task<CrdtProject> Import(string projectName)
    {
        var startTime = Stopwatch.GetTimestamp();
        var fwDataProject = FieldWorksProjectList.GetProject(projectName);
        if (fwDataProject is null)
        {
            throw new InvalidOperationException($"Project {projectName} not found.");
        }
        using var fwDataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, false);
        try
        {
            var project = await projectsService.CreateProject(fwDataProject.Name,
                afterCreate: async (provider, project) =>
                {
                    var crdtApi = provider.GetRequiredService<ILexboxApi>();
                    await ImportProject(crdtApi, fwDataApi, fwDataApi.EntryCount);
                });
            var timeSpent = Stopwatch.GetElapsedTime(startTime);
            logger.LogInformation("Import of {ProjectName} complete, took {TimeSpend}", fwDataApi.Project.Name, timeSpent.Humanize());
            return project;
        }
        catch
        {
            logger.LogError("Import of {ProjectName} failed, deleting project", fwDataApi.Project.Name);
            throw;
        }
    }

    private async Task ImportProject(ILexboxApi importTo, ILexboxApi importFrom, int entryCount)
    {
        var writingSystems = await importFrom.GetWritingSystems();
        foreach (var ws in writingSystems.Analysis)
        {
            await importTo.CreateWritingSystem(WritingSystemType.Analysis, ws);
            logger.LogInformation("Imported ws {WsId}", ws.Id);
        }

        foreach (var ws in writingSystems.Vernacular)
        {
            await importTo.CreateWritingSystem(WritingSystemType.Vernacular, ws);
            logger.LogInformation("Imported ws {WsId}", ws.Id);
        }

        await foreach (var semanticDomain in importFrom.GetSemanticDomains())
        {
            await importTo.CreateSemanticDomain(semanticDomain);
            logger.LogTrace("Imported semantic domain {Id}", semanticDomain.Id);
        }
        await foreach (var partOfSpeech in importFrom.GetPartsOfSpeech())
        {
            await importTo.CreatePartOfSpeech(partOfSpeech);
            logger.LogInformation("Imported part of speech {Id}", partOfSpeech.Id);
        }

        var entries = importFrom.GetEntries(new QueryOptions(Count: 100_000, Offset: 0));
        if (importTo is CrdtLexboxApi crdtLexboxApi)
        {
            await crdtLexboxApi.BulkCreateEntries(entries);
        }
        else
        {
            var index = 0;
            await foreach (var entry in entries)
            {
                await importTo.CreateEntry(entry);
                logger.LogTrace("Imported entry, {Index} of {Count} {Id}", index++, entryCount, entry.Id);
            }
        }
        logger.LogInformation("Imported {Count} entries", entryCount);
    }
}
