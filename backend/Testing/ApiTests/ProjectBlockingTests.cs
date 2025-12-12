using System.Net;
using System.Net.Http.Json;
using LexCore.Sync;
using Testing.FwHeadless;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class ProjectBlockingTests : ApiTestBase
{
    [Fact]
    public async Task GetBlockStatus_WhenNotBlocked_ReturnsFalse()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        var response = await HttpClient.GetAsync($"api/fw-lite/sync/block-status?projectId={projectId}");

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
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");
        const string blockReason = "Test blocking for integration tests";

        // Block the project
        var blockUrl = $"api/fw-lite/sync/block?projectId={projectId}&reason={Uri.EscapeDataString(blockReason)}";
        var blockResponse = await HttpClient.PostAsync(blockUrl, null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get the status
        var statusUrl = $"api/fw-lite/sync/block-status?projectId={projectId}";
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
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        // Block the project
        var blockUrl = $"api/fw-lite/sync/block?projectId={projectId}&reason=Test";
        var blockResponse = await HttpClient.PostAsync(blockUrl, null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Unblock the project
        var unblockUrl = $"api/fw-lite/sync/unblock?projectId={projectId}";
        var unblockResponse = await HttpClient.PostAsync(unblockUrl, null);
        unblockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get the status
        var statusUrl = $"api/fw-lite/sync/block-status?projectId={projectId}";
        var statusResponse = await HttpClient.GetAsync(statusUrl);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await statusResponse.Content.ReadFromJsonAsync<SyncBlockStatus>();

        content.Should().NotBeNull();
        content.IsBlocked.Should().BeFalse();
        content.Reason.Should().BeNull();
        content.BlockedAt.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteMerge_WhenProjectBlocked_ReturnsProblemResponse()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");
        const string blockReason = "Sync blocked for testing";

        // Block the project
        var blockUrl = $"api/fw-lite/sync/block?projectId={projectId}&reason={Uri.EscapeDataString(blockReason)}";
        var blockResponse = await HttpClient.PostAsync(blockUrl, null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to execute merge
        var mergeResponse = await HttpClient.PostAsync(
            $"api/merge/execute?projectId={projectId}",
            null);

        // Should return a Problem response indicating sync is blocked
        mergeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await mergeResponse.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Project is blocked from syncing");
        responseContent.Should().Contain(blockReason);
    }

    [Fact]
    public async Task BlockProject_WithNonexistentProjectId_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.PostAsync($"api/fw-lite/sync/block?projectId={Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBlockStatus_WithNonexistentProjectId_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.GetAsync($"api/fw-lite/sync/block-status?projectId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BlockProject_WithProjectCode_ResolvesAndBlocks()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        // Block using projectCode instead of projectId
        var blockUrl = $"api/fw-lite/sync/block?projectCode={projectCode}&reason=Test%20with%20code";
        var blockResponse = await HttpClient.PostAsync(blockUrl, null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's blocked by checking with projectId
        var statusUrl = $"api/fw-lite/sync/block-status?projectId={projectId}";
        var statusResponse = await HttpClient.GetAsync(statusUrl);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await statusResponse.Content.ReadFromJsonAsync<SyncBlockStatus>();
        content.Should().NotBeNull();
        content.IsBlocked.Should().BeTrue();
        content.Reason.Should().Be("Test with code");
    }

    [Fact]
    public async Task UnblockProject_WithProjectCode_ResolvesAndUnblocks()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        // Block the project
        var blockUrl = $"api/fw-lite/sync/block?projectId={projectId}&reason=Test";
        var blockResponse = await HttpClient.PostAsync(blockUrl, null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Unblock using projectCode instead of projectId
        var unblockUrl = $"api/fw-lite/sync/unblock?projectCode={projectCode}";
        var unblockResponse = await HttpClient.PostAsync(unblockUrl, null);
        unblockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's unblocked
        var statusUrl = $"api/fw-lite/sync/block-status?projectId={projectId}";
        var statusResponse = await HttpClient.GetAsync(statusUrl);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await statusResponse.Content.ReadFromJsonAsync<SyncBlockStatus>();
        content.Should().NotBeNull();
        content.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task GetBlockStatus_WithProjectCode_ResolvesAndReturnsStatus()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        // Block the project
        var blockUrl = $"api/fw-lite/sync/block?projectId={projectId}";
        await HttpClient.PostAsync(blockUrl, null);

        // Get status using projectCode instead of projectId
        var statusUrl = $"api/fw-lite/sync/block-status?projectCode={projectCode}";
        var statusResponse = await HttpClient.GetAsync(statusUrl);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await statusResponse.Content.ReadFromJsonAsync<SyncBlockStatus>();
        content.Should().NotBeNull();
        content.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task BlockProject_WithoutProjectIdOrCode_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.PostAsync("api/fw-lite/sync/block", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Either projectId or projectCode must be provided");
    }

    [Fact]
    public async Task UnblockProject_WithoutProjectIdOrCode_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.PostAsync("api/fw-lite/sync/unblock", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Either projectId or projectCode must be provided");
    }

    [Fact]
    public async Task GetBlockStatus_WithoutProjectIdOrCode_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.GetAsync("api/fw-lite/sync/block-status");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Either projectId or projectCode must be provided");
    }

    [Fact]
    public async Task BlockProject_WithNonexistentProjectCode_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.PostAsync("api/fw-lite/sync/block?projectCode=nonexistent", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Project code 'nonexistent' not found");
    }

    [Fact]
    public async Task UnblockProject_WithNonexistentProjectCode_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.PostAsync("api/fw-lite/sync/unblock?projectCode=nonexistent", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Project code 'nonexistent' not found");
    }

    [Fact]
    public async Task GetBlockStatus_WithNonexistentProjectCode_ReturnsBadRequest()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        var response = await HttpClient.GetAsync("api/fw-lite/sync/block-status?projectCode=nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Project code 'nonexistent' not found");
    }
}
