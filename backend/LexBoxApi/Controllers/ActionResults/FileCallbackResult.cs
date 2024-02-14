using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace LexBoxApi.Controllers.ActionResults;

/// <summary>
/// Represents an <see cref="ActionResult"/> that when executed will
/// execute a callback to write the file content out as a stream.
/// copied from https://github.com/StephenClearyExamples/AsyncDynamicZip/tree/net6-ziparchive
/// MIT License
/// </summary>
public sealed class FileCallbackResult : FileResult
{
    private readonly Func<Stream, ActionContext, Task> _callback;

    /// <summary>
    /// Creates a new <see cref="FileCallbackResult"/> instance.
    /// </summary>
    /// <param name="contentType">The Content-Type header of the response.</param>
    /// <param name="callback">The stream with the file.</param>
    public FileCallbackResult(string contentType, Func<Stream, ActionContext, Task> callback)
        : base(contentType)
    {
        _ = callback ?? throw new ArgumentNullException(nameof(callback));
        _callback = callback;
    }

    /// <inheritdoc />
    public override Task ExecuteResultAsync(ActionContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));
        var executor =
            new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
        return executor.ExecuteAsync(context, this);
    }

    private sealed class FileCallbackResultExecutor : FileResultExecutorBase
    {
        public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
        {
        }

        public Task ExecuteAsync(ActionContext context, FileCallbackResult result)
        {
            SetHeadersAndLog(context, result, fileLength: null, enableRangeProcessing: false);
            return result._callback(context.HttpContext.Response.BodyWriter.AsStream(), context);
        }
    }
}
