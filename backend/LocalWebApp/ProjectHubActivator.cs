using LcmCrdt;
using Microsoft.AspNetCore.SignalR;

namespace LocalWebApp;

public class ProjectHubActivator<THub>(
    IServiceProvider serviceProvider,
    ProjectContext projectContext,
    IHttpContextAccessor contextAccessor,
    ProjectsService projectsService) : IHubActivator<THub> where THub : Hub
{
    private static readonly Lazy<ObjectFactory> ObjectFactory =
        new(() => ActivatorUtilities.CreateFactory(typeof(THub), Type.EmptyTypes));

    private bool? _created;

    private CrdtProject? GetProject()
    {
        if (contextAccessor.HttpContext is null) return null;
        var projectNameObj = contextAccessor.HttpContext.Request.RouteValues.GetValueOrDefault(LexboxApiHub.ProjectRouteKey, null);
        if (projectNameObj is null) return null;
        var projectName = projectNameObj.ToString();
        if (string.IsNullOrWhiteSpace(projectName)) return null;
        return projectsService.GetProject(projectName);
    }

    public THub Create()
    {
        // Project must be set before creating the hub, this is because the db context depends on the project to get its connection string
        projectContext.Project = GetProject();

        _created = false;
        var hub = serviceProvider.GetService<THub>();
        if (hub == null)
        {
            hub = (THub)ObjectFactory.Value(serviceProvider, []);
            _created = true;
        }

        return hub;
    }

    public void Release(THub hub)
    {
        ArgumentNullException.ThrowIfNull(hub);
        if (_created is true)
        {
            hub.Dispose();
        }
    }
}
