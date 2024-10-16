using MiniLcm.Models;

namespace MiniLcm.Tests;

public abstract class PartOfSpeechTestsBase : MiniLcmTestBase
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        var nounPos = new PartOfSpeech() { Id = Guid.NewGuid(), Name = { { "en", "Noun" } } };
        await Api.CreatePartOfSpeech(nounPos);

        await Api.CreatePartOfSpeech(new() { Id = Guid.NewGuid(), Name = { { "en", "Verb" } } });

        await Api.CreateEntry(new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "Apple" } },
            Senses = new List<Sense>()
            {
                new Sense() { Gloss = { { "en", "Fruit" } }, PartOfSpeechId = nounPos.Id }
            }
        });
    }

    [Fact]
    public async Task GetPartsOfSpeech_ReturnsAllPartsOfSpeech()
    {
        var partOfSpeeches = await Api.GetPartsOfSpeech().ToArrayAsync();
        partOfSpeeches.Should()
            .NotBeEmpty()
            .And
            .AllSatisfy(po => po.Id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public async Task Sense_HasPartOfSpeech()
    {
        var entry = await Api.GetEntries().FirstAsync(e => e.Senses.Any(s => !string.IsNullOrEmpty(s.PartOfSpeech)));
        var sense = entry.Senses.First(s => !string.IsNullOrEmpty(s.PartOfSpeech));
        sense.PartOfSpeech.Should().NotBeNullOrEmpty();
        sense.PartOfSpeechId.Should().NotBeNull();
    }

    [Fact]
    public async Task Sense_UpdatesPartOfSpeech()
    {
        var entry = await Api.GetEntries().FirstAsync(e => e.Senses.Any(s => !string.IsNullOrEmpty(s.PartOfSpeech)));
        var sense = entry.Senses.First(s => !string.IsNullOrEmpty(s.PartOfSpeech));
        var newPartOfSpeech = await Api.GetPartsOfSpeech().FirstAsync(po => po.Id != sense.PartOfSpeechId);

        var update = new UpdateObjectInput<Sense>()
            .Set(s => s.PartOfSpeech,
                newPartOfSpeech.Name
                    ["en"]) //this won't actually update the part of speech, but it shouldn't cause an issue either.
            .Set(s => s.PartOfSpeechId, newPartOfSpeech.Id);
        await Api.UpdateSense(entry.Id, sense.Id, update);

        entry = await Api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.PartOfSpeechId.Should().Be(newPartOfSpeech.Id);
        updatedSense.PartOfSpeech.Should()
            .NotBe(sense
                .PartOfSpeech); //the part of speech here is whatever the default is for the project, not english.
    }
}
