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
            Id = Guid.NewGuid(),
            Name = new MultiString() { { "en", "new-semantic-domain" } },
            Abbreviation = new MultiString() { { "en", "1.0" } },
        };
        await Api.CreateSemanticDomain(semanticDomain);
        await Api.CreateSemanticDomain(new SemanticDomain()
        {
            Id = Guid.NewGuid(),
            Name = new MultiString() { { "en", "new-semantic-domain-2" } },
            Abbreviation = new MultiString() { { "en", "1.1" } },
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
    public async Task CreateSemanticDomain_RoundTripsExtendedFields()
    {
        var id = Guid.NewGuid();
        var created = await Api.CreateSemanticDomain(new SemanticDomain
        {
            Id = id,
            Name = new MultiString { { "en", "Animals" } },
            Abbreviation = new MultiString { { "en", "1.6" } },
            Description = new RichMultiString { { "en", new RichString("Living creatures") } },
            OcmCodes = "22; 22.1",
            LouwNidaCodes = "4.1; 4.2",
        });

        created.Abbreviation.Values["en"].Should().Be("1.6");
        created.Code.Should().Be("1.6");
        created.Description["en"].GetPlainText().Should().Be("Living creatures");
        created.OcmCodes.Should().Be("22; 22.1");
        created.LouwNidaCodes.Should().Be("4.1; 4.2");

        var fetched = await Api.GetSemanticDomain(id);
        fetched.Should().NotBeNull();
        fetched!.Abbreviation.Values["en"].Should().Be("1.6");
        fetched.Code.Should().Be("1.6");
        fetched.Description["en"].GetPlainText().Should().Be("Living creatures");
        fetched.OcmCodes.Should().Be("22; 22.1");
        fetched.LouwNidaCodes.Should().Be("4.1; 4.2");
    }

    [Fact]
    public async Task UpdateSemanticDomain_UpdatesExtendedFields()
    {
        var id = Guid.NewGuid();
        var before = await Api.CreateSemanticDomain(new SemanticDomain
        {
            Id = id,
            Name = new MultiString { { "en", "Food" } },
            Abbreviation = new MultiString { { "en", "5.2" } },
        });

        var after = before.Copy();
        after.Abbreviation = new MultiString { { "en", "5.2.1" } };
        after.Code = string.Empty; // prefer Abbreviation when Code is cleared
        after.Description = new RichMultiString { { "en", new RichString("Things people eat") } };
        after.OcmCodes = "25";
        after.LouwNidaCodes = "5";

        var updated = await Api.UpdateSemanticDomain(before, after);
        updated.Abbreviation.Values["en"].Should().Be("5.2.1");
        updated.Code.Should().Be("5.2.1");
        updated.Description["en"].GetPlainText().Should().Be("Things people eat");
        updated.OcmCodes.Should().Be("25");
        updated.LouwNidaCodes.Should().Be("5");
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
