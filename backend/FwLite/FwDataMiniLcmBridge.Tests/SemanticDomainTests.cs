using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class SemanticDomainTests(ProjectLoaderFixture fixture)
{
    [Fact]
    public async Task GetSemanticDomains_ReturnsAllSemanticDomains()
    {
        var api = fixture.CreateApi("sena-3");
        var semanticDomains = await api.GetSemanticDomains().ToArrayAsync();
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
        var api = fixture.CreateApi("sena-3");
        var entry = await api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.SemanticDomains.Any()));
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
        var api = fixture.CreateApi("sena-3");
        var entry = await api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.SemanticDomains.Any()));
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var currentSemanticDomain = sense.SemanticDomains.First();
        var newSemanticDomain = await api.GetSemanticDomains().FirstAsync(sd => sd.Id != currentSemanticDomain.Id);

        var update = api.CreateUpdateBuilder<Sense>()
            .Add(s => s.SemanticDomains, newSemanticDomain)
            .Build();
        await api.UpdateSense(entry.Id, sense.Id, update);

        entry = await api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.SemanticDomains.Select(sd => sd.Id).Should().Contain(newSemanticDomain.Id);
    }

    [Fact]
    public async Task Sense_RemoveSemanticDomain()
    {
        var api = fixture.CreateApi("sena-3");
        var entry = await api.GetEntries().FirstAsync(e => e.Senses.Any(s => s.SemanticDomains.Any()));
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var domainToRemove = sense.SemanticDomains[0];

        var update = api.CreateUpdateBuilder<Sense>()
            .Remove(s => s.SemanticDomains, 0)
            .Build();
        await api.UpdateSense(entry.Id, sense.Id, update);

        entry = await api.GetEntry(entry.Id);
        ArgumentNullException.ThrowIfNull(entry);
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.SemanticDomains.Select(sd => sd.Id).Should().NotContain(domainToRemove.Id);
    }
}
