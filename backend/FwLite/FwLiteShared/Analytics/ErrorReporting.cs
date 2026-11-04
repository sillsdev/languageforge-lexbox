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
    [GeneratedRegex(@"^([\w.]{0,120}?(?:Error|Exception)):")]
    private static partial Regex JsErrorTypeRegex();

    // A relayed .NET type name: namespaced, PascalCase segments
    [GeneratedRegex(@"^(?:[A-Z]\w*\.)+[A-Z]\w*$")]
    private static partial Regex DotnetTypeNameRegex();

    // The viewer's message text is untrusted, so only these (or .NET type names) are reported as the type.
    private static readonly HashSet<string> JsErrorTypes =
    [
        "Error", "AggregateError", "EvalError", "RangeError", "ReferenceError", "SyntaxError", "TypeError",
        "URIError", "DOMException",
    ];

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

        var inner = InnerExceptions(exception)
            .Take(MaxInnerExceptions)
            .Select(e => ErrorScrubber.Truncate($"{ExceptionType(e)}: {ErrorScrubber.ScrubMessage(e.Message)}",
                ErrorScrubber.MaxStringBytes))
            .ToList();
        if (inner.Count > 0)
            properties["inner_exceptions"] = inner;
        return properties;
    }

    /// <summary>
    /// Every nested exception, outermost first. An <see cref="AggregateException"/> (e.g. several failed tasks)
    /// contributes all of its inner exceptions, not just the first one that <see cref="Exception.InnerException"/> returns.
    /// </summary>
    private static IEnumerable<Exception> InnerExceptions(Exception exception)
    {
        var pending = new Queue<Exception>(Children(exception));
        while (pending.TryDequeue(out var next))
        {
            yield return next;
            foreach (var child in Children(next))
                pending.Enqueue(child);
        }

        static IEnumerable<Exception> Children(Exception e) => e switch
        {
            AggregateException aggregate => aggregate.InnerExceptions,
            { InnerException: { } innerException } => [innerException],
            _ => [],
        };
    }

    private static string ExceptionType(Exception exception)
    {
        if (exception is JsErrorException)
        {
            var match = JsErrorTypeRegex().Match(exception.Message);
            var type = match.Groups[1].Value;
            return match.Success && (JsErrorTypes.Contains(type) || DotnetTypeNameRegex().IsMatch(type)) ? type : "Error";
        }
        return exception.GetType().FullName ?? exception.GetType().Name;
    }
}
