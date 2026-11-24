using System.Net;
using LexBoxApi.ErrorHandling;
using LexCore.Exceptions;

namespace Testing.LexBoxApi.ErrorHandling;

public class ExceptionStatusCodesTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 400)]
    [InlineData(HttpStatusCode.NotFound, 404)]
    [InlineData(HttpStatusCode.Conflict, 409)]
    [InlineData(HttpStatusCode.Locked, 423)]
    [InlineData(HttpStatusCode.Unauthorized, 502)]
    [InlineData(HttpStatusCode.Forbidden, 502)]
    [InlineData(HttpStatusCode.InternalServerError, 502)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 502)]
    public void FwHeadlessStatus_MapsToLexboxStatus(HttpStatusCode fwHeadlessStatus, int expected)
    {
        ExceptionStatusCodes.Select(new FwHeadlessException(fwHeadlessStatus, null)).Should().Be(expected);
    }

    [Fact]
    public void OtherExceptions_KeepTheirStatus()
    {
        ExceptionStatusCodes.Select(new UnauthorizedAccessException()).Should().Be(401);
        ExceptionStatusCodes.Select(new InvalidOperationException()).Should().Be(500);
    }
}
