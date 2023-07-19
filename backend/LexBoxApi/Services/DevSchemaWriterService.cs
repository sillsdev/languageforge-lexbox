using HotChocolate.Execution;

namespace LexBoxApi.Services;

public class DevSchemaWriterService : IHostedService
{
    private readonly RequestExecutorProxy _executorProxy;
    private readonly ILogger<DevSchemaWriterService> _logger;
    public DevSchemaWriterService(IRequestExecutorResolver executorResolver, ILogger<DevSchemaWriterService> logger)
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
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
