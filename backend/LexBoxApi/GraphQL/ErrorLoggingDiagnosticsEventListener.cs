using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace LexBoxApi.GraphQL;

public class ErrorLoggingDiagnosticsEventListener(ILogger<ErrorLoggingDiagnosticsEventListener> log) : ExecutionDiagnosticEventListener
{
    public override void ResolverError(
        IMiddlewareContext context,
        IError error)
    {
        LogError(error);
    }

    public override void TaskError(
        IExecutionTask task,
        IError error)
    {
        LogError(error);
    }

    public override void RequestError(
        IRequestContext context,
        Exception exception)
    {
        LogException(exception);
    }

    public override void SubscriptionEventError(
        SubscriptionEventContext context,
        Exception exception)
    {
        LogException(exception);
    }

    public override void SubscriptionTransportError(
        ISubscription subscription,
        Exception exception)
    {
        LogException(exception);
    }

    public override void SyntaxError(IRequestContext context, IError error)
    {
        LogError(error);
    }

    public override void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            LogError(error);
        }
    }

    public override void ResolverError(IRequestContext context, ISelection selection, IError error)
    {
        LogError(error, extraTags: [
            new("graphql.selection.field_name", selection.Field.Name),
            new("graphql.selection.field_declaring_type", selection.DeclaringType),
            new("graphql.selection.field_response_name", selection.ResponseName),
        ]);
    }

    private void LogError(IError error, [CallerMemberName] string source = "", params KeyValuePair<string, object?>[] extraTags)
    {
        log.LogError(error.Exception, "{Source}: {Message}", source, error.Message);
        TraceError(error, source, extraTags);
    }

    private void LogException(Exception exception, [CallerMemberName] string source = "")
    {
        log.LogError(exception, "{Source}: {Message}", source, exception.Message);
        TraceException(exception, source);
    }

    private void TraceError(IError error, string source, params KeyValuePair<string, object?>[] extraTags)
    {
        if (error.Exception != null)
        {
            TraceException(error.Exception, source, extraTags);
        }
        else
        {
            Activity.Current?.AddEvent(new(source, tags: AddTags(new()
            {
                ["error.message"] = error.Message,
                ["error.code"] = error.Code,
            }, extraTags)));
        }
    }

    private void TraceException(Exception exception, string source, params KeyValuePair<string, object?>[] extraTags)
    {
        Activity.Current?
            .SetStatus(ActivityStatusCode.Error)
            .AddEvent(new(source, tags: AddTags(new()
            {
                ["exception.message"] = exception.Message,
                ["exception.stacktrace"] = exception.StackTrace,
                ["exception.source"] = exception.Source,
            }, extraTags)));
        if (exception.InnerException != null)
        {
            TraceException(exception.InnerException, $"{source} - Inner");
        }
    }

    private static ActivityTagsCollection AddTags(ActivityTagsCollection tags, KeyValuePair<string, object?>[] moreTags)
    {
        foreach (var kvp in moreTags)
        {
            tags.Add(kvp.Key, kvp.Value);
        }

        return tags;
    }
}
