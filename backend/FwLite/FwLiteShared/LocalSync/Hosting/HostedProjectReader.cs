using LcmCrdt;
using LcmCrdt.LocalSync;
using Microsoft.Extensions.DependencyInjection;

namespace FwLiteShared.LocalSync.Hosting;

internal static class HostedProjectReader
{
    /// <summary>Reads the hosted project as it is now, in a new scope for it.</summary>
    public static async Task<HostedProject> Read(IServiceProvider services, CrdtProject project)
    {
        await using var scope = services.CreateAsyncScope();
        var projectData = await scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
        var identity = await scope.ServiceProvider.GetRequiredService<ProjectIdentityService>().GetIdentity();
        return new HostedProject(projectData.Id, projectData.Code, projectData.Name, projectData.OriginDomain,
            identity.RootCommitId, [.. identity.WritingSystemIds]);
    }
}
