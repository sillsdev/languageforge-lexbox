using FwDataMiniLcmBridge;
using LocalWebApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;

namespace LocalWebApp.Routes;

public static class FwIntegrationRoutes
{
    public static IEndpointConventionBuilder MapFwIntegrationRoutes(this WebApplication app)
    {
        var group = app.MapGroup($"/api/fw/{{{FwDataMiniLcmHub.ProjectRouteKey}}}").WithOpenApi(
            operation =>
            {
                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = FwDataMiniLcmHub.ProjectRouteKey, In = ParameterLocation.Path, Required = true
                });
                return operation;
            });
        group.MapGet("/open/entry/{id}",
            async (FwDataProjectContext context, IHubContext<FwDataMiniLcmHub> hubContext, FwDataFactory factory, Guid id) =>
            {
                if (context.Project is null) return Results.BadRequest("No project is set in the context");
                await hubContext.Clients.Group(context.Project.Name).SendCoreAsync(nameof(ILexboxHubClient.OnProjectClosed), [CloseReason.Locked]);
                factory.CloseCurrentProject();
                //need to use redirect as a way to not trigger flex until after we have closed the project
                return Results.Redirect($"silfw://localhost/link?database={context.Project.Name}&tool=lexiconEdit&guid={id}");
            });
        return group;
    }
}
