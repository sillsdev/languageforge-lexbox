using System.Diagnostics;
using FwDataMiniLcmBridge;
using FwLiteProjectSync.Import;
using Humanizer;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Import;
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

    public async Task ImportProject(IMiniLcmApi importTo, IMiniLcmReadApi importFrom, int entryCount)
    {
        using var activity = FwLiteProjectSyncActivitySource.Value.StartActivity();
        logger.LogInformation("Importing {Count} entries", entryCount);
        // ResumableImportApi dedupes creates by object Id so a crashed import can be re-run; it wraps the
        // destination, so every write ProjectImporter makes goes through it.
        var resumableImportTo = new ResumableImportApi(importTo);
        await ProjectImporter.ImportData(resumableImportTo, await importFrom.TakeProjectSnapshot());

        activity?.SetTag("app.import.entries", entryCount);
        logger.LogInformation("Imported {Count} entries", entryCount);
    }
}
