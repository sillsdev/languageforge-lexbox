using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LexCore.Sync;
using SIL.Harmony.Core;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.FwHeadless;

[Trait("Category", "Integration")]
public class MergeFwDataWithHarmonyTests : ApiTestBase, IAsyncLifetime
{
    private async Task<Guid> CopyProjectToNewProject(string newProjectCode, string existingProjectCode)
    {
        var result = await HttpClient.PostAsync(
            $"api/Testing/copyToNewProject?newProjectCode={newProjectCode}&existingProjectCode={existingProjectCode}",
            null);
        result.EnsureSuccessStatusCode();
        await Utils.WaitForHgRefreshIntervalAsync();
        return await result.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task TriggerSync(Guid projectId)
    {
        var result = await HttpClient.PostAsync($"api/fw-lite/sync/trigger/{projectId}", null);
        if (result.IsSuccessStatusCode) return;
        var responseString = await result.Content.ReadAsStringAsync();
        Assert.Fail($"trigger failed with error {result.ReasonPhrase}, body: {responseString}" );
    }

    private async Task<SyncResult?> AwaitSyncFinished(Guid projectId)
    {
        var giveUpAt = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (giveUpAt > DateTime.UtcNow)
        {
            try
            {
                var result = await HttpClient.GetAsync($"api/fw-lite/sync/await-sync-finished/{projectId}", new CancellationTokenSource(TimeSpan.FromSeconds(25)).Token);
                result.EnsureSuccessStatusCode();
                return await result.Content.ReadFromJsonAsync<SyncResult>();
            }
            catch (OperationCanceledException)
            {
            }
        }
        Assert.Fail("timed out waiting for sync to finish");
        return null;
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

    private Guid _projectId;

    public async Task InitializeAsync()
    {
        await LoginAs("admin");
        var projectCode = Utils.NewProjectCode();
        _projectId = await CopyProjectToNewProject(projectCode, "sena-3");
    }

    public async Task DisposeAsync()
    {
        await HttpClient.DeleteAsync($"api/project/{_projectId}");
    }

    [Fact]
    public async Task TriggerSync_WorksTheFirstTime()
    {
        await TriggerSync(_projectId);
        var result = await AwaitSyncFinished(_projectId);
        result.Should().NotBeNull();
        result.CrdtChanges.Should().BeGreaterThan(100);
        result.FwdataChanges.Should().Be(0);
    }

    [Fact]
    public async Task TriggerSync_WorksWithSomeCommits()
    {
        await TriggerSync(_projectId);
        await AwaitSyncFinished(_projectId);

        await AddTestCommit(_projectId);
        await TriggerSync(_projectId);
        var result = await AwaitSyncFinished(_projectId);
        result.Should().NotBeNull();
        result.CrdtChanges.Should().Be(0);
        result.FwdataChanges.Should().BeGreaterThan(0);
    }
}
