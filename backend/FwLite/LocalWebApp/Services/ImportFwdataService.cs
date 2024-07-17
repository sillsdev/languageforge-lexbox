using System.Diagnostics;
#if !DISABLE_FW_BRIDGE
using FwDataMiniLcmBridge;
#endif
using Humanizer;
using LcmCrdt;
using MiniLcm;

namespace LocalWebApp.Services;

public class ImportFwdataService(ProjectsService projectsService, ILogger<ImportFwdataService> logger
    #if !DISABLE_FW_BRIDGE
    , FwDataFactory fwDataFactory
    #endif
    )
{
    public async Task<CrdtProject> Import(string projectName)
    {
        #if DISABLE_FW_BRIDGE
        throw new NotSupportedException("FW bridge is disabled");
        #else
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
            logger.LogInformation("Import of {ProjectName} complete, took {TimeSpend}", fwDataApi.Project.Name, timeSpent.Humanize(2));
            return project;

        }
        catch
        {
            logger.LogError("Import of {ProjectName} failed, deleting project", fwDataApi.Project.Name);
            throw;
        }
#endif
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

        await foreach (var partOfSpeech in importFrom.GetPartsOfSpeech())
        {
            await importTo.CreatePartOfSpeech(partOfSpeech);
            logger.LogInformation("Imported part of speech {Id}", partOfSpeech.Id);
        }


        var semanticDomains = importFrom.GetSemanticDomains();
        var entries = importFrom.GetEntries(new QueryOptions(Count: 100_000, Offset: 0));
        if (importTo is CrdtLexboxApi crdtLexboxApi)
        {
            logger.LogInformation("Importing semantic domains");
            await crdtLexboxApi.BulkImportSemanticDomains(semanticDomains.ToBlockingEnumerable());
            logger.LogInformation("Importing {Count} entries", entryCount);
            await crdtLexboxApi.BulkCreateEntries(entries);
        }
        else
        {
            await foreach (var semanticDomain in semanticDomains)
            {
                await importTo.CreateSemanticDomain(semanticDomain);
                logger.LogTrace("Imported semantic domain {Id}", semanticDomain.Id);
            }

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
