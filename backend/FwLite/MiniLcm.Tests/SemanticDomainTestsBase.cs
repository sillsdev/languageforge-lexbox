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

    private Task<Entry> GetEntry()
    {
        var entry = Api.GetEntry(_entryId);
        entry.Should().NotBeNull();
        return entry!;
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
        var sense = entry!.Senses.First(s => s.SemanticDomains.Any());
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
        var entry = await GetEntry();
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var currentSemanticDomain = sense.SemanticDomains.First();
        var newSemanticDomain = await Api.GetSemanticDomains().FirstAsync(sd => sd.Id != currentSemanticDomain.Id);

        var update = new UpdateObjectInput<Sense>()
            .Add(s => s.SemanticDomains, newSemanticDomain);
        await Api.UpdateSense(entry.Id, sense.Id, update);

        entry = await GetEntry();
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.SemanticDomains.Select(sd => sd.Id).Should().Contain(newSemanticDomain.Id);
    }

    [Fact]
    public async Task Sense_RemoveSemanticDomain()
    {
        var entry = await GetEntry();
        var sense = entry.Senses.First(s => s.SemanticDomains.Any());
        var domainToRemove = sense.SemanticDomains[0];

        var update = new UpdateObjectInput<Sense>()
            .Remove(s => s.SemanticDomains, 0);
        await Api.UpdateSense(entry.Id, sense.Id, update);

        entry = await GetEntry();
        ArgumentNullException.ThrowIfNull(entry);
        var updatedSense = entry.Senses.First(s => s.Id == sense.Id);
        updatedSense.SemanticDomains.Select(sd => sd.Id).Should().NotContain(domainToRemove.Id);
    }
}
