using System.Net;
using LexBoxApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Contrib.HttpClient;

namespace Testing.LexBoxApi.Services;

public class FwHeadlessClientTests
{
    [Fact]
    public async Task RegenerateProjectSnapshot_PassesThroughFwHeadlessStatusCode()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.NotFound, "Commit not found");
        var httpClient = handler.CreateClient();
        httpClient.BaseAddress = new Uri("http://fw-headless");
        var client = new FwHeadlessClient(httpClient, NullLogger<FwHeadlessClient>.Instance);

        var (success, statusCode, error) = await client.RegenerateProjectSnapshot(Guid.NewGuid(), Guid.NewGuid());

        success.Should().BeFalse();
        statusCode.Should().Be(HttpStatusCode.NotFound);
        error.Should().Be("Commit not found");
    }
}
