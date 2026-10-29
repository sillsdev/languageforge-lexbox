using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Services;

public class JsInvokableLogger(ILogger<JsInvokableLogger> logger)
{
    [JSInvokable]
    public Task Log(LogLevel level, string message)
    {
        logger.Log(level, message);
        return Task.CompletedTask;
    }

    /// <summary>Log an unexpected viewer error so it reaches the log file and error reporting.</summary>
    [JSInvokable]
    public Task LogError(string message, string? stack)
    {
        logger.LogError(new JsErrorException(message, stack), "Viewer error");
        return Task.CompletedTask;
    }
}
