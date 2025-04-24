using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.DependencyInjection;
using SIL.LCModel;

namespace LcmDebugger;

public static class Utils
{
    public static LcmCache? LoadProject(this IServiceProvider services, FwDataProject project)
    {
        var projectLoader = services.GetRequiredService<IProjectLoader>();
        var projectService = projectLoader.LoadCache(project);
        return projectService;
    }

    public static ILexEntry GetLexEntry(this IServiceProvider services, FwDataProject project, Guid entryId)
    {
        var cache = LoadProject(services, project) ?? throw new InvalidOperationException("Project not found.");
        var entryRepo = cache.ServiceLocator.GetInstance<ILexEntryRepository>() ?? throw new InvalidOperationException("Entry repository not found.");
        return entryRepo.GetObject(entryId) ?? throw new InvalidOperationException(message: "Entry not found.");
    }
}
