using LcmCrdt;
using LcmCrdt.Project;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

// TODO: Pick better name
public class ProjectContextFromIdService(IOptions<FwHeadlessConfig> config, IProjectLookupService projectLookupService, ProjectDataCache projectDataCache)
{
    public async Task PopulateProjectContext(HttpContext context, Func<Task> next)
    {
        if (context.GetProjectId() is Guid projectId)
        {
            var projectCode = await projectLookupService.GetProjectCode(projectId);
            if (projectCode is not null)
            {
                var crdtFile = config.Value.GetCrdtFile(projectCode, projectId);
                if (File.Exists(crdtFile))
                {
                    //the file can exist before a concurrent sync finishes creating the project; Try... no-ops in that window
                    var project = new CrdtProject("crdt", crdtFile, projectDataCache);
                    await context.RequestServices.GetRequiredService<CurrentProjectService>().TrySetupProjectContext(project);
                }
            }
        }
        await next();
    }
}
