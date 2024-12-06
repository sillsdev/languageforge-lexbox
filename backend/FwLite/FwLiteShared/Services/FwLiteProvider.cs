using System.Text.Json.Serialization;
using FwLiteShared.Auth;
using FwLiteShared.Projects;

namespace FwLiteShared.Services;

public class FwLiteProvider(
    CombinedProjectsService projectService,
    AuthService authService,
    ImportFwdataService importFwdataService
)
{
    public Dictionary<DotnetService, object> GetServices()
    {
        return Enum.GetValues<DotnetService>().ToDictionary(s => s, GetService);
    }

    public object GetService(DotnetService service)
    {
        return service switch
        {
            DotnetService.CombinedProjectsService => projectService,
            DotnetService.AuthService => authService,
            DotnetService.ImportFwdataService => importFwdataService,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
        };
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DotnetService
{
    CombinedProjectsService,
    AuthService,
    ImportFwdataService,
}
