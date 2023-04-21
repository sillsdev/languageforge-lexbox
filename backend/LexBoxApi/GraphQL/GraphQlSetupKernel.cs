using HotChocolate.Diagnostics;
using LexBoxApi.Config;
using LexBoxApi.GraphQL.CustomTypes;
using LexData;
using Microsoft.Extensions.Options;

namespace LexBoxApi.GraphQL;

public static class GraphQlSetupKernel
{
    public static void AddLexGraphQL(this IServiceCollection services, IWebHostEnvironment env)
    {
        services.AddHttpClient("hasura",
            (provider, client) =>
            {
                var hasuraConfig = provider.GetRequiredService<IOptions<HasuraConfig>>().Value;
                client.BaseAddress = new Uri(hasuraConfig.HasuraUrl);
                client.DefaultRequestHeaders.Add("x-hasura-admin-secret", hasuraConfig.HasuraSecret);
            });
        var graphqlBuilder = services.AddGraphQLServer()
            .ModifyRequestOptions(options =>
            {
                options.IncludeExceptionDetails = true;
            })
            .AddType(new DateTimeType("DateTime"))
            .AddType(new UuidType("UUID"))
            .AddType(new DateTimeType("timestamptz"))
            .AddType(new UuidType("uuid"));
        graphqlBuilder
            .AddRemoteSchema("hasura")
            .AddGraphQL("hasura")
            .AddType(new DateTimeType("timestamptz"))
            .AddType(new UuidType("uuid"))
            .AddInstrumentation(options =>
            {
                options.IncludeDocument = true;
                options.Scopes = ActivityScopes.Default | ActivityScopes.ExecuteRequest;
            });
        graphqlBuilder.AddLocalSchema("LexBox")
            .RegisterDbContext<LexBoxDbContext>()
            .AddGraphQL("LexBox")
            .ModifyRequestOptions(options =>
            {
                options.IncludeExceptionDetails = true;
            })
            .AddType(new DateTimeType("DateTime"))
            .AddType(new UuidType("UUID"))
            .AddType<LexAuthUserType>()
            .AddMutationType<LexMutations>()
            .AddQueryType<LexQueries>()
            .AddSorting()
            .AddFiltering()
            .AddProjections()
            .AddMutationConventions(false)
            .AddInstrumentation();
    }
}