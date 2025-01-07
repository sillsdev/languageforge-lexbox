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
        var entry = await Api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.PartOfSpeech is not null));
        var sense = entry.Senses.First(s => s.PartOfSpeech is not null);
        sense.PartOfSpeech.Should().NotBeNull();
        sense.PartOfSpeechId.Should().NotBeNull();
        // TODO: This test becomes meaningless now that PartOfSpeech is an object. Rewrite, or toss?
    }

    [Fact]
    public async Task Sense_UpdatesPartOfSpeech()
    {
        var entry = await Api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.PartOfSpeech is not null));
        var sense = entry.Senses.First(s => s.PartOfSpeech is not null);
        var newPartOfSpeech = await Api.GetPartsOfSpeech().FirstAsync(po => po.Id != sense.PartOfSpeechId);

        var update = new UpdateObjectInput<Sense>()
            //This is required for CRDTs, but not for FW
            .Set(s => s.PartOfSpeech, newPartOfSpeech)
            .Set(s => s.PartOfSpeechId, newPartOfSpeech.Id);
        await Api.UpdateSense(entry.Id, sense.Id, update);

        entry = await Api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.PartOfSpeechId.Should().Be(newPartOfSpeech.Id);
        updatedSense.PartOfSpeech.Should()
        //the part of speech here is whatever the default is for the project, not english.
            .BeEquivalentTo(newPartOfSpeech);
    }
}
