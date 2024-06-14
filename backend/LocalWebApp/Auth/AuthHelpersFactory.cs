using System.Collections.Concurrent;
using LcmCrdt;
using Microsoft.Extensions.Options;

namespace LocalWebApp.Auth;

public class AuthHelpersFactory(
    IServiceProvider provider,
    ProjectContext projectContext,
    IHttpContextAccessor contextAccessor,
    IOptions<AuthConfig> options)
{
    private readonly ConcurrentDictionary<string, AuthHelpers> _helpers = new();

    /// <summary>
    /// gets the default (as configured in the options) Auth Helper, usually for lexbox.org
    /// </summary>
    public AuthHelpers GetDefault()
    {
        return GetHelper(options.Value.DefaultAuthority);
    }

    private string AuthorityKey(Uri authority) =>
        authority.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped);

    /// <summary>
    /// gets an Auth Helper for the given authority
    /// </summary>
    /// <param name="authority">should include scheme, host and port, no path</param>
    public AuthHelpers GetHelper(Uri authority)
    {
        var helper = _helpers.GetOrAdd(AuthorityKey(authority),
            static (host, arg) => ActivatorUtilities.CreateInstance<AuthHelpers>(arg.provider, arg.authority),
            (authority, provider));
        //an auth helper can get created based on the server host, however in development that will not be the same as the client host
        //so we need to recreate it if the host is not valid
        if (!helper.IsHostUrlValid())
        {
            _helpers.TryRemove(AuthorityKey(authority), out _);
            return GetHelper(authority);
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
        return GetHelper(new Uri(originDomain));
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
