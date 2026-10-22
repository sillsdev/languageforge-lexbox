using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using LexCore.Exceptions;
using LexCore.Sync;
using LexCore.Utils;
using Testing.Fixtures;
using Testing.FwHeadless;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
[Collection("FwHeadless Sync")]
// IntegrationFixture is only used to upload the template repo, so deleting a project removes actual content
public class DeleteProjectTests(IntegrationFixture fixture) : ApiTestBase, IClassFixture<IntegrationFixture>
{
    private async Task<LexboxProject> CreateProjectWithTemplateRepo(ProjectConfig config)
    {
        var project = await this.RegisterProjectInLexBox(config, waitForRepoReady: true);
        await fixture.PopulateWithTemplateRepo(config.Code);
        return project;
    }

    [Fact]
    public async Task DeleteProject_RemovesRepo_AndRecreatingTheCodeGivesAnEmptyRepo()
    {
        await LoginAs("admin");
        var config = Utils.GetNewProjectConfig();
        var project = await CreateProjectWithTemplateRepo(config);
        await using var cleanup = Defer.Async(async () =>
        {
            // in case the test fails before deleting it, not asserted so it can't hide the actual failure
            await HttpClient.DeleteAsync($"api/project/{project.Id}");
        });
        // so the null last commit at the end shows the recreated project doesn't reuse the old repo
        (await GetProjectLastCommit(config.Code)).Should().NotBeNull();

        var response = await HttpClient.DeleteAsync($"api/project/{project.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await this.AssertRepoNotServed(config.Code);

        // creating a repo fails if one already exists for the code, so this also verifies the repo was removed
        await using var recreated = await this.RegisterProjectInLexBox(config with { Id = Guid.NewGuid() }, waitForRepoReady: true);
        await this.AssertRepoEmpty(config.Code);
        (await GetProjectLastCommit(config.Code)).Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteProject_RemovesRepo_AndTheCodeCanBeReusedWithAnEmptyRepo()
    {
        await LoginAs("admin");
        var config = Utils.GetNewProjectConfig();
        // A soft deleted project can't be hard deleted (its code gets a suffix that isn't a valid repo name),
        // so like DeleteProject_NonDevRetentionPolicy_ReturnsForbidden this leaves it behind
        var project = await CreateProjectWithTemplateRepo(config);
        (await GetProjectLastCommit(config.Code)).Should().NotBeNull();

        var gqlResponse = await ExecuteGql($$"""
            mutation {
                softDeleteProject(input: { projectId: "{{project.Id}}" }) {
                    project { id code deletedDate }
                    errors { __typename }
                }
            }
            """);
        // the returned project is always null: the query filter hides soft deleted projects
        gqlResponse["data"]!["softDeleteProject"]!["errors"].Should().BeNull();
        var codeAvailable = await HttpClient.GetFromJsonAsync<bool>($"api/project/projectCodeAvailable/{config.Code}");
        codeAvailable.Should().BeTrue("the soft deleted project's code gets a suffix");
        await this.AssertRepoNotServed(config.Code);

        await using var recreated = await this.RegisterProjectInLexBox(config with { Id = Guid.NewGuid() }, waitForRepoReady: true);
        await this.AssertRepoEmpty(config.Code);
        (await GetProjectLastCommit(config.Code)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteProject_Success_RemovesProjectAndCallsFwHeadless()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, projectId);
        await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, projectId);
        await AssertProjectStatus(projectId, ProjectSyncStatusEnum.ReadyToSync);

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

        var testSucceeded = false;
        await using var cleanup = Defer.Async(async () =>
        {
            try
            {

                await ExecuteGql($$"""
                mutation {
                    softDeleteProject(input: { projectId: "{{projectId}}" }) {
                        project { id }
                    }
                }
                """);
            }
            catch
            {
                if (testSucceeded) throw;
            }
        });

        // Act
        var response = await HttpClient.DeleteAsync($"api/project/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        testSucceeded = true;
    }

    [Fact]
    public async Task DeleteProject_DuringSyncInProgress_FwHeadlessReturnsConflict()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        var testSucceeded = false;
        await using var cleanup = Defer.Async(async () =>
        {
            try
            {
                await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, projectId);
            }
            catch (Exception ex)
            {
                // ignore: this is not what we're testing and we don't want exceptions here to prevent deletion
                Console.WriteLine($"[DeleteProjectTests] Ignored exception in cleanup: {ex}");
            }

            try
            {
                var cleanupResponse = await HttpClient.DeleteAsync($"api/project/{projectId}");
                cleanupResponse.ShouldBeSuccessful();
            }
            catch
            {
                if (testSucceeded) throw;
            }
        });

        // Act
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, projectId);
        // no delay means the sync job might only be queued, but not actually be running yet
        // this should return Conflict as well, otherwise the deletion and sync could interfere with each other
        var response = await HttpClient.DeleteAsync($"api/project/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Act
        // wait a bit to ensure the sync is actually in progress
        await Task.Delay(3000);
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, projectId);
        response = await HttpClient.DeleteAsync($"api/project/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        testSucceeded = true;
    }

