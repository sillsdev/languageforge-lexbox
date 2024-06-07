using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using LcmCrdt;
using MiniLcm;

namespace LocalWebApp.Services;

public class ImportFwdataService(ProjectsService projectsService, ILogger<ImportFwdataService> logger, FwDataFactory fwDataFactory)
{
    public async Task<CrdtProject> Import(string projectName)
    {
        using var fwDataApi = fwDataFactory.GetFwDataMiniLcmApi(projectName, false);
        var project = await projectsService.CreateProject(Path.GetFileNameWithoutExtension(projectName),
            afterCreate: async (provider, project) =>
            {
                var crdtApi = provider.GetRequiredService<ILexboxApi>();
                await ImportProject(crdtApi, fwDataApi, fwDataApi.EntryCount);
            });
        logger.LogInformation("Import of {ProjectName} complete!", fwDataApi.Project.Name);
        return project;
    }

    async Task ImportProject(ILexboxApi importTo, ILexboxApi importFrom, int entryCount)
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

        var index = 0;
        await foreach (var entry in importFrom.GetEntries(new QueryOptions(Count: 100_000, Offset: 0)))
        {
            if (importTo is CrdtLexboxApi crdtLexboxApi)
            {
                await crdtLexboxApi.CreateEntryLite(entry);
            }
            else
            {
                await importTo.CreateEntry(entry);
            }

            logger.LogInformation("Imported entry, {Index} of {Count} {Id}", index++, entryCount, entry.Id);
        }
    }
}
