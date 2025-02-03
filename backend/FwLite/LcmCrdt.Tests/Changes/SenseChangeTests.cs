using SIL.Harmony.Changes;
using LcmCrdt.Changes;

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
}
