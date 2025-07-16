using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SIL.Harmony.Core;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.FwHeadless;

[Trait("Category", "Integration")]
public class MergeFwDataWithHarmonyTests : ApiTestBase, IAsyncLifetime
{
    private async Task AddTestCommit(Guid projectId)
    {
        var entryId = Guid.NewGuid();
        ServerCommit[] serverCommits =
        [
            new ServerCommit(Guid.NewGuid())
            {
                ChangeEntities =
                [
                    new ChangeEntity<ServerJsonChange>()
                    {
                        Change = JsonSerializer.Deserialize<ServerJsonChange>(
                            $$"""
                              {
                                "$type": "CreateEntryChange",
                                "LexemeForm": {
                                  "en": "Apple"
                                },
                                "CitationForm": {
                                  "en": "Apple"
                                },
                                "Note": {},
                                "EntityId": "{{entryId}}"
                              }
                              """
                        ) ?? throw new JsonException("unable to deserialize"),
                        Index = 0,
                        CommitId = Guid.NewGuid(),
                        EntityId = entryId
                    }
                ],
                ClientId = Guid.NewGuid(),
                ProjectId = projectId,
                HybridDateTime = new HybridDateTime(DateTime.UtcNow, 0)
            }
        ];
        var result = await HttpClient.PostAsJsonAsync($"api/crdt/{projectId}/add", serverCommits);
        result.EnsureSuccessStatusCode();
    }

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
        var result = await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, _projectId);
        result.Should().NotBeNull();
        result.CrdtChanges.Should().BeGreaterThan(100);
        result.FwdataChanges.Should().Be(0);

        // there should be a short grace period during which the result remains available
        var result2 = await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, _projectId);
        result2.Should().BeEquivalentTo(result);
    }

    [Fact]
    public async Task TriggerSync_WorksWithSomeCommits()
    {
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, _projectId);

        await AddTestCommit(_projectId);
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, _projectId);
        result.Should().NotBeNull();
        result.CrdtChanges.Should().Be(0);
        result.FwdataChanges.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    // Don't wait more than 10 seconds, otherwise the sync could complete before we can cancel it
    public async Task TriggerSync_CanBeCancelled(int waitSeconds)
    {
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        var result1 = await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, _projectId);
        result1.Should().NotBeNull();

        await AddTestCommit(_projectId);
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        Thread.Sleep(TimeSpan.FromSeconds(waitSeconds));
        await FwHeadlessTestHelpers.CancelSync(HttpClient, _projectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, _projectId);
        result.Should().NotBeNull();
        // Depending on whether the second sync started before being canceled, we may be seeing the cached result from the first sync
        result.CrdtChanges.Should().BeOneOf(0, result1.CrdtChanges);
        result.FwdataChanges.Should().Be(0);
    }
}
