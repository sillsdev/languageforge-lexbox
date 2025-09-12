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
}
