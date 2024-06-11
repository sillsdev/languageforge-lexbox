using LocalWebApp.Auth;

namespace LocalWebApp.Services;

public class LexboxProjectService(AuthHelpersFactory helpersFactory, ILogger<LexboxProjectService> logger)
{
    public record LexboxCrdtProject(Guid Id, string Name);

    public async Task<LexboxCrdtProject[]> GetLexboxProjects()
    {
        var httpClient = await helpersFactory.GetDefault().CreateClient();
        if (httpClient is null) return [];
        try
        {
            return await httpClient.GetFromJsonAsync<LexboxCrdtProject[]>("api/crdt/listProjects") ?? [];
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Error getting lexbox projects");
            return [];
        }
    }
}
