using LcmCrdt.FullTextSearch;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace LcmCrdt.Tests.FullTextSearch;

public class EntrySearchServiceTests : IAsyncLifetime
{
    private MiniLcmApiFixture fixture = new MiniLcmApiFixture();

    private static readonly AutoFaker Faker = new(AutoFakerDefault.Config);

    private Entry _entry = Faker.Generate<Entry>();
    private EntrySearchService _service = null!;
    private LcmCrdtDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await fixture.InitializeAsync();
        await fixture.Api.CreateWritingSystem(new WritingSystem()
        {
            Id = Guid.NewGuid(),
            WsId = "fr",
            Name = "French",
            Abbreviation = "fr",
            Font = "Arial",
            Exemplars = ["a", "b"],
            Type = WritingSystemType.Vernacular
        });
        _context = fixture.GetService<LcmCrdtDbContext>();
        _service = fixture.GetService<EntrySearchService>();
    }

    [Fact]
    public async Task CanUpdateAnEntrySearchRecord()
    {
        await _service.UpdateEntrySearchTable(_entry);
        var result = await _service.EntrySearchRecords.AsAsyncEnumerable().ToArrayAsync();
        result.Should().Contain(e => e.Id == _entry.Id);
    }

    [Fact]
    public async Task CanRegenerateTheSearchTable()
    {
        var id = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry()
        {
            Id = id,
            LexemeForm = {["en"] = "word1"},
        });
        await _context.SaveChangesAsync();
        _service.EntrySearchRecords.Should().NotContain(e => e.Id == id);
        await _service.RegenerateEntrySearchTable();
        _service.EntrySearchRecords.Should().Contain(e => e.Id == id);
    }


    [Theory]
    [InlineData("word1", "word1", true)]
    [InlineData("ทำอาหาร", "ทำอาหาร", true)]
    [InlineData("ทำอา", "ทำอาหาร", true)]
    public async Task MatchWorksAsExpected(string searchTerm, string word, bool matches)
    {
        var id = Guid.NewGuid();
        await _service.UpdateEntrySearchTable(new Entry() { Id = id, LexemeForm = { { "en", word } } });


        var result = await _service.Search(searchTerm).ToArrayAsync();
        if (matches)
        {
            result.Should().Contain(e => e.Id == id);
        }
        else
        {
            result.Should().NotContain(e => e.Id == id);
        }
    }

    [Theory]
    [InlineData("lexemeform_en", true)]
    [InlineData("lexemeform_fr", true)]
    [InlineData("citation_fr", true)]
    [InlineData("CitationForm: citation", true)]
    [InlineData("lexemeform_de", false)]
    [InlineData("gloss", true)]
    [InlineData("definition", true)]
    [InlineData("Gloss: definition", false)]
    [InlineData("Definition: def", true)]
    [InlineData("LexemeForm: cit OR CitationForm: cit", true)]
    public async Task MatchColumnWorksAsExpected(string searchTerm, bool matches)
    {
        var id = Guid.NewGuid();
        await _service.UpdateEntrySearchTable(new Entry()
        {
            Id = id,
            LexemeForm = { { "en", "lexemeform_en" }, { "fr", "lexemeform_fr" } },
            CitationForm = { { "fr", "citation_fr" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "gloss" } }, Definition = { { "en", new RichString("definition", "en") } } }
            ]
        });


        var result = await _service.Search(searchTerm).ToArrayAsync();
        if (matches)
        {
            result.Should().Contain(e => e.Id == id);
        }
        else
        {
            result.Should().NotContain(e => e.Id == id);
        }
    }

    [Theory]
    [InlineData("word1", "word1", "word1")]
    [InlineData("app", "app,apple,banana", "app,apple")]
    [InlineData("apple", "app,apple,banana", "apple")]
    [InlineData("att", "att,attack,battery", "att,attack,battery")]
    [InlineData("att*", "att,attack,battery", "att,attack,battery")]
    [InlineData("att NOT attack", "att,attack,battery", "att,battery")]
    [InlineData("battery OR attack", "att,attack,battery", "attack,battery")]
    [InlineData("word1 word3", "word1 word2 word3,word4", "word1 word2 word3")]
    public async Task RanksResultsAsExpected(string searchTerm, string words, string expectedOrder)
    {
        var ids = new HashSet<Guid>();
        foreach (var word in words.Split(","))
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            await _service.UpdateEntrySearchTable(new Entry() { Id = id, LexemeForm = { { "en", word } } });
        }

        var result = await _service.Search(searchTerm)
            .Where(e => ids.Contains(e.Id))//only include the entries we added to the search table, there may be others from other tests.
            .ToArrayAsync();
        string.Join(",", result.Select(e => e.LexemeForm)).Should().Be(expectedOrder);
    }

    public async Task DisposeAsync()
    {
        await fixture.DisposeAsync();
    }
}
