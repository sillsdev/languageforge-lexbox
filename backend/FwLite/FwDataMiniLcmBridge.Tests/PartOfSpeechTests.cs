using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class PartOfSpeechTests(ProjectLoaderFixture fixture)
{
    [Fact]
    public async Task GetPartsOfSpeech_ReturnsAllPartsOfSpeech()
    {
        var api = fixture.CreateApi("sena-3");
        var partOfSpeeches = await api.GetPartsOfSpeech().ToArrayAsync();
        partOfSpeeches.Should().AllSatisfy(po => po.Id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public async Task Sense_HasPartOfSpeech()
    {
        var api = fixture.CreateApi("sena-3");
        var entry = await api.GetEntries().FirstAsync(e => e.Senses.Any(s => !string.IsNullOrEmpty(s.PartOfSpeech)));
        var sense = entry.Senses.First(s => !string.IsNullOrEmpty(s.PartOfSpeech));
        sense.PartOfSpeech.Should().NotBeNullOrEmpty();
        sense.PartOfSpeechId.Should().NotBeNull();
    }

    [Fact]
    public async Task Sense_UpdatesPartOfSpeech()
    {
        var api = fixture.CreateApi("sena-3");
        var entry = await api.GetEntries().FirstAsync(e => e.Senses.Any(s => !string.IsNullOrEmpty(s.PartOfSpeech)));
        var sense = entry.Senses.First(s => !string.IsNullOrEmpty(s.PartOfSpeech));
        var newPartOfSpeech = await api.GetPartsOfSpeech().FirstAsync(po => po.Id != sense.PartOfSpeechId);

        var update = api.CreateUpdateBuilder<Sense>()
            .Set(s => s.PartOfSpeech, newPartOfSpeech.Name["en"])//this won't actually update the part of speech, but it shouldn't cause an issue either.
            .Set(s => s.PartOfSpeechId, newPartOfSpeech.Id)
            .Build();
        await api.UpdateSense(entry.Id, sense.Id, update);

        entry = await api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.PartOfSpeechId.Should().Be(newPartOfSpeech.Id);
        updatedSense.PartOfSpeech.Should().NotBe(sense.PartOfSpeech);//the part of speech here is whatever the default is for the project, not english.
    }
}
