using FwLiteShared.Projects;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FwLiteShared.Auth;

public record ServerStatus(string DisplayName, bool LoggedIn, string? LoggedInAs, LexboxServer Server);
public class AuthService(LexboxProjectService lexboxProjectService, OAuthClientFactory clientFactory, IOptions<AuthConfig> options)
{
    [JSInvokable]
    public async Task<ServerStatus[]> Servers()
    {
        return await lexboxProjectService.Servers().ToAsyncEnumerable().SelectAwait(async s =>
        {
            var currentName = await clientFactory.GetClient(s).GetCurrentName();
            return new ServerStatus(s.DisplayName,
                !string.IsNullOrEmpty(currentName),
                currentName,
                s);
        }).ToArrayAsync();
    }

    public async Task SignInWebView(LexboxServer server)
    {
        var result = await clientFactory.GetClient(server).SignIn(string.Empty);//does nothing here
        if (!result.HandledBySystemWebView) throw new InvalidOperationException("Sign in not handled by system web view");
    }

    public async Task<string> SignInWebApp(LexboxServer server, string returnUrl)
    {
        var result = await clientFactory.GetClient(server).SignIn(returnUrl);
        if (result.HandledBySystemWebView) throw new InvalidOperationException("Sign in handled by system web view");
        if (result.AuthUri is null) throw new InvalidOperationException("AuthUri is null");
        return result.AuthUri.ToString();
    }

    public async Task Logout(LexboxServer server)
    {
        await clientFactory.GetClient(server).Logout();
    }

    public async Task<string?> GetLoggedInName(LexboxServer server)
    {
        return await clientFactory.GetClient(server).GetCurrentName();
    }
}
