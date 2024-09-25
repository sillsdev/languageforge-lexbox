using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class PartOfSpeechTests(ProjectLoaderFixture fixture) : IAsyncLifetime
{
    private FwDataMiniLcmApi _api = null!;

    public async Task InitializeAsync()
    {
        var projectName = "part-of-speech_" + Guid.NewGuid();
        fixture.MockFwProjectLoader.NewProject(projectName, "en", "en");
        _api = fixture.CreateApi(projectName);
        _api.Should().NotBeNull();

        var nounPos = new PartOfSpeech()
        {
            Id = Guid.NewGuid(), Name = { { "en", "Noun" } }
        };
        await _api.CreatePartOfSpeech(nounPos);

        await _api.CreatePartOfSpeech(new() { Id = Guid.NewGuid(), Name = { { "en", "Verb" } } });

        await _api.CreateEntry(new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = {{"en", "Apple"}},
            Senses = new List<Sense>()
            {
                new Sense()
                {
                    Gloss = {{"en", "Fruit"}},
                    PartOfSpeechId = nounPos.Id
                }
        }});
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetPartsOfSpeech_ReturnsAllPartsOfSpeech()
    {
        var partOfSpeeches = await _api.GetPartsOfSpeech().ToArrayAsync();
        partOfSpeeches.Should()
            .NotBeEmpty()
            .And
            .AllSatisfy(po => po.Id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public async Task Sense_HasPartOfSpeech()
    {
        var entry = await _api.GetEntries().FirstAsync(e => e.Senses.Any(s => !string.IsNullOrEmpty(s.PartOfSpeech)));
        var sense = entry.Senses.First(s => !string.IsNullOrEmpty(s.PartOfSpeech));
        sense.PartOfSpeech.Should().NotBeNullOrEmpty();
        sense.PartOfSpeechId.Should().NotBeNull();
    }

    [Fact]
    public async Task Sense_UpdatesPartOfSpeech()
    {
        var entry = await _api.GetEntries().FirstAsync(e => e.Senses.Any(s => !string.IsNullOrEmpty(s.PartOfSpeech)));
        var sense = entry.Senses.First(s => !string.IsNullOrEmpty(s.PartOfSpeech));
        var newPartOfSpeech = await _api.GetPartsOfSpeech().FirstAsync(po => po.Id != sense.PartOfSpeechId);

        var update = new UpdateObjectInput<Sense>()
            .Set(s => s.PartOfSpeech,
                newPartOfSpeech.Name
                    ["en"]) //this won't actually update the part of speech, but it shouldn't cause an issue either.
            .Set(s => s.PartOfSpeechId, newPartOfSpeech.Id);
        await _api.UpdateSense(entry.Id, sense.Id, update);

        entry = await _api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.PartOfSpeechId.Should().Be(newPartOfSpeech.Id);
        updatedSense.PartOfSpeech.Should()
            .NotBe(sense
                .PartOfSpeech); //the part of speech here is whatever the default is for the project, not english.
    }
}
