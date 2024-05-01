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

    public THub Create()
    {
        projectContext.Project = projectsService.GetProject(contextAccessor.HttpContext);
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
