using LcmCrdt;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

// TODO: Pick better name
public class ProjectContextFromIdService(IOptions<FwHeadlessConfig> config, CrdtProjectsService projectsService, ProjectLookupService projectLookupService)
{
    public async Task PopulateProjectContext(HttpContext context, Func<Task> next)
    {
        if (context.GetProjectId() is Guid projectId)
        {
            var projectCode = await projectLookupService.GetProjectCode(projectId);
            if (projectCode is not null)
            {
                var projectFolder = Path.Join(config.Value.ProjectStorageRoot, $"{projectCode}-{projectId}");
                var crdtFile = Path.Join(projectFolder, "crdt.sqlite");
                if (File.Exists(crdtFile))
                {
                    var project = new CrdtProject("crdt", crdtFile);
                    projectsService.SetProjectScope(project);
                    await context.RequestServices.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
                }
            }
        }
        await next();
    }
}
