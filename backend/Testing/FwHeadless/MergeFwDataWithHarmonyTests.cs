using FluentAssertions;
using LexCore.Sync;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.FwHeadless;

[Trait("Category", "Integration")]
[Collection("FwHeadless Sync")]
public class MergeFwDataWithHarmonyTests : ApiTestBase, IAsyncLifetime
{
    private Guid _projectId;

    public async Task InitializeAsync()
    {
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        _projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, projectCode, "sena-3");
    }

    public async Task DisposeAsync()
    {
        await HttpClient.DeleteAsync($"api/project/{_projectId}");
    }

    [Fact]
    public async Task TriggerSync_WorksTheFirstTime()
    {
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, _projectId);
        result.Should().NotBeNull();
        result.Status.Should().Be(SyncJobStatusEnum.Success);
        result.SyncResult.Should().NotBeNull();
        result.SyncResult.CrdtChanges.Should().BeGreaterThan(100);
        result.SyncResult.FwdataChanges.Should().Be(0);

        // there should be a short grace period during which the result remains available
        var result2 = await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, _projectId);
        result2.Should().BeEquivalentTo(result);
    }

    [Fact]
    public async Task TriggerSync_WorksWithSomeCommits()
    {
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, _projectId);

        await FwHeadlessTestHelpers.AddCreateEntryCommit(HttpClient, _projectId);
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, _projectId);
        result.Should().NotBeNull();
        result.Status.Should().Be(SyncJobStatusEnum.Success);
        result.SyncResult.Should().NotBeNull();
        result.SyncResult.CrdtChanges.Should().Be(0);
        result.SyncResult.FwdataChanges.Should().BeGreaterThan(0);
    }
}
