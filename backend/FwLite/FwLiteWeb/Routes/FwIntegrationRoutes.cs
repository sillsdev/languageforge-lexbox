using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteWeb.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;

namespace FwLiteWeb.Routes;

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
        group.MapGet("/link/entry/{id}",
            async ([FromServices] FwDataProjectContext context,
                [FromServices] IHubContext<FwDataMiniLcmHub, ILexboxHubClient> hubContext,
                [FromServices] FwDataFactory factory,
                Guid id) =>
            {
                if (context.Project is null) return Results.BadRequest("No project is set in the context");
                await hubContext.Clients.Group(context.Project.Name).OnProjectClosed(CloseReason.Locked);
                factory.CloseProject(context.Project);
                var link = FwLink.ToEntry(id, context.Project.Name);
                return Results.Text(link, "text/plain");
            });
        return group;
    }
}
