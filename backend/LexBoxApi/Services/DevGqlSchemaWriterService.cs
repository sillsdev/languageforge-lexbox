using HotChocolate.Execution;
using LexBoxApi.Auth;
using LexBoxApi.GraphQL;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Services.Email;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.Extensions.Hosting.Internal;

namespace LexBoxApi.Services;

public class DevGqlSchemaWriterService : IHostedService
{
    private readonly RequestExecutorProxy _executorProxy;
    private readonly ILogger<DevGqlSchemaWriterService> _logger;
    private static bool _schemaGenerated = false;

    public static bool IsSchemaGenerationRequest(string[] args)
    {
        return args is ["generate-gql-schema"];
    }

    public static async Task GenerateGqlSchema(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services
            .AddLogging()
            .AddSingleton<IHostLifetime, ConsoleLifetime>()
            .AddScoped<IEmailService, EmailService>()
            .AddScoped<IHgService, HgService>()
            .AddScoped<LoggedInContext>()
            .AddScoped<LexBoxDbContext>()
            .AddScoped<IPermissionService, PermissionService>()
            .AddScoped<ProjectService>()
            .AddScoped<FwHeadlessClient>()
            .AddScoped<UserService>()
            .AddScoped<LexAuthService>()
            .AddLexGraphQL(builder.Environment, true);
        var host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();
        if (!_schemaGenerated) throw new ApplicationException("Schema generation not executed");
    }

    public DevGqlSchemaWriterService(IRequestExecutorResolver executorResolver, ILogger<DevGqlSchemaWriterService> logger)
    {
        _logger = logger;
        _executorProxy = new RequestExecutorProxy(executorResolver, Schema.DefaultName);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var schema = await _executorProxy.GetSchemaAsync(cancellationToken);
        await using var file = File.Open("../../frontend/schema.graphql", FileMode.Create);
        await SchemaPrinter.PrintAsync(schema, file, true, cancellationToken);
        _logger.LogInformation("Schema written frontend/schema.graphql");
        _schemaGenerated = true;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
