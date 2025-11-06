using SIL.Harmony.Changes;
using LcmCrdt.Changes;
using Microsoft.EntityFrameworkCore;
using SIL.Harmony.Db;

namespace LcmCrdt.Tests.Changes;

public class SenseChangeTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    [Fact]
    public async Task AddSenseAndUpdatePartOfSpeechInOneCommit()
    {
        // arrange
        var entry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "test entry" } }, });
        var partOfSpeech = await fixture.Api.CreatePartOfSpeech(new() { Name = new() { { "en", "test pos" } } });
        var sense = new Sense() { Id = Guid.NewGuid(), Gloss = new() { { "en", "test sense" } } };

        var createSenseChange = new CreateSenseChange(sense, entry.Id);
        var setPartOfSpeechChange = new SetPartOfSpeechChange(sense.Id, partOfSpeech.Id);
        List<IChange> changes = [createSenseChange, setPartOfSpeechChange];

        // act
        await fixture.DataModel.AddChanges(Guid.NewGuid(), changes);

        // assert
        var actualSense = await fixture.Api.GetSense(entry.Id, sense.Id);
        actualSense.Should().NotBeNull();
        actualSense.PartOfSpeechId.Should().Be(partOfSpeech.Id);
    }

    [Fact]
    public async Task SetPartOfSpeechChange_PartOfSpeechIsAvailableInSnapshot()
    {
        // arrange
        var entry = await fixture.Api.CreateEntry(new() { LexemeForm = { { "en", "test entry" } }, });
        var partOfSpeech = await fixture.Api.CreatePartOfSpeech(new() { Name = new() { { "en", "test pos" } } });
        var sense = await fixture.Api.CreateSense(entry.Id, new Sense() { Gloss = new() { { "en", "test sense" } } });

        // act
        await fixture.Api.SetSensePartOfSpeech(sense.Id, partOfSpeech.Id);

        // assert - verify the PartOfSpeech object is populated in the snapshot
        var dbContext = (ICrdtDbContext)fixture.DbContext;
        var snapshots = await dbContext.Snapshots
            .Where(s => s.EntityId == sense.Id)
            .Include(s => s.Commit)
            .ToListAsync();

        var snapshot = snapshots.OrderByDescending(s => s.Commit.HybridDateTime).FirstOrDefault();
        snapshot.Should().NotBeNull();
        var snapshotSense = snapshot!.Entity.DbObject as Sense;
        snapshotSense.Should().NotBeNull();
        snapshotSense!.PartOfSpeechId.Should().Be(partOfSpeech.Id);
        snapshotSense.PartOfSpeech.Should().NotBeNull("PartOfSpeech should be populated in the snapshot");
        snapshotSense.PartOfSpeech!.Id.Should().Be(partOfSpeech.Id);
        snapshotSense.PartOfSpeech.Name["en"].Should().Be("test pos");
    }
}
