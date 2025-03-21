using LcmCrdt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

// TODO: Pick better name
public class ProjectContextFromIdService(IOptions<FwHeadlessConfig> config, CrdtProjectsService projectsService, ProjectLookupService projectLookupService, IMemoryCache memoryCache)
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
                    var project = new CrdtProject("crdt", crdtFile, memoryCache);
                    await context.RequestServices.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
                }
            }
        }
        await next();
    }
}
