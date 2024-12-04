using FwLiteShared.Projects;
using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public class FwLiteProvider(CombinedProjectsService projectService)
{
    public CombinedProjectsService ProjectService { get; } = projectService;
}
