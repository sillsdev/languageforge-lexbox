// using System.Net.Http.Json;
// using System.Text.Json;
// using FluentAssertions;
// using SIL.Harmony.Core;
using System.Text.Json;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.FwHeadless;

[Trait("Category", "Integration")]
public class MergeFailureTests : ApiTestBase, IAsyncLifetime
{

    private Guid _projectId;
    private string _projectCode = "";

    public async Task InitializeAsync()
    {
        await LoginAs("admin");
        _projectCode = Utils.NewProjectCode();
        _projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, _projectCode, "sena-3");
    }

    public async Task DisposeAsync()
    {
        await HttpClient.DeleteAsync($"api/project/{_projectId}");
    }

    [Fact]
    public async Task TriggerSync_FailsWithEmptyProject()
    {
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, _projectId);
        await FwHeadlessTestHelpers.ResetProjectToEmpty(HttpClient, _projectCode);
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncFinishedExpectingFailure(HttpClient, _projectId);
        result.Should().NotBeNull();
        result.Status.Should().Be(500);
        result.Detail.Should().NotBeNullOrEmpty();
        result.Detail.Should().Contain("Could not find file");
    }
}
