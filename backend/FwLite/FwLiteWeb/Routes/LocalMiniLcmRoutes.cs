using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;
using MiniLcm.Models;
using MiniLcm.Project;
using MiniLcm.Wrappers;
using MiniLcmWeb;
using System.Text.Json.Nodes;

namespace FwLiteWeb.Routes;

/// <summary>
/// Maps the shared MiniLcm routes for local projects, addressed by data format and code.
/// </summary>
public static class LocalMiniLcmRoutes
{
    public static IEndpointConventionBuilder MapLocalMiniLcmRoutes(this IEndpointRouteBuilder app, [StringSyntax("route")] string prefix)
    {
        return app.MapMiniLcmRoutes(prefix + "/{projectType}/{projectCode}", ResolveProject)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                operation.Parameters?.Add(new OpenApiParameter()
                {
                    Name = "projectType",
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema()
                    {
                        Enum =
                        [
                            JsonValue.Create(ProjectDataFormat.FwData.ToString()),
                            JsonValue.Create(ProjectDataFormat.Harmony.ToString())
                        ],
                        Type = JsonSchemaType.String
                    },
                });
                operation.Parameters?.Add(new OpenApiParameter()
                {
                    Name = "projectCode",
                    In = ParameterLocation.Path,
                    Required = true
                });
                return Task.CompletedTask;
            });
    }

    private static async ValueTask<MiniLcmApiResolution> ResolveProject(HttpContext context)
    {
        var typeString = context.Request.RouteValues.GetValueOrDefault("projectType")?.ToString();
        if (!Enum.TryParse<ProjectDataFormat>(typeString, out var type))
        {
            return MiniLcmApiResolution.Failed(Results.Problem($"Invalid project type '{typeString}'",
                statusCode: StatusCodes.Status400BadRequest));
        }

        var projectCode = context.Request.RouteValues.GetValueOrDefault("projectCode")?.ToString();
        if (string.IsNullOrWhiteSpace(projectCode))
        {
            return MiniLcmApiResolution.Failed(Results.Problem("Project code is required",
                statusCode: StatusCodes.Status400BadRequest));
        }

        var services = context.RequestServices;
        var projectProvider = services.GetServices<IProjectProvider>().FirstOrDefault(p => p.DataFormat == type)
            ?? throw new InvalidOperationException($"No project provider found for {type}");

        var project = projectProvider.GetProject(projectCode);
        if (project is null)
        {
            return MiniLcmApiResolution.Failed(Results.Problem($"Project {projectCode} not found",
                statusCode: StatusCodes.Status404NotFound));
        }

        var userFacingWrappers = services.GetRequiredService<MiniLcmApiUserFacingWrappers>();
        return MiniLcmApiResolution.Resolved(userFacingWrappers.Apply(
            await projectProvider.OpenProject(project, services), project));
    }
}
