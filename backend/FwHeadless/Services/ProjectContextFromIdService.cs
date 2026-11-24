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
        if (context.GetProjectId() is Guid projectId && await FindCrdtProject(projectId) is { } project)
        {
            await context.RequestServices.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
        }
        await next();
    }

    /// <summary>
    /// The headless harmony project for <paramref name="projectId"/>, or null when Lexbox doesn't know the project
    /// or FwHeadless holds no CRDT copy of it yet.
    /// </summary>
    private async Task<CrdtProject?> FindCrdtProject(Guid projectId)
    {
        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null) return null;
        var crdtFile = config.Value.GetCrdtFile(projectCode, projectId);
        if (!File.Exists(crdtFile)) return null;
        return new CrdtProject("crdt", crdtFile, projectDataCache);
    }

    /// <summary>
    /// Like <see cref="FindCrdtProject"/>, but also null until a merge has finished, since before that the CRDT copy
    /// may hold a partial import.
    /// </summary>
    public async Task<CrdtProject?> FindReadyCrdtProject(Guid projectId)
    {
        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null || !config.Value.IsHeadlessHarmonyProjectReady(projectCode, projectId)) return null;
        return new CrdtProject("crdt", config.Value.GetCrdtFile(projectCode, projectId), projectDataCache);
    }
}
