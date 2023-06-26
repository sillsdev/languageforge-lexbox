using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace LexBoxApi.GraphQL;

public class ErrorLoggingDiagnosticsEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<ErrorLoggingDiagnosticsEventListener> log;

    public override bool EnableResolveFieldValue => base.EnableResolveFieldValue;

    public ErrorLoggingDiagnosticsEventListener(
        ILogger<ErrorLoggingDiagnosticsEventListener> log)
    {
        this.log = log;
    }

    public override void ResolverError(
        IMiddlewareContext context,
        IError error)
    {
        LogError(error, "ResolverError");
    }

    public override void TaskError(
        IExecutionTask task,
        IError error)
    {
        LogError(error, "TaskError");
    }

    public override void RequestError(
        IRequestContext context,
        Exception exception)
    {
        LogException(exception, "RequestError");
    }

    public override void SubscriptionEventError(
        SubscriptionEventContext context,
        Exception exception)
    {
        LogException(exception, "SubscriptionEventError");
    }

    public override void SubscriptionTransportError(
        ISubscription subscription,
        Exception exception)
    {
        LogException(exception, "SubscriptionTransportError");
    }

    public override void SyntaxError(IRequestContext context, IError error)
    {
        LogError(error, "SyntaxError");
    }

    public override void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            LogError(error, "ValidationError");
        }
    }

    public override void ResolverError(IRequestContext context, ISelection selection, IError error)
    {
        LogError(error, "ResolverError");
    }

    private void LogError(IError error, string source)
    {
        log.LogError(error.Exception, "{Source}: {Message}", source, error.Message);
    }

    private void LogException(Exception exception, string source)
    {
        log.LogError(exception, "{Source}: {Message}", source, exception.Message);
    }
}
