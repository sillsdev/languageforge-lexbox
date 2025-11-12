using System.Net;
using System.Net.Http.Json;
using LexCore.Sync;
using Testing.FwHeadless;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class DeleteProjectTests : ApiTestBase
{
    [Fact]
    public async Task DeleteProject_Success_RemovesProjectAndCallsFwHeadless()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, projectId);
        await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, projectId);

        var statusResponse = await HttpClient.GetAsync($"api/fw-lite/sync/status/{projectId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await HttpClient.DeleteAsync($"api/project/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkResponse = await HttpClient.GetAsync($"api/project/projectCodeAvailable/{projectCode}");
        checkResponse.EnsureSuccessStatusCode();
        var isAvailable = await checkResponse.Content.ReadFromJsonAsync<bool>();
        isAvailable.Should().BeTrue();

        var statusAfterDelete = await HttpClient.GetAsync($"api/fw-lite/sync/status/{projectId}");
        statusAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_NonExistentProject_ReturnsNotFound()
    {
        // Arrange
        await LoginAs("admin");
        var nonExistentProjectId = Guid.NewGuid();

        // Act
        var response = await HttpClient.DeleteAsync($"api/project/{nonExistentProjectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_NonDevRetentionPolicy_ReturnsForbidden()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();

        var gqlResponse = await ExecuteGql($$"""
            mutation {
                createProject(input: {
                    name: "Test Project"
                    code: "{{projectCode}}"
                    type: FL_EX
                    retentionPolicy: TEST
                    isConfidential: false
                    description: "Test project"
                }) {
                    createProjectResponse { id }
                }
            }
            """);

        var projectId = Guid.Parse(gqlResponse["data"]!["createProject"]!["createProjectResponse"]!["id"]!.GetValue<string>());

        try
        {
            // Act
            var response = await HttpClient.DeleteAsync($"api/project/{projectId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            await ExecuteGql($$"""
                mutation {
                    softDeleteProject(input: { projectId: "{{projectId}}" }) {
                        project { id }
                    }
                }
                """);
        }
    }

    [Fact]
    public async Task DeleteProject_DuringSyncInProgress_FwHeadlessReturnsConflict()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, projectId);
        await Task.Delay(100);

        try
        {
            // Act
            var response = await HttpClient.DeleteAsync($"api/project/{projectId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        finally
        {
            // Cleanup
            await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, projectId);
            var cleanupResponse = await HttpClient.DeleteAsync($"api/project/{projectId}");
            if (cleanupResponse.StatusCode != HttpStatusCode.NotFound)
            {
                cleanupResponse.EnsureSuccessStatusCode();
            }
        }
    }

    [Fact]
    public async Task DeleteProject_WithoutAdminRole_ReturnsUnauthorized()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        await LoginAs("manager");

        // Act
        var response = await HttpClient.DeleteAsync($"api/project/{projectId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);

        // Cleanup
        await LoginAs("admin");
        await HttpClient.DeleteAsync($"api/project/{projectId}");
    }

    [Fact]
    public async Task ResetProject_Success_DeletesOnlyFwDataRepoNotCrdtDb()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, projectId);
        await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, projectId);

        var statusBeforeReset = await HttpClient.GetAsync($"api/fw-lite/sync/status/{projectId}");
        statusBeforeReset.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        await StartLexboxProjectReset(projectCode);

        // Assert
        var statusAfterReset = await HttpClient.GetAsync($"api/fw-lite/sync/status/{projectId}");
        statusAfterReset.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusContent = await statusAfterReset.Content.ReadFromJsonAsync<ProjectSyncStatus>();
        statusContent.Should().NotBeNull();
        statusContent.status.Should().Be(ProjectSyncStatusEnum.ReadyToSync);

        // Cleanup
        await HttpClient.DeleteAsync($"api/project/{projectId}");
    }
}
