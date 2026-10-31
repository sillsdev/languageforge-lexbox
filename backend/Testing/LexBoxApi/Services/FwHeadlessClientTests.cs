using System.Net;
using LexBoxApi.Services;
using LexCore.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Contrib.HttpClient;

namespace Testing.LexBoxApi.Services;

public class FwHeadlessClientTests
{
    private static FwHeadlessClient ClientReturning(HttpStatusCode statusCode, string body, string mediaType)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.SetupAnyRequest().ReturnsResponse(statusCode, body, mediaType);
        var httpClient = handler.CreateClient();
        httpClient.BaseAddress = new Uri("http://fw-headless");
        return new FwHeadlessClient(httpClient, NullLogger<FwHeadlessClient>.Instance);
    }

    [Fact]
    public async Task RegenerateProjectSnapshot_ThrowsWithUnquotedDetail_ForJsonStringBody()
    {
        // FwHeadless's NotFound<string> serializes the message as a JSON string
        var client = ClientReturning(HttpStatusCode.NotFound, "\"Commit not found\"", "application/json");

        var act = () => client.RegenerateProjectSnapshot(Guid.NewGuid(), Guid.NewGuid());

        var exception = (await act.Should().ThrowAsync<FwHeadlessException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Detail.Should().Be("Commit not found");
        exception.Message.Should().Be("Commit not found");
    }

    [Fact]
    public async Task SyncMercurialAndHarmony_ThrowsWithProblemDetail_ForProblemJsonBody()
    {
        var client = ClientReturning(HttpStatusCode.Locked,
            """{"type":"https://tools.ietf.org/html/rfc4918#section-11.3","title":"Locked","status":423,"detail":"Project is blocked from syncing. Reason: testing"}""",
            "application/problem+json");

        var act = () => client.SyncMercurialAndHarmony(Guid.NewGuid());

        var exception = (await act.Should().ThrowAsync<FwHeadlessException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.Locked);
        exception.Detail.Should().Be("Project is blocked from syncing. Reason: testing");
    }

    [Fact]
    public async Task BlockProject_ThrowsWithProblemTitle_WhenThereIsNoDetail()
    {
        var client = ClientReturning(HttpStatusCode.NotFound,
            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404}""",
            "application/problem+json");

        var act = () => client.BlockProject(Guid.NewGuid());

        var exception = (await act.Should().ThrowAsync<FwHeadlessException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Detail.Should().Be("Not Found");
    }

    [Fact]
    public async Task UnblockProject_ThrowsWithPlainTextDetail()
    {
        var client = ClientReturning(HttpStatusCode.InternalServerError, "Something broke", "text/plain");

        var act = () => client.UnblockProject(Guid.NewGuid());

        var exception = (await act.Should().ThrowAsync<FwHeadlessException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        exception.Detail.Should().Be("Something broke");
    }

    [Fact]
    public async Task DeleteRepo_ThrowsProjectSyncInProgress_OnConflict()
    {
        var client = ClientReturning(HttpStatusCode.Conflict, "", "text/plain");

        var act = () => client.DeleteRepo(Guid.NewGuid());

        await act.Should().ThrowAsync<ProjectSyncInProgressException>();
    }
}
