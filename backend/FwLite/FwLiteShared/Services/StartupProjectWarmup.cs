using System.Diagnostics;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteShared.Services;

/// <summary>
/// Opens the project the app starts on while the window and viewer are still loading. The first project open and
/// the first entry queries in a process spend seconds on one-time EF Core, linq2db and JIT setup, which would
/// otherwise all happen after the viewer is up.
/// </summary>
public class StartupProjectWarmup(IServiceProvider services, CrdtProjectsService projects, ILogger<StartupProjectWarmup> logger)
{
    private string? _projectCode;
    private Task _warmup = Task.CompletedTask;

    public void Start(string? startPath)
    {
        if (CrdtProjectCode(startPath) is not { } code) return;
        _projectCode = code;
        _warmup = Task.Run(() => WarmUp(code));
    }

    /// <summary>
    /// Run alongside the warm-up, the viewer's first queries would pay the same one-time costs at the same time,
    /// so opening the warmed-up project waits for it instead.
    /// </summary>
    public async Task WaitFor(string projectCode)
    {
        if (projectCode != _projectCode) return;
        try
        {
            await _warmup.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Opening project without waiting for its warm-up to finish");
        }
    }

    private async Task WarmUp(string code)
    {
        try
        {
            if (projects.GetProject(code) is not { } project) return;
            var stopwatch = Stopwatch.StartNew();
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
            using var api = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scope.ServiceProvider, project);
            //each opens its own db context
            await Task.WhenAll(
                api.GetEntries(new QueryOptions(SortOptions.Default, Count: 50)),
                api.GetEntry(Guid.Empty),
                api.CountEntries(null, null),
                api.GetWritingSystems());
            StartupTiming.Mark("Project warmed up");
            logger.LogInformation("Warmed up project in {Elapsed} ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to warm up the startup project");
        }
    }

    private static string? CrdtProjectCode(string? path)
    {
        var segments = path?.Split('?')[0].Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments is ["project", var code, ..] ? Uri.UnescapeDataString(code) : null;
    }
}
