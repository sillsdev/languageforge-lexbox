using System.Diagnostics;
using System.Text.Json.Serialization;
using FwLiteShared.Projects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FwLiteShared.Auth;

public record ServerStatus(string DisplayName, bool LoggedIn, string? LoggedInAs, LexboxServer Server);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoginResult
{
    Success,
    Offline,
    Cancelled,
}

public class AuthService(
    LexboxProjectService lexboxProjectService,
    OAuthClientFactory clientFactory,
    ILogger<AuthService> logger,
    IOptions<AuthConfig> options)
{
    [JSInvokable]
    public async Task<ServerStatus[]> Servers()
    {
        return await lexboxProjectService.Servers().ToAsyncEnumerable().Select(async (LexboxServer s, CancellationToken _) =>
        {
            var currentName = await clientFactory.GetClient(s).GetCurrentName();
            return new ServerStatus(s.DisplayName,
                !string.IsNullOrEmpty(currentName),
                currentName,
                s);
        }).ToArrayAsync();
    }

    [JSInvokable]
    public Task<LoginResult> SignInWebView(LexboxServer server) => SignInWebView(server, CancellationToken.None);

    // Not [JSInvokable]: a CancellationToken can't be marshaled from JS.
    public async Task<LoginResult> SignInWebView(LexboxServer server, CancellationToken cancellation)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await clientFactory.GetClient(server).SignIn(string.Empty, cancellation);//returnUrl does nothing here
            if (!result.HandledBySystemWebView) throw new InvalidOperationException("Sign in not handled by system web view");
            options.Value.AfterLoginWebView?.Invoke();
            logger.LogInformation("Web view sign in to {Server} succeeded after {Elapsed}",
                server.Authority, Stopwatch.GetElapsedTime(started));
            return LoginResult.Success;
        }
        catch (Exception e)
        {
            var classified = OAuthClient.ClassifyInteractiveLoginFailure(e);
            if (classified is null)
            {
                logger.LogError(e, "Web view sign in to {Server} failed after {Elapsed}",
                    server.Authority, Stopwatch.GetElapsedTime(started));
                throw;
            }
            logger.LogInformation(e, "Web view sign in to {Server} did not complete after {Elapsed}: {LoginResult}",
                server.Authority, Stopwatch.GetElapsedTime(started), classified);
            return classified.Value;
        }
    }

    [JSInvokable]
    public bool UseSystemWebView()
    {
        return options.Value.SystemWebViewLogin;
    }

    public async Task<string> SignInWebApp(LexboxServer server, string returnUrl)
    {
        var result = await clientFactory.GetClient(server).SignIn(returnUrl);
        if (result.HandledBySystemWebView) throw new InvalidOperationException("Sign in handled by system web view");
        if (result.AuthUri is null) throw new InvalidOperationException("AuthUri is null");
        return result.AuthUri.ToString();
    }

    [JSInvokable]
    public async Task Logout(LexboxServer server)
    {
        await clientFactory.GetClient(server).Logout();
    }

    public async Task<string?> GetLoggedInName(LexboxServer server)
    {
        return await clientFactory.GetClient(server).GetCurrentName();
    }
}
