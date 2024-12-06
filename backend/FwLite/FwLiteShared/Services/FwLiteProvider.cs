using System.Text.Json.Serialization;
using FwLiteShared.Projects;

namespace FwLiteShared.Services;

public class FwLiteProvider(CombinedProjectsService projectService)
{
    public CombinedProjectsService ProjectService { get; } = projectService;

    public Dictionary<DotnetService, object> GetServices()
    {
        return Enum.GetValues<DotnetService>().ToDictionary(s => s, GetService);
    }

    public object GetService(DotnetService service)
    {
        return service switch
        {
            DotnetService.CombinedProjectsService => ProjectService,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
        };
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DotnetService
{
    CombinedProjectsService
}