    [Fact]
    public async Task DeleteProject_WithoutAdminRole_ReturnsUnauthorized()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");
        var testSucceeded = false;
        await using var cleanup = Defer.Async(async () =>
        {
            try
            {
                await LoginAs("admin");
                await HttpClient.DeleteAsync($"api/project/{projectId}");
            }
            catch
            {
                if (testSucceeded) throw;
            }
        });

        await LoginAs("manager");

        // Act
        var response = await HttpClient.DeleteAsync($"api/project/{projectId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);

        testSucceeded = true;
    }

    [Fact]
    public async Task ResetProject_Success_DeletesOnlyFwDataRepoNotCrdtDb()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");
        var testSucceeded = false;
        await using var cleanup = Defer.Async(async () =>
        {
            try
            {
                await HttpClient.DeleteAsync($"api/project/{projectId}");
            }
            catch
            {
                if (testSucceeded) throw;
            }
        });
        // this status should be different than after a sync and repo reset
        await AssertProjectStatus(projectId, ProjectSyncStatusEnum.NeverSynced);

        await FwHeadlessTestHelpers.TriggerSync(HttpClient, projectId);
        await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, projectId);

        var statusBeforeReset = await HttpClient.GetAsync($"api/fw-lite/sync/status/{projectId}");
        statusBeforeReset.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        await StartLexboxProjectReset(projectCode);

        // Assert
        await AssertProjectStatus(projectId, ProjectSyncStatusEnum.ReadyToSync);

        testSucceeded = true;
    }

    [Fact]
    public async Task SoftDeleteProject_DuringSyncInProgress_ReturnsGqlError()
    {
        // Arrange
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");

        var testSucceeded = false;
        await using var cleanup = Defer.Async(async () =>
        {
            try
            {
                await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, projectId);
            }
            catch
            {
                // ignore: this is not what we're testing and we don't want exceptions here to prevent deletion
            }

            try
            {
                var cleanupResponse = await HttpClient.DeleteAsync($"api/project/{projectId}");
                cleanupResponse.ShouldBeSuccessful();
            }
            catch
            {
                if (testSucceeded) throw;
            }
        });

        // Act
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, projectId);
        // no delay means the sync job might only be queued, but not actually be running yet
        // this should return a GraphQL error, otherwise the deletion and sync could interfere with each other
        var response = await ExecuteGql($$"""
            mutation {
                softDeleteProject(input: { projectId: "{{projectId}}" }) {
                    project { id }
                    errors {
                        __typename
                    }
                }
            }
            """, expectGqlError: true);

        // Assert
        var softDeleteResult = response["data"]!["softDeleteProject"]?.AsObject();
        var expectedErrorName = nameof(ProjectSyncInProgressException).Replace("Exception", "Error");
        AssertGqlErrorOfType(softDeleteResult, expectedErrorName);

        // Act
        // wait a bit to ensure the sync is actually in progress
        await Task.Delay(3000);
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, projectId);
        response = await ExecuteGql($$"""
            mutation {
                softDeleteProject(input: { projectId: "{{projectId}}" }) {
                    project { id }
                    errors {
                        __typename
                    }
                }
            }
            """, expectGqlError: true);

        // Assert
        softDeleteResult = response["data"]!["softDeleteProject"]?.AsObject();
        AssertGqlErrorOfType(softDeleteResult, expectedErrorName);

        testSucceeded = true;
    }

    private async Task AssertProjectStatus(Guid projectId, ProjectSyncStatusEnum expectedStatus)
    {
        var statusResponse = await HttpClient.GetAsync($"api/fw-lite/sync/status/{projectId}");
        statusResponse.ShouldBeSuccessful();

        var status = await statusResponse.Content.ReadFromJsonAsync<ProjectSyncStatus>();
        status.Should().NotBeNull();
        status.status.Should().Be(expectedStatus);
    }

    private void AssertGqlErrorOfType(JsonObject? gqlResponse, string expectedErrorTypeName)
    {
        gqlResponse.Should().NotBeNull();
        var errors = gqlResponse["errors"]?.AsArray();
        errors.Should().NotBeNullOrEmpty();
        var errorTypeName = errors![0]!["__typename"]?.GetValue<string>();
        errorTypeName.Should().Be(expectedErrorTypeName);
    }
}
