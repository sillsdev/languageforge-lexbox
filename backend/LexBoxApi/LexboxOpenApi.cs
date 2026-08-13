using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace LexBoxApi;

public static class LexboxOpenApi
{
    public const string OpenApiPrivateDocumentName = "v1";
    public const string OpenApiPublicDocumentName = "public";

    /// <summary>
    /// Path (relative to the LexBoxApi project/content root) of the committed public OpenAPI schema (YAML, for
    /// readable diffs). CI regenerates this and fails if it drifts from what's committed, see
    /// `task api:generate-openapi-schema`.
    /// </summary>
    public const string PublicSchemaPath = "openapi/public.yaml";

    public static bool IsSchemaGenerationRequest(string[] args) => args is ["generate-openapi-schema"];

    /// <summary>
    /// Drops the app's infrastructure-bound hosted services (EF migrations, Quartz, hg setup, …) while keeping
    /// the web host service that builds endpoint routing. This lets the app start far enough to materialize the
    /// endpoint data source (so minimal API endpoints are discovered) without needing a database or other
    /// infrastructure. Call before <c>builder.Build()</c>, only when generating the schema.
    /// </summary>
    public static void RemoveInfrastructureHostedServices(IServiceCollection services)
    {
        var toRemove = services
            .Where(d => d.ServiceType == typeof(IHostedService)
                        && d.ImplementationType?.FullName != "Microsoft.AspNetCore.Hosting.GenericWebHostService")
            .ToList();
        foreach (var descriptor in toRemove) services.Remove(descriptor);
    }

    /// <summary>
    /// Starts the app (minimal API endpoints are only discoverable once endpoint routing is materialized at
    /// startup), writes the public OpenAPI document to <see cref="PublicSchemaPath"/>, then stops. Pair with
    /// <see cref="RemoveInfrastructureHostedServices"/> so no external infrastructure is required.
    /// </summary>
    public static async Task GenerateSchema(WebApplication app)
    {
        await app.StartAsync();
        try
        {
            var documentProvider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>(OpenApiPublicDocumentName);
            var document = await documentProvider.GetOpenApiDocumentAsync();
            //Servers reflect the (ephemeral) address the app bound to at generation time; drop them so the
            //committed schema is deterministic and free of environment-specific host URLs.
            document.Servers?.Clear();
            var path = System.IO.Path.Combine(app.Environment.ContentRootPath, PublicSchemaPath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            await using var stream = File.Create(path);
            await document.SerializeAsYamlAsync(stream, OpenApiSpecVersion.OpenApi3_0);
            app.Logger.LogInformation("Public OpenAPI schema written to {Path}", path);
        }
        finally
        {
            await app.StopAsync();
        }
    }
    extension(IServiceCollection services)
    {
        public IServiceCollection AddLexboxOpenApi()
        {
            services.AddOpenApi(LexboxOpenApi.OpenApiPublicDocumentName,
                options =>
                {
                    options.ShouldInclude = (api) => api.GroupName == LexboxOpenApi.OpenApiPublicDocumentName;
                    options.AddDocumentTransformer((document, context, _) =>
                    {
                        document.Info.Title = "Lexbox Public Api";
                        document.Info.Description = "This is the public api for LexBox";
                        return Task.CompletedTask;
                    });
                });
            services.AddOpenApi(LexboxOpenApi.OpenApiPrivateDocumentName,
                options =>
                {
                    options.ShouldInclude = (_) => true;
                    options.AddDocumentTransformer((document, context, _) =>
                    {
                        document.Info.Title = "Lexbox Api";
                        document.Info.Description = """
                                                    This is the open api for LexBox, most of the api is in the [graphql endpoint](/api/graphql/ui).
                                                    However there are some test users for login here, with the default password of `pass`:
                                                    * admin@test.com (site admin)
                                                    * manager@test.com (Sena 3 manager)
                                                    * editor@test.com (Sena 3 editor)
                                                    """;
                        return Task.CompletedTask;
                    });
                });
            return services;
        }
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointConventionBuilder MapLexboxOpenApi()
        {
            var group = endpoints.MapGroup("/");
            group.MapOpenApi("/api/openapi/{documentName}.json");
            group.MapGet("/api/swagger", () => Results.LocalRedirect("/api/scalar"));
            group.MapScalarApiReference("/api/scalar",
                (options) =>
                {
                    options.WithOpenApiRoutePattern("/api/openapi/{documentName}.json");
                    options.AddDocuments(LexboxOpenApi.OpenApiPublicDocumentName, LexboxOpenApi.OpenApiPrivateDocumentName);
                });
            return group;
        }
    }
}
