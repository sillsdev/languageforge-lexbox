using System.Text.Json;
using SIL.Harmony.Changes;

namespace LcmCrdt.Tests;

// A change type from a newer client that this client doesn't have registered deserializes as
// OpaqueChange (null EntityType, apply skipped), and the activity view must tolerate it.
public class HistoryServiceOpaqueChangeTests : HistoryServiceActivityTestsBase
{

    private static OpaqueChange UnknownChange(Guid entityId)
    {
        using var doc = JsonDocument.Parse(
            $$"""{"$type":"ChangeFromTheFuture","EntityId":"{{entityId}}","SomeValue":7}""");
        return new OpaqueChange
        {
            TypeName = "ChangeFromTheFuture",
            EntityId = entityId,
            RawJson = doc.RootElement.Clone()
        };
    }

    [Fact]
    public async Task ProjectActivity_ToleratesOpaqueChanges()
    {
        var entryId = await CreateEntry("apple");
        await DataModel.AddChange(ClientId, UnknownChange(entryId), Meta());

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
        var commit = await DataModel.AddChange(ClientId, UnknownChange(entryId), Meta());

        var context = await Service.LoadChangeContext(commit.Id, 0);

        context.ChangeName.Should().Be("Unknown");
        context.Snapshot.Should().BeNull();
        context.AffectedEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadChangeContext_ToleratesOpaqueCreateOfUnknownEntity()
    {
        // An opaque create: the entity was never materialized on this client, so it has no snapshots.
        var commit = await DataModel.AddChange(ClientId, UnknownChange(Guid.NewGuid()), Meta());

        var context = await Service.LoadChangeContext(commit.Id, 0);

        context.ChangeName.Should().Be("Unknown");
        context.Snapshot.Should().BeNull();
        context.AffectedEntries.Should().BeEmpty();
    }
}
