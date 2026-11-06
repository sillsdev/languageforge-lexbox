using System.Net;

namespace LexCore.Exceptions;

/// <summary>
/// FwHeadless answered with a non-success status. Lexbox maps <see cref="StatusCode"/> to its own response status in one place.
/// </summary>
public class FwHeadlessException(HttpStatusCode statusCode, string? detail)
    : Exception(detail ?? $"FwHeadless responded with {(int)statusCode} {statusCode}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? Detail { get; } = detail;
}
