using System.Text.RegularExpressions;
using FwLiteShared.Services;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Analytics;

/// <summary>
/// Builds the Mixpanel <see cref="ErrorEvent"/> from a logged error. Every free-text value goes through
/// <see cref="ErrorScrubber"/>; see it for what is redacted.
/// </summary>
public static partial class ErrorReporting
{
    public const string ErrorEvent = "error";
    public const string DotnetSource = "dotnet";
    public const string JsSource = "js";
    private const int MaxInnerExceptions = 5;

    // "TypeError: x" or "System.InvalidOperationException: x" at the start of a viewer error message
    [GeneratedRegex(@"^([\w.]*(?:Error|Exception)):")]
    private static partial Regex JsErrorTypeRegex();

    public static Dictionary<string, object?> BuildProperties(
        string category,
        LogLevel level,
        string? messageTemplate,
        Exception? exception)
    {
        var properties = new Dictionary<string, object?>
        {
            ["error_source"] = exception is JsErrorException ? JsSource : DotnetSource,
            ["log_category"] = category,
            ["log_level"] = level.ToString(),
        };
        if (ErrorScrubber.ScrubMessage(messageTemplate) is { } template)
            properties["message_template"] = template;
        if (exception is null)
            return properties;

        properties["exception_type"] = ExceptionType(exception);
        if (ErrorScrubber.ScrubMessage(exception.Message) is { } message)
            properties["exception_message"] = message;
        var stack = ErrorScrubber.ScrubStack(exception.StackTrace);
        if (stack.Count > 0)
            properties["exception_stack"] = stack;

        var inner = new List<string>();
        for (var e = exception.InnerException; e is not null && inner.Count < MaxInnerExceptions; e = e.InnerException)
        {
            inner.Add(ErrorScrubber.Truncate($"{ExceptionType(e)}: {ErrorScrubber.ScrubMessage(e.Message)}",
                ErrorScrubber.MaxStringBytes));
        }
        if (inner.Count > 0)
            properties["inner_exceptions"] = inner;
        return properties;
    }

    private static string ExceptionType(Exception exception)
    {
        if (exception is JsErrorException)
        {
            var match = JsErrorTypeRegex().Match(exception.Message);
            return match.Success ? match.Groups[1].Value : "Error";
        }
        return exception.GetType().FullName ?? exception.GetType().Name;
    }
}
