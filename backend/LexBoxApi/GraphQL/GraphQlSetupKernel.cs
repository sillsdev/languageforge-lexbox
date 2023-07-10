using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Diagnostics;
using LexBoxApi.Auth;
using LexBoxApi.Config;
using LexBoxApi.Services;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.Extensions.Options;

namespace LexBoxApi.GraphQL;

public static class GraphQlSetupKernel
{
    public const string LexBoxSchemaName = "LexBox";
    public static void AddLexGraphQL(this IServiceCollection services, IWebHostEnvironment env)
    {

        services.AddGraphQLServer()
            .InitializeOnStartup()
            .RegisterDbContext<LexBoxDbContext>()
            .RegisterService<IHgService>()
            .RegisterService<LoggedInContext>()
            .RegisterService<EmailService>()
            .RegisterService<LexAuthService>()
            .AddSorting(descriptor =>
            {
                descriptor.AddDefaults();
                descriptor.ArgumentName("orderBy");
            })
            .AddFiltering()
            .AddProjections(descriptor =>
            {
                descriptor.Provider(new QueryableProjectionProvider(providerDescriptor =>
                {
                    //does not work because hot chocolate wants to make this as the select `p => new project { userCount = p.usercount}`
                    // which doesn't work when using projectable because the field needs to be write only
                    //shelving it for now
                    providerDescriptor.RegisterFieldHandler<EfCoreProjectablesFieldHandler>();
                    providerDescriptor.AddDefaults();
                }));
            })
            .AddAuthorization()
            .AddLexBoxApiTypes()
            .AddMutationConventions(false)
            .AddDiagnosticEventListener<ErrorLoggingDiagnosticsEventListener>()
            .ModifyRequestOptions(options =>
            {
                options.IncludeExceptionDetails = true;
            })
            .AddType(new DateTimeType("DateTime"))
            .AddType(new UuidType("UUID"))
            .AddType(new DateTimeType("timestamptz"))
            .AddType(new UuidType("uuid"))
            .AddInstrumentation(options =>
            {
                options.IncludeDocument = true;
                options.Scopes = ActivityScopes.Default | ActivityScopes.ExecuteRequest;
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
