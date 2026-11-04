using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Analytics;

public static class UnhandledExceptionLogging
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Log unhandled and unobserved exceptions. Logging an error also reports it (see
    /// <see cref="AnalyticsErrorLoggerProvider"/>); when the process is about to die, wait briefly for that
    /// report to be sent.
    /// </summary>
    public static void Register(IServiceProvider services, ILogger logger)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                logger.LogError(exception, "Unhandled exception");
            }
            else
            {
                logger.LogError("Unhandled exception");
            }
            if (e.IsTerminating)
                FlushAnalytics(services);
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            logger.LogError(e.Exception, "Unobserved task exception");
        };
    }

    private static void FlushAnalytics(IServiceProvider services)
    {
        try
        {
            // Blocking is deliberate: the process exits as soon as this handler returns.
#pragma warning disable VSTHRD002
            services.GetService<IAnalyticsService>()?.FlushAsync(FlushTimeout).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }
        catch
        {
            // Crashing anyway; nothing more to do.
        }
    }
}
