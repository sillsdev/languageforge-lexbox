using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class SemanticDomainTests(ProjectLoaderFixture fixture) : IAsyncLifetime
{
    private FwDataMiniLcmApi _api = null!;

    public async Task InitializeAsync()
    {
        var projectName = "ws-test_" + Guid.NewGuid();
        fixture.MockFwProjectLoader.NewProject(projectName, "en", "en");
        _api = fixture.CreateApi(projectName);
        _api.Should().NotBeNull();

        var semanticDomain = new SemanticDomain()
        {
            Id = Guid.NewGuid(), Name = new MultiString() { { "en", "new-semantic-domain" } }, Code = "1.0"
        };
        await _api.CreateSemanticDomain(semanticDomain);
        await _api.CreateSemanticDomain(new SemanticDomain() { Id = Guid.NewGuid(), Name = new MultiString() { { "en", "new-semantic-domain-2" } }, Code = "1.1" });

        await _api.CreateEntry(new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "new-lexeme-form" } },
            Senses = new List<Sense>()
            {
                new Sense() { Gloss = { { "en", "new-sense-gloss" } }, SemanticDomains = { semanticDomain } }
            }
        });
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetSemanticDomains_ReturnsAllSemanticDomains()
    {
        var semanticDomains = await _api.GetSemanticDomains().ToArrayAsync();
        semanticDomains.Should().AllSatisfy(sd =>
        {
            sd.Id.Should().NotBe(Guid.Empty);
            sd.Name.Values.Should().NotBeEmpty();
            sd.Code.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task Sense_HasSemanticDomains()
    {
        var entry = await _api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.SemanticDomains.Any()));
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        sense.SemanticDomains.Should().NotBeEmpty();
        sense.SemanticDomains.Should().AllSatisfy(sd =>
        {
            sd.Id.Should().NotBe(Guid.Empty);
            sd.Name.Values.Should().NotBeEmpty();
            sd.Code.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task Sense_AddSemanticDomain()
    {
        var entry = await _api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.SemanticDomains.Any()));
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var currentSemanticDomain = sense.SemanticDomains.First();
        var newSemanticDomain = await _api.GetSemanticDomains().FirstAsync(sd => sd.Id != currentSemanticDomain.Id);

        var update = new UpdateObjectInput<Sense>()
            .Add(s => s.SemanticDomains, newSemanticDomain);
        await _api.UpdateSense(entry.Id, sense.Id, update);

        entry = await _api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.SemanticDomains.Select(sd => sd.Id).Should().Contain(newSemanticDomain.Id);
    }

    [Fact]
    public async Task Sense_RemoveSemanticDomain()
    {
        var entry = await _api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.SemanticDomains.Any()));
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var domainToRemove = sense.SemanticDomains[0];

        var update = new UpdateObjectInput<Sense>()
            .Remove(s => s.SemanticDomains, 0);
        await _api.UpdateSense(entry.Id, sense.Id, update);

        entry = await _api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.SemanticDomains.Select(sd => sd.Id).Should().NotContain(domainToRemove.Id);
    }
}
