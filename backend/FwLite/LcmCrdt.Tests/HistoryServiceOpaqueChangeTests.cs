using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SIL.Harmony.Changes;
using SIL.Harmony.Config;

namespace LcmCrdt.Tests;

// With UnknownChangeHandling.Fallback, a change type from a newer client that this client doesn't
// have registered deserializes as OpaqueChange (null EntityType, apply skipped) — the activity view
// must tolerate it. Production still uses the default Throw (sync fails instead); only this fixture
// opts into Fallback, covering the activity view for when it's turned on.
public class HistoryServiceOpaqueChangeTests : HistoryServiceActivityTestsBase
{
    protected override MiniLcmApiFixture CreateFixture() => MiniLcmApiFixture.Create(configureServices: services =>
        services.Configure<HarmonyConfig>(config => config.UnknownChangeHandling = UnknownChangeHandling.Fallback));

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
        activity.Changes.Single().Info.Subject.Should().BeNull();
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
