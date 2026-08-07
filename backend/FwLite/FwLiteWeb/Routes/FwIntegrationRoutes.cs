using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace FwLiteWeb.Routes;

public static class FwIntegrationRoutes
{
    public static IEndpointConventionBuilder MapFwIntegrationRoutes(this WebApplication app)
    {
        var group = app.MapGroup($"/api/fw/{{{RouteKeys.FwData}}}").AddOpenApiOperationTransformer(
            (operation, _, _) =>
            {
                operation.Parameters?.Add(new OpenApiParameter()
                {
                    Name = RouteKeys.FwData, In = ParameterLocation.Path, Required = true
                });
                return Task.CompletedTask;
            });
        group.MapGet("/link/entry/{id}",
            async ([FromServices] FwDataProjectContext context,
                [FromServices] FwDataFactory factory,
                Guid id) =>
            {
                if (context.Project is null) return Results.BadRequest("No project is set in the context");
                await factory.CloseProjectAsync(context.Project);
                var link = FwLink.ToEntry(id, context.Project.Name);
                return Results.Text(link, "text/plain");
            });
        return group;
    }
}
