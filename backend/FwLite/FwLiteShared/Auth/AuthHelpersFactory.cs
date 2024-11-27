using System.Collections.Concurrent;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteShared.Auth;

public class AuthHelpersFactory(IServiceProvider provider,
    IOptions<AuthConfig> options,
    IRedirectUrlProvider? redirectUrlProvider,
    ILogger<AuthHelpersFactory> logger)
{
    private readonly ConcurrentDictionary<string, AuthHelpers> _helpers = new();

    private string AuthorityKey(LexboxServer server) => "AuthHelper|" + server.Authority.Authority;

    /// <summary>
    /// gets an Auth Helper for the given server
    /// </summary>
    public AuthHelpers GetHelper(LexboxServer server)
    {
        var helper = _helpers.GetOrAdd(AuthorityKey(server),
            static (host, arg) => ActivatorUtilities.CreateInstance<AuthHelpers>(arg.provider, arg.server),
            (server, provider));
        //an auth helper can get created based on the server host, however in development that will not be the same as the client host
        //so we need to recreate it if the host is not valid, this is only required when not using system web view login
        if (!options.Value.SystemWebViewLogin && redirectUrlProvider is not null && redirectUrlProvider.ShouldRecreateAuthHelper(helper.RedirectUrl))
        {
            logger.LogInformation("Recreating auth helper with Redirect Url {RedirectUrl}", helper.RedirectUrl);
            _helpers.TryRemove(AuthorityKey(server), out _);
            return GetHelper(server);
        }

        return helper;
    }

    /// <summary>
    /// get auth helper for a given project
    /// </summary>
    public AuthHelpers GetHelper(ProjectData project)
    {
        var originDomain = project.OriginDomain;
        if (string.IsNullOrEmpty(originDomain)) throw new InvalidOperationException("No origin domain in project data");
        return GetHelper(options.Value.GetServer(project));
    }
}
