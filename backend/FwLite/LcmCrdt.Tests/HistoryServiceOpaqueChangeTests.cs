using System.Text.Json;
using SIL.Harmony;
using SIL.Harmony.Changes;

namespace LcmCrdt.Tests;

// A change type from a newer client that this client doesn't have registered deserializes as
// OpaqueChange (null EntityType, apply skipped), and the activity view must tolerate it.
public class HistoryServiceOpaqueChangeTests : HistoryServiceActivityTestsBase
{
    // Only a newer client can author it (authoring an unregistered type throws), so it arrives as sync JSON.
    private async Task<Commit> SyncUnknownChange(Guid entityId)
    {
        var commitId = Guid.NewGuid();
        var json = $$$"""
            {"Id":"{{{commitId}}}","ClientId":"{{{Guid.NewGuid()}}}",
             "HybridDateTime":{"DateTime":"{{{DateTimeOffset.UtcNow:O}}}","Counter":0},
             "Metadata":{"AuthorName":"A","AuthorId":"a"},
             "ChangeEntities":[{"Index":0,"CommitId":"{{{commitId}}}","EntityId":"{{{entityId}}}",
               "Change":{"$type":"ChangeFromTheFuture","EntityId":"{{{entityId}}}","SomeValue":7}}]}
            """;
        var commit = JsonSerializer.Deserialize<Commit>(json, Fixture.HarmonyConfig.JsonSerializerOptions)!;
        commit.ChangeEntities.Single().Change.Should().BeOfType<OpaqueChange>();
        await ((ISyncable)DataModel).AddRangeFromSync([commit]);
        return commit;
    }

    [Fact]
    public async Task ProjectActivity_ToleratesOpaqueChanges()
    {
        var entryId = await CreateEntry("apple");
        await SyncUnknownChange(entryId);

        var activities = await Service.ProjectActivity();

        var activity = activities.Should().ContainSingle(a => a.Changes.Any(c => c.Entity.Change is OpaqueChange)).Subject;
        activity.ChangeName.Should().Be("Unknown");
        var info = activity.Changes.Single().Info;
        info.Entry.Should().BeNull();
        info.Sense.Should().BeNull();
        info.ObjectName.Should().BeNull();
    }

    [Fact]
    public async Task LoadChangeContext_ToleratesOpaqueEditOfExistingEntity()
    {
        var entryId = await CreateEntry("apple");
        var commit = await SyncUnknownChange(entryId);

        var context = await Service.LoadChangeContext(commit.Id, 0);

        context.ChangeName.Should().Be("Unknown");
        context.Snapshot.Should().BeNull();
        context.AffectedEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadChangeContext_ToleratesOpaqueCreateOfUnknownEntity()
    {
        // An opaque create: the entity was never materialized on this client, so it has no snapshots.
        var commit = await SyncUnknownChange(Guid.NewGuid());

        var context = await Service.LoadChangeContext(commit.Id, 0);

        context.ChangeName.Should().Be("Unknown");
        context.Snapshot.Should().BeNull();
        context.AffectedEntries.Should().BeEmpty();
    }
}
