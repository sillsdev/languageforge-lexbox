using System.Diagnostics;
using FwLiteProjectSync;
using FwDataMiniLcmBridge;
using Humanizer;
using LcmCrdt;
using MiniLcm;

namespace LocalWebApp.Services;

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
            var project = await projectsService.CreateProject(new(fwDataProject.Name,
                SeedNewProjectData: false,
                AfterCreate: async (provider, project) =>
                {
                    using var fwDataApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, false);
                    var crdtApi = provider.GetRequiredService<ILexboxApi>();
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
