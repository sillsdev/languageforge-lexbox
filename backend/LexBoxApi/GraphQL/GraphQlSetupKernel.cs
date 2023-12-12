using DataAnnotatedModelValidations;
using HotChocolate.Diagnostics;
using LexBoxApi.Auth;
using LexBoxApi.GraphQL.CustomFilters;
using LexBoxApi.Services;
using LexCore.ServiceInterfaces;
using LexData;

namespace LexBoxApi.GraphQL;

public static class GraphQlSetupKernel
{
    public const string LexBoxSchemaName = "LexBox";
    public static void AddLexGraphQL(this IServiceCollection services, IHostEnvironment env, bool forceGenerateSchema = false)
    {
        if (forceGenerateSchema || env.IsDevelopment())
            services.AddHostedService<DevGqlSchemaWriterService>();

        services.AddGraphQLServer()
            .InitializeOnStartup()
            .RegisterDbContext<LexBoxDbContext>()
            .RegisterService<IHgService>()
            .RegisterService<LoggedInContext>()
            .RegisterService<EmailService>()
            .RegisterService<LexAuthService>()
            .RegisterService<IPermissionService>()
            .AddDataAnnotationsValidator()
            .AddSorting(descriptor =>
            {
                descriptor.AddDefaults();
                descriptor.ArgumentName("orderBy");
            })
            .AddFiltering((descriptor) =>
            {
                descriptor.AddDefaults();
                descriptor.AddDeterministicInvariantContainsFilter();
            })
            .AddProjections()
            .SetPagingOptions(new()
            {
                DefaultPageSize = 100,
                MaxPageSize = 1000,
                IncludeTotalCount = true
            })
            .AddAuthorization()
            .AddLexBoxApiTypes()
            .AddMutationConventions(false)
            .AddDiagnosticEventListener<ErrorLoggingDiagnosticsEventListener>()
            .ModifyRequestOptions(options =>
            {
                options.IncludeExceptionDetails = true;
            })
            .AddType<DbErrorCode>()
            .AddType(new DateTimeType("DateTime"))
            .AddType(new UuidType("UUID"))
            .AddType(new DateTimeType("timestamptz"))
            .AddType(new UuidType("uuid"))
            .AddInstrumentation(options =>
            {
                options.IncludeDocument = true;
                // ResolveFieldValue causes one activity per field in the query (which is a lot) because it's for each user in a response for example.
                // that's why we don't use the default anymore.
                options.Scopes = ActivityScopes.ExecuteHttpRequest |
                                 ActivityScopes.ParseHttpRequest |
                                 ActivityScopes.ValidateDocument |
                                 ActivityScopes.CompileOperation |
                                 ActivityScopes.FormatHttpResponse |
                                 ActivityScopes.DataLoaderBatch |
                                 ActivityScopes.ExecuteRequest;
            });

        // services.AddHttpClient("hasura",
        //     (provider, client) =>
        //     {
        //         var hasuraConfig = provider.GetRequiredService<IOptions<HasuraConfig>>().Value;
        //         client.BaseAddress = new Uri(hasuraConfig.HasuraUrl);
        //         client.DefaultRequestHeaders.Add("x-hasura-admin-secret", hasuraConfig.HasuraSecret);
        //     });
        // graphqlBuilder
        //     .AddRemoteSchema("hasura")
        //     .AddGraphQL("hasura")
        //     .AddType(new DateTimeType("timestamptz"))
        //     .AddType(new UuidType("uuid"))
        //     .AddInstrumentation(options =>
        //     {
        //         options.IncludeDocument = true;
        //         options.Scopes = ActivityScopes.Default | ActivityScopes.ExecuteRequest;
        //     });
    }
}
