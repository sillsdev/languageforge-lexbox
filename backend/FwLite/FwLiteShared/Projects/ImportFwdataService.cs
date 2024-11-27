using System.Diagnostics;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using Humanizer;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniLcm;

namespace FwLiteShared.Projects;

public class ImportFwdataService(
    ProjectsService projectsService,
    ILogger<ImportFwdataService> logger,
    FwDataFactory fwDataFactory,
    FieldWorksProjectList fieldWorksProjectList,
    MiniLcmImport miniLcmImport
)
{
    public async Task<CrdtProject> Import(string projectName)
    {
        var startTime = Stopwatch.GetTimestamp();
        var fwDataProject = fieldWorksProjectList.GetProject(projectName);
        if (fwDataProject is null)
        {
            throw new InvalidOperationException($"Project {projectName} not found.");
        }
        try
        {
            using var fwDataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, false);
            var project = await projectsService.CreateProject(new(fwDataProject.Name,
                SeedNewProjectData: false,
                FwProjectId: fwDataApi.ProjectId,
                AfterCreate: async (provider, project) =>
                {
                    var crdtApi = provider.GetRequiredService<IMiniLcmApi>();
                    await miniLcmImport.ImportProject(crdtApi, fwDataApi, fwDataApi.EntryCount);
                }));
            var timeSpent = Stopwatch.GetElapsedTime(startTime);
            logger.LogInformation("Import of {ProjectName} complete, took {TimeSpend}", fwDataProject.Name, timeSpent.Humanize(2));
            return project;
        }
        catch
        {
            logger.LogError("Import of {ProjectName} failed, deleting project", fwDataProject.Name);
            throw;
        }
    }

}
