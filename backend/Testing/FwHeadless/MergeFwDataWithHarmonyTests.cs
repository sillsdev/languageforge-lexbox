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

    private async Task AddWritingSystemCommit(Guid projectId, string writingSystemId = "en")
    {
        var wsGuid = Guid.NewGuid();
        var change = JsonSerializer.Deserialize<ServerJsonChange>(
            $$"""
                {
                    "$type": "CreateWritingSystemChange",
                    "WsId": "{{writingSystemId}}",
                    "Name": "TestWritingSystem",
                    "Abbreviation": "{{writingSystemId}}",
                    "Type": 1,
                    "Order": 1,
                    "EntityId": "{{wsGuid}}"
                }
                """
        ) ?? throw new JsonException("unable to deserialize");

        ServerCommit[] serverCommits =
        [
            new ServerCommit(Guid.NewGuid())
            {
                ChangeEntities =
                [
                    new ChangeEntity<ServerJsonChange>()
                    {
                        Change = change,
                        Index = 0,
                        CommitId = Guid.NewGuid(),
                        EntityId = wsGuid
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

    private async Task AddTestCommit(Guid projectId, string writingSystemId = "en")
    {
        var entryId = Guid.NewGuid();
        var change = JsonSerializer.Deserialize<ServerJsonChange>(
            $$"""
                {
                "$type": "CreateEntryChange",
                "LexemeForm": {
                    "{{writingSystemId}}": "Apple"
                },
                "CitationForm": {
                    "{{writingSystemId}}": "Apple"
                },
                "Note": {},
                "EntityId": "{{entryId}}"
                }
                """
        ) ?? throw new JsonException("unable to deserialize");

        ServerCommit[] serverCommits =
        [
            new ServerCommit(Guid.NewGuid())
            {
                ChangeEntities =
                [
                    new ChangeEntity<ServerJsonChange>()
                    {
                        Change = change,
                        Index = 0,
                        CommitId = Guid.NewGuid(),
                        EntityId = entryId
                    }
                ],
                ClientId = Guid.NewGuid(),
                ProjectId = projectId,
                HybridDateTime = new HybridDateTime(DateTime.UtcNow, 1)
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

    [Fact]
    public async Task TriggerSync_FailsWithInvalidCommit()
    {
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, _projectId);
        var needCleanup = false;

        try
        {
            await AddTestCommit(_projectId, writingSystemId: "fr"); // Should not exist in the project
            needCleanup = true;
            await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
            var result = await FwHeadlessTestHelpers.AwaitSyncFinishedExpectingFailure(HttpClient, _projectId);
            result.Should().NotBeNull();
            result.Detail.Should().NotBeNullOrEmpty();
            Console.WriteLine(result.Detail);
            result.Status.Should().Be(500);
            result.Detail.Should().StartWith("Failed to create entry");
        }
        finally
        {
            if (needCleanup)
            {
                await AddWritingSystemCommit(_projectId, writingSystemId: "fr");
                // Now the entry change should be valid and we should be able to sync the project again
                await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
                await FwHeadlessTestHelpers.AwaitSyncFinished(HttpClient, _projectId);
            }
        }
    }
}
