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
    }

    [Fact]
    public async Task UpdateSense_UpdatesPartOfSpeech()
    {
        var entry = await Api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.PartOfSpeech is not null));
        var sense = entry.Senses.First(s => s.PartOfSpeech is not null);
        var newPartOfSpeech = await Api.GetPartsOfSpeech().FirstAsync(po => po.Id != sense.PartOfSpeechId);

        var updatedSense = sense.Copy();
        updatedSense.PartOfSpeech = newPartOfSpeech;
        updatedSense.PartOfSpeechId = newPartOfSpeech.Id;
        await Api.UpdateSense(entry.Id, sense, updatedSense);

        entry = await Api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var actualSense = entry.Senses.First(s => s.Id == sense.Id);
        actualSense.PartOfSpeechId.Should().Be(newPartOfSpeech.Id);
        actualSense.PartOfSpeech.Should()
        //the part of speech here is whatever the default is for the project, not english.
            .BeEquivalentTo(newPartOfSpeech);
    }

    [Fact]
    public async Task SetSensePartOfSpeech_UpdatesPartOfSpeech()
    {
        var entry = await Api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.PartOfSpeech is not null));
        var sense = entry.Senses.First(s => s.PartOfSpeech is not null);
        var newPartOfSpeech = await Api.GetPartsOfSpeech().FirstAsync(po => po.Id != sense.PartOfSpeechId);

        await Api.SetSensePartOfSpeech(sense.Id, newPartOfSpeech.Id);

        entry = await Api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var actualSense = entry.Senses.First(s => s.Id == sense.Id);
        actualSense.PartOfSpeechId.Should().Be(newPartOfSpeech.Id);
        actualSense.PartOfSpeech.Should()
        //the part of speech here is whatever the default is for the project, not english.
            .BeEquivalentTo(newPartOfSpeech);
    }

    [Fact]
    public async Task SetSensePartOfSpeech_HandlesMultipleNulls()
    {
        var entry = await Api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.PartOfSpeech is not null));
        var sense = entry.Senses.First(s => s.PartOfSpeech is not null);

        await Api.SetSensePartOfSpeech(sense.Id, null);
        await Api.SetSensePartOfSpeech(sense.Id, null);

        entry = await Api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var actualSense = entry.Senses.First(s => s.Id == sense.Id);
        actualSense.PartOfSpeechId.Should().Be(null);
    }

    [Fact]
    public async Task DeletePartOfSpeech_Works()
    {
        var posId = Guid.NewGuid();
        var pos = new PartOfSpeech() { Id = posId, Name = { { "en", "Test POS" } } };
        await Api.CreatePartOfSpeech(pos);

        await Api.DeletePartOfSpeech(posId);

        var partOfSpeech = await Api.GetPartOfSpeech(posId);
        partOfSpeech.Should().BeNull();
    }

    [Fact]
    public async Task DeletePartOfSpeech_WorksWhenUsed()
    {
        var posId = Guid.NewGuid();
        var pos = new PartOfSpeech() { Id = posId, Name = { { "en", "Test POS" } } };
        await Api.CreatePartOfSpeech(pos);
        var entryId = Guid.NewGuid();
        await Api.CreateEntry(new Entry()
        {
            Id = entryId,
            LexemeForm = { { "en", "Apple" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Fruit" } }, PartOfSpeechId = posId }
            ]
        });

        await Api.DeletePartOfSpeech(posId);

        var partOfSpeech = await Api.GetPartOfSpeech(posId);
        partOfSpeech.Should().BeNull();

        // verify the referencing sense Pos is null
        var entry = await Api.GetEntry(entryId);
        entry.Should().NotBeNull();
        var sense = entry!.Senses.Single();
        sense.PartOfSpeechId.Should().BeNull();
        sense.PartOfSpeech.Should().BeNull();
    }
}
