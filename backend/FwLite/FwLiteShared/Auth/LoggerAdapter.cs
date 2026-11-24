using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;

namespace FwLiteShared.Auth;

public partial class LoggerAdapter(ILogger<LoggerAdapter> logger): IIdentityLogger
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
        logger.Log(Convert(entry.EventLogLevel), StripMsalPrefix(entry.Message ?? ""));
    }

    public const string MsalClientName = "FwLite";

    //MSAL starts every message with "False MSAL 4.91.1.0 MSAL.NetCore .NET 10.0.15 Microsoft Windows 10.0.26200 [2026-11-16 07:35:07Z - <correlation id>] (FwLite: <app version>) ",
    //there is no option to turn that off. The versions are logged once at startup and our log lines already carry a timestamp,
    //so keep only the start of the correlation id to tie the lines of one request together
    [GeneratedRegex(@"^(?:True|False) MSAL .*? \[\d{4}-\d\d-\d\d \d\d:\d\d:\d\dZ(?: - (?<id>[0-9a-fA-F]{8})[0-9a-fA-F-]{28})?\](?: \(" + MsalClientName + @"(?:: [^)]*)?\))? ?")]
    private static partial Regex MsalPrefix();

    internal static string StripMsalPrefix(string message)
    {
        return MsalPrefix().Replace(message, match => match.Groups["id"].Success ? $"[{match.Groups["id"].Value}] " : "");
    }
}
