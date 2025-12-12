using System.Net;
using System.Net.Http.Json;
using LexCore.Sync;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class ProjectBlockingTests : ApiTestBase
{
    private const string TestProjectCode = "test";

    [Fact]
    public async Task GetBlockStatus_WhenNotBlocked_ReturnsFalse()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.GetAsync($"api/project/block-status?projectCode={TestProjectCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SyncBlockStatus>();
        content.Should().NotBeNull();
        content.IsBlocked.Should().BeFalse();
        content.Reason.Should().BeNull();
        content.BlockedAt.Should().BeNull();
    }

    [Fact]
    public async Task BlockProject_ThenGetBlockStatus_ReturnsBlocked()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        const string blockReason = "Test blocking for integration tests";

        // Block the project
        var blockUrl = $"api/project/block?projectCode={TestProjectCode}&reason={Uri.EscapeDataString(blockReason)}";
        var blockResponse = await HttpClient.PostAsync(blockUrl, null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get the status
        var statusUrl = $"api/project/block-status?projectCode={TestProjectCode}";
        var statusResponse = await HttpClient.GetAsync(statusUrl);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await statusResponse.Content.ReadFromJsonAsync<SyncBlockStatus>();

        content.Should().NotBeNull();
        content.IsBlocked.Should().BeTrue();
        content.Reason.Should().Be(blockReason);
        content.BlockedAt.Should().NotBeNull();
        content.BlockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task BlockProject_ThenUnblockProject_ReturnsUnblocked()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        // Block the project
        var blockUrl = $"api/project/block?projectCode={TestProjectCode}&reason=Test";
        var blockResponse = await HttpClient.PostAsync(blockUrl, null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Unblock the project
        var unblockUrl = $"api/project/unblock?projectCode={TestProjectCode}";
        var unblockResponse = await HttpClient.PostAsync(unblockUrl, null);
        unblockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get the status
        var statusUrl = $"api/project/block-status?projectCode={TestProjectCode}";
        var statusResponse = await HttpClient.GetAsync(statusUrl);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await statusResponse.Content.ReadFromJsonAsync<SyncBlockStatus>();

        content.Should().NotBeNull();
        content.IsBlocked.Should().BeFalse();
        content.Reason.Should().BeNull();
        content.BlockedAt.Should().BeNull();
    }

    [Fact(Skip = "Requires SyncBlockedResult implementation in ExecuteMergeRequest")]
    public async Task ExecuteMerge_WhenProjectBlocked_ReturnsSyncBlockedResult()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        // Block the project
        var blockUrl = $"api/project/block?projectCode={TestProjectCode}&reason=Sync blocked for testing";
        var blockResponse = await HttpClient.PostAsync(blockUrl, null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to execute merge
        var mergeResponse = await HttpClient.PostAsync(
            $"api/merge/execute?projectCode={TestProjectCode}",
            null);

        // Should return a result indicating sync is blocked
        // (exact response format TBD once SyncBlockedResult is implemented)
        mergeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        // TODO: Verify response body contains sync blocked status
    }

    [Fact]
    public async Task BlockProject_WithoutProjectIdentifier_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.PostAsync($"api/project/block", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBlockStatus_WithNonexistentProjectCode_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.GetAsync($"api/project/block-status?projectCode=NONEXISTENT-PROJECT-XYZ");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
