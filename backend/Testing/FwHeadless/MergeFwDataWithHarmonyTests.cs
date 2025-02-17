using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LexCore.Sync;
using SIL.Harmony.Core;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.FwHeadless;

public class MergeFwDataWithHarmonyTests : ApiTestBase
{
    private async Task<Guid> CopyProjectToNewProject(string newProjectCode, string existingProjectCode)
    {
        var result = await HttpClient.PostAsync(
            $"api/Testing/copyToNewProject?newProjectCode={newProjectCode}&existingProjectCode={existingProjectCode}",
            null);
        result.EnsureSuccessStatusCode();
        return await result.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task TriggerSync(Guid projectId)
    {
        var result = await HttpClient.PostAsync($"api/fw-lite/sync/trigger/{projectId}", null);
        result.EnsureSuccessStatusCode();
    }

    private async Task<SyncResult?> AwaitSyncFinished(Guid projectId)
    {
        var result = await HttpClient.GetAsync($"api/fw-lite/sync/await-sync-finished/{projectId}");
        result.EnsureSuccessStatusCode();
        return await result.Content.ReadFromJsonAsync<SyncResult>();
    }

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

    [Fact]
    public async Task TriggerSync_WorksTheFirstTime()
    {
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await CopyProjectToNewProject(projectCode, "sena-3");
        await TriggerSync(projectId);
        var result = await AwaitSyncFinished(projectId);
        result.Should().NotBeNull();
        result.CrdtChanges.Should().BeGreaterThan(100);
        result.FwdataChanges.Should().Be(0);
    }

    [Fact]
    public async Task TriggerSync_WorksWithSomeCommits()
    {
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        var projectId = await CopyProjectToNewProject(projectCode, "sena-3");
        await TriggerSync(projectId);
        await AwaitSyncFinished(projectId);

        await AddTestCommit(projectId);
        await TriggerSync(projectId);
        var result = await AwaitSyncFinished(projectId);
        result.Should().NotBeNull();
        result.CrdtChanges.Should().Be(0);
        result.FwdataChanges.Should().BeGreaterThan(0);
    }
}
