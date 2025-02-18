using DataAnnotatedModelValidations;
using HotChocolate.Diagnostics;
using LexBoxApi.GraphQL.CustomFilters;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Services;
using LexCore.ServiceInterfaces;
using LexData;
using LfClassicData;
using Microsoft.Extensions.Options;
using Polly;

namespace LexBoxApi.GraphQL;

public static class GraphQlSetupKernel
{
    public const string LexBoxSchemaName = "LexBox";
    public const string RefreshedJwtMembershipsKey = "RefreshedJwtMemberships";
    public static void AddLexGraphQL(this IServiceCollection services, IHostEnvironment env, bool forceGenerateSchema = false)
    {
        if (forceGenerateSchema || env.IsDevelopment())
            services.AddHostedService<DevGqlSchemaWriterService>();

        services.AddScoped<IIsHarmonyProjectDataLoader, IsHarmonyProjectDataLoader>();
        services.AddScoped<IIsLanguageForgeProjectDataLoader, IsLanguageForgeProjectDataLoader>();
        services.AddResiliencePipeline<string, IReadOnlyDictionary<string, bool>>(
            IsLanguageForgeProjectDataLoader.ResiliencePolicyName,
            (builder, context) =>
            {
                builder.ConfigureTelemetry(context.ServiceProvider.GetRequiredService<ILoggerFactory>());
                IsLanguageForgeProjectDataLoader.ConfigureResiliencePipeline(builder,
                    context.ServiceProvider.GetRequiredService<IOptions<LfClassicConfig>>().Value
                        .IsLfProjectConnectionRetryTimeout);
            });

        services
            .AddGraphQLServer()
            .ModifyCostOptions(options =>
            {
                // See: https://github.com/sillsdev/languageforge-lexbox/issues/1179
                options.EnforceCostLimits = false;
            })
            .InitializeOnStartup()
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
            .ModifyPagingOptions(options =>
            {
                options.DefaultPageSize = 100;
                options.MaxPageSize = 1000;
                options.IncludeTotalCount = true;
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
    }
}
