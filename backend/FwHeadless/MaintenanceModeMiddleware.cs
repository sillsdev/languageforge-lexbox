using Microsoft.Extensions.Options;

namespace FwHeadless;

public class MaintenanceModeMiddleware(RequestDelegate next, IOptions<MaintenanceModeConfig> config)
{
    public const string DefaultMaintenanceMessage = "Lexbox is in read-only mode for scheduled maintenance, please try again in an hour or two";

#pragma warning disable IDE0022
    public static bool IsWriteMethod(string httpMethod) => httpMethod switch
    {
        // Sadly, readonly static vars do not count as consts for switch expressions
        var m when m == HttpMethods.Post => true,
        var m when m == HttpMethods.Put => true,
        var m when m == HttpMethods.Patch => true,
        var m when m == HttpMethods.Delete => true,
        _ => false
    };
#pragma warning restore IDE0022

    public async Task Invoke(HttpContext context)
    {
        var readOnly = config.Value.ReadOnlyMode;
        var message = config.Value.MaintenanceMessage;
        // If read-only mode is set without an explicit message, use the default
        if (readOnly) message ??= DefaultMaintenanceMessage;
        // But if not in read-only mode, then an empty message should not set the header

        // If we get here without a maintenance message, exit fast
        if (string.IsNullOrEmpty(message))
        {
            await next(context);
            return;
        }

        // Non-empty maintenance messages should be set on all requests
        context.Response.Headers["maintenance-message"] = message;

        // But request filtering should only happen if we're in read-only mode
        if (readOnly && IsWriteMethod(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync(message);
            return;
        }

        await next(context);
    }
}
