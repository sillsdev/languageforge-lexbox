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

    public async Task<Guid?> GetLexboxProjectId(string code)
    {
        var httpClient = await helpersFactory.GetDefault().CreateClient();
        if (httpClient is null) return null;
        try
        {
            return (await httpClient.GetFromJsonAsync<Guid?>($"api/crdt/lookupProjectId?code={code}"));
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Error getting lexbox project id");
            return null;
        }
    }
}
