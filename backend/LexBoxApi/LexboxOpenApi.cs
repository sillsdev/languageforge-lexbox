using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace LexBoxApi;

public static class LexboxOpenApi
{
    public const string OpenApiPrivateDocumentName = "v1";
    public const string OpenApiPublicDocumentName = "public";
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
