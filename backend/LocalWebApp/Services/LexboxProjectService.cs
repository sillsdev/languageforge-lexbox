using LocalWebApp.Auth;

namespace LocalWebApp.Services;

public class LexboxProjectService(AuthHelpersFactory helpersFactory)
{
    public record LexboxCrdtProject(Guid Id, string Name);

    public async Task<LexboxCrdtProject[]> GetLexboxProjects()
    {
        var httpClient = await helpersFactory.GetDefault().CreateClient();
        if (httpClient is null) return [];
        return await httpClient.GetFromJsonAsync<LexboxCrdtProject[]>("api/crdt/listProjects") ?? [];
    }
}
