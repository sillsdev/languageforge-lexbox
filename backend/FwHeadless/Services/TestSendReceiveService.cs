using FwDataMiniLcmBridge;

namespace FwHeadless.Services;

public class TestSendReceiveService : IHostedService // (IOptions<FwHeadlessConfig> config, SafeLoggingProgress progress)
{
    private readonly ILogger<TestSendReceiveService> _logger;
    private readonly SendReceiveService _srService;

    public static bool IsTestRequest(string[] args)
    {
        return args.Contains("test-hg-incoming");
    }

    public static async Task TestHgIncoming(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var services = builder.Services;
        services
            .AddLogging(builder => builder.AddConsole().AddDebug().AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
        services.AddOptions<FwHeadlessConfig>()
            .BindConfiguration("FwHeadlessConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<SendReceiveService>();
        services.AddScoped<LogSanitizerService>();
        services.AddScoped<SafeLoggingProgress>();
        services.AddHostedService<TestSendReceiveService>();
        var host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();
    }

    public TestSendReceiveService(ILogger<TestSendReceiveService> logger, SendReceiveService srService)
    {
        _logger = logger;
        _srService = srService;
    }

    public async Task StartAsync(CancellationToken token)
    {
        _logger.LogInformation("Starting test");
        var project = new FwDataProject("sena-3", ".");
        var count = await _srService.PendingCommitCount(project, "sena-3");
        _logger.LogInformation("Count was {count}", count);
    }

    public Task StopAsync(CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
