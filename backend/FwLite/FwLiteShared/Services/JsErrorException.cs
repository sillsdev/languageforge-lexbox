namespace FwLiteShared.Services;

/// <summary>
/// An error raised in the viewer's JavaScript and reported to .NET by the global error handler.
/// Wrapping it in an exception lets it flow through <c>ILogger</c> (file log, error reporting) like any .NET error.
/// </summary>
public class JsErrorException(string message, string? stack) : Exception(message)
{
    public override string? StackTrace => stack;
}
