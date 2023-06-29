using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace LexBoxApi.GraphQL;

public class ErrorLoggingDiagnosticsEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<ErrorLoggingDiagnosticsEventListener> log;

    public ErrorLoggingDiagnosticsEventListener(
        ILogger<ErrorLoggingDiagnosticsEventListener> log)
    {
        this.log = log;
    }

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
        LogError(error);
    }

    private void LogError(IError error, [CallerMemberName] string source = "")
    {
        log.LogError(error.Exception, "{Source}: {Message}", source, error.Message);
        TraceError(error, source);
    }

    private void LogException(Exception exception, [CallerMemberName] string source = "")
    {
        log.LogError(exception, "{Source}: {Message}", source, exception.Message);
        TraceException(exception, source);
    }

    private void TraceError(IError error, string source)
    {
        if (error.Exception != null)
        {
            TraceException(error.Exception, source);
        }
        else
        {
            Activity.Current?.AddEvent(new(source, tags: new()
            {
                ["error.message"] = error.Message,
                ["error.code"] = error.Code,
            }));
        }
    }

    private void TraceException(Exception exception, string source)
    {
        Activity.Current?
            .SetStatus(ActivityStatusCode.Error)
            .AddEvent(new(source, tags: new()
            {
                ["exception.message"] = exception.Message,
                ["exception.stacktrace"] = exception.StackTrace,
                ["exception.source"] = exception.Source,
            }));
        if (exception.InnerException != null)
        {
            TraceException(exception.InnerException, $"{source} - Inner");
        }
    }
}
