using LocalWebApp.Auth;
using Microsoft.Extensions.Options;

namespace LocalWebApp.Services;

public class LexboxProjectService(AuthHelpersFactory helpersFactory, ILogger<LexboxProjectService> logger)
{
    public record LexboxCrdtProject(Guid Id, string Name);

    public async Task<LexboxCrdtProject[]> GetLexboxProjects(LexboxServer server)
    {
        var httpClient = await helpersFactory.GetHelper(server).CreateClient();
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

    public async Task<Guid?> GetLexboxProjectId(LexboxServer server, string code)
    {
        var httpClient = await helpersFactory.GetHelper(server).CreateClient();
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
