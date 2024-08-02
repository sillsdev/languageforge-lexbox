using Microsoft.IdentityModel.Abstractions;

namespace LocalWebApp.Auth;

public class LoggerAdapter(ILogger<LoggerAdapter> logger): IIdentityLogger
{
    private LogLevel Convert(EventLogLevel eventLogLevel)
    {
        return eventLogLevel switch
        {
            EventLogLevel.LogAlways => LogLevel.Trace,
            EventLogLevel.Critical => LogLevel.Critical,
            EventLogLevel.Error => LogLevel.Error,
            EventLogLevel.Warning => LogLevel.Warning,
            EventLogLevel.Informational => LogLevel.Information,
            EventLogLevel.Verbose => LogLevel.Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(eventLogLevel), eventLogLevel, null)
        };
    }
    public bool IsEnabled(EventLogLevel eventLogLevel)
    {
        return logger.IsEnabled(Convert(eventLogLevel));
    }

    public void Log(LogEntry entry)
    {
        logger.Log(Convert(entry.EventLogLevel), entry.Message);
    }
}
