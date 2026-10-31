using LexCore.Exceptions;

namespace LexBoxApi.ErrorHandling;

public static class ExceptionStatusCodes
{
    /// <summary>
    /// The response status for an unhandled exception, used as the exception handler's StatusCodeSelector.
    /// </summary>
    public static int Select(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            FwHeadlessException fwHeadless => FromFwHeadless((int)fwHeadless.StatusCode),
            _ => StatusCodes.Status500InternalServerError
        };
    }

    // FwHeadless's client errors describe the caller's request, so they pass through.
    // Its auth failures are between Lexbox and FwHeadless (they must not tell the caller they aren't logged in),
    // and its server errors are ours, not the caller's, so both become 502.
    private static int FromFwHeadless(int statusCode)
    {
        return statusCode is >= 400 and < 500
            and not StatusCodes.Status401Unauthorized
            and not StatusCodes.Status403Forbidden
            ? statusCode
            : StatusCodes.Status502BadGateway;
    }
}
