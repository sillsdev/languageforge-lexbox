using System.Collections.Concurrent;
using LcmCrdt;
using LocalWebApp.Services;
using Microsoft.Extensions.Options;

namespace LocalWebApp.Auth;

public class AuthHelpersFactory(
    IServiceProvider provider,
    ProjectContext projectContext,
    IOptions<AuthConfig> options,
    IHttpContextAccessor contextAccessor)
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
        //so we need to recreate it if the host is not valid
        if (!helper.IsHostUrlValid())
        {
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
        ;
        var originDomain = project.OriginDomain;
        if (string.IsNullOrEmpty(originDomain)) throw new InvalidOperationException("No origin domain in project data");
        return GetHelper(options.Value.GetServer(project));
    }

    /// <summary>
    /// get the auth helper for the current project, this method is used when trying to inject an AuthHelper into a service
    /// </summary>
    /// <exception cref="InvalidOperationException">when not in the context of a project (typically requests include the project name in the path)</exception>
    public AuthHelpers GetCurrentHelper()
    {
        if (projectContext.Project is null)
            throw new InvalidOperationException("No current project, probably not in a request context");
        var currentProjectService =
            contextAccessor.HttpContext?.RequestServices.GetRequiredService<CurrentProjectService>();
        if (currentProjectService is null) throw new InvalidOperationException("No current project service");
        return GetHelper(currentProjectService.ProjectData);
    }
}
