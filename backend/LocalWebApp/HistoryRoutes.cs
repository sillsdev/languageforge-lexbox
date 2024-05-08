using Crdt;
using LcmCrdt;

namespace LocalWebApp;

public static class HistoryRoutes
{
    public static RouteHandlerBuilder MapHistoryRoutes(this WebApplication app)
    {
        return app.MapGet("/api/{project}/history/snapshot/{id}",
            async (Guid id, string project, ProjectsService projectsService, IServiceProvider serviceProvider) =>
            {
                projectsService.SetProjectScope(projectsService.GetProject(project) ??
                                                throw new InvalidOperationException("Project not found"));
                var dataModel = serviceProvider.GetRequiredService<DataModel>();
                return await dataModel.GetLatestSnapshotByObjectId(id);
            });
    }
}
