using SIL.Progress;

namespace FwHeadless.Services;

public class SafeLoggingProgress(ILoggerFactory loggerFactory, LogSanitizerService sanitizer) : IProgress
{
    private readonly ILogger logger = loggerFactory.CreateLogger("SendReceive");
    public bool ShowVerbose { get; set; }
    public bool CancelRequested { get; set; }
    public bool ErrorEncountered { get; set; }
    public IProgressIndicator ProgressIndicator { get; set; } = null!;
    public SynchronizationContext SyncContext { get; set; } = null!;

    private string Sanitize(string message, params object[] args)
    {
        return sanitizer.SanitizeLogMessage(GenericProgress.SafeFormat(message, args));
    }

    public void WriteError(string message, params object[] args)
    {
        logger.LogError(Sanitize(message, args));
    }

    public void WriteException(Exception error)
    {
        WriteError(error.ToString());
    }

    public void WriteMessage(string message, params object[] args)
    {
        logger.LogInformation(Sanitize(message, args));
    }

    public void WriteMessageWithColor(string colorName, string message, params object[] args)
    {
        WriteMessage(message, args);
    }

    public void WriteStatus(string message, params object[] args)
    {
        WriteMessage(message, args);
    }

    public void WriteVerbose(string message, params object[] args)
    {
        logger.LogDebug(Sanitize(message, args));
    }

    public void WriteWarning(string message, params object[] args)
    {
        logger.LogWarning(Sanitize(message, args));
    }
}
