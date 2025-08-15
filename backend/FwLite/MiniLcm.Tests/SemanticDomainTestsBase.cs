using MiniLcm.Models;

namespace MiniLcm.Tests;

public abstract class SemanticDomainTestsBase : MiniLcmTestBase
{
    private readonly Guid _entryId = Guid.NewGuid();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        var semanticDomain = new SemanticDomain()
        {
            Id = Guid.NewGuid(), Name = new MultiString() { { "en", "new-semantic-domain" } }, Code = "1.0"
        };
        await Api.CreateSemanticDomain(semanticDomain);
        await Api.CreateSemanticDomain(new SemanticDomain()
        {
            Id = Guid.NewGuid(), Name = new MultiString() { { "en", "new-semantic-domain-2" } }, Code = "1.1"
        });

        await Api.CreateEntry(new Entry()
        {
            Id = _entryId,
            LexemeForm = { { "en", "new-lexeme-form" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "new-sense-gloss" } }, SemanticDomains = { semanticDomain } }
            ]
        });
    }

    private async Task<Entry> GetEntry()
    {
        var entry = await Api.GetEntry(_entryId);
        entry.Should().NotBeNull();
        return entry;
    }

    [Fact]
    public async Task GetSemanticDomains_ReturnsAllSemanticDomains()
    {
        var semanticDomains = await Api.GetSemanticDomains().ToArrayAsync();
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
        var entry = await GetEntry();
        entry.Should().NotBeNull();
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
    public async Task UpdateSense_AddSemanticDomain()
    {
        var entry = await GetEntry();
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var currentSemanticDomain = sense.SemanticDomains.First();
        var newSemanticDomain = await Api.GetSemanticDomains().FirstAsync(sd => sd.Id != currentSemanticDomain.Id);

        var updatedSense = sense.Copy();
        updatedSense.SemanticDomains.Add(newSemanticDomain);
        await Api.UpdateSense(entry.Id, sense, updatedSense);

        entry = await GetEntry();
        var actualSense = entry.Senses.First(s => s.Id == sense.Id);
        actualSense.SemanticDomains.Select(sd => sd.Id).Should().Contain(newSemanticDomain.Id);
    }

    [Fact]
    public async Task AddSemanticDomainToSense_AddSemanticDomain()
    {
        var entry = await GetEntry();
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var currentSemanticDomain = sense.SemanticDomains.First();
        var newSemanticDomain = await Api.GetSemanticDomains().FirstAsync(sd => sd.Id != currentSemanticDomain.Id);

        await Api.AddSemanticDomainToSense(sense.Id, newSemanticDomain);

        entry = await GetEntry();
        var actualSense = entry.Senses.First(s => s.Id == sense.Id);
        actualSense.SemanticDomains.Select(sd => sd.Id).Should().Contain(newSemanticDomain.Id);
    }

    [Fact]
    public async Task UpdateSense_RemoveSemanticDomain()
    {
        var entry = await GetEntry();
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var domainToRemove = sense.SemanticDomains[0];

        var updatedSense = sense.Copy();
        updatedSense.SemanticDomains = [..updatedSense.SemanticDomains.Where(sd => sd.Id != domainToRemove.Id)];
        await Api.UpdateSense(entry.Id, sense, updatedSense);

        entry = await GetEntry();
        ArgumentNullException.ThrowIfNull(entry);
        var actualSense = entry.Senses.First(s => s.Id == sense.Id);
        actualSense.SemanticDomains.Select(sd => sd.Id).Should().NotContain(domainToRemove.Id);
    }

    [Fact]
    public async Task RemoveSemanticDomainFromSense_RemoveSemanticDomain()
    {
        var entry = await GetEntry();
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var domainToRemove = sense.SemanticDomains[0];

        await Api.RemoveSemanticDomainFromSense(sense.Id, domainToRemove.Id);

        entry = await GetEntry();
        ArgumentNullException.ThrowIfNull(entry);
        var actualSense = entry.Senses.First(s => s.Id == sense.Id);
        actualSense.SemanticDomains.Select(sd => sd.Id).Should().NotContain(domainToRemove.Id);
    }
}
