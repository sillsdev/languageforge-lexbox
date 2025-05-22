using LcmCrdt.FullTextSearch;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace LcmCrdt.Tests.FullTextSearch;

public class EntrySearchServiceTests : IAsyncLifetime
{
    private MiniLcmApiFixture fixture = new MiniLcmApiFixture();

    private static readonly AutoFaker Faker = new(new AutoFakerConfig()
    {
        RepeatCount = 5,
        Overrides =
        [
            new MultiStringOverride(),
            new RichMultiStringOverride(),
            new WritingSystemIdOverride(),
            new OrderableOverride(),
        ]
    });

    private Entry _entry = Faker.Generate<Entry>();
    private EntrySearchService Service => fixture.GetService<EntrySearchService>();

    public async Task InitializeAsync()
    {
        await fixture.InitializeAsync();
    }

    [Fact]
    public async Task CanUpdateAnEntrySearchRecord()
    {
        await Service.UpdateEntrySearchTable(_entry);
        var result = await fixture.GetService<LcmCrdtDbContext>().EntrySearchRecords.AsAsyncEnumerable().ToArrayAsync();
        result.Should().Contain(e => e.Id == _entry.Id);
    }


    [Theory]
    [InlineData("word1", "word1", true)]
    [InlineData("ทำอาหาร", "ทำอาหาร", true)]
    [InlineData("ทำอา", "ทำอาหาร", true)]
    public async Task MatchWorksAsExpected(string searchTerm, string word, bool matches)
    {
        var id = Guid.NewGuid();
        await Service.UpdateEntrySearchTable(new Entry() { Id = id, LexemeForm = { { "en", word } } });


        var result = await Service.Search(searchTerm).ToArrayAsync();
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
    public async Task RanksResultsAsExpected(string searchTerm, string words, string expectedOrder)
    {
        foreach (var word in words.Split(","))
        {
            await Service.UpdateEntrySearchTable(new Entry() { LexemeForm = { { "en", word } } });
        }

        var result = await Service.Search(searchTerm).ToArrayAsync();
        string.Join(",", result.Select(e => e.Headword)).Should().Be(expectedOrder);
    }

    public async Task DisposeAsync()
    {
        await fixture.DisposeAsync();
    }
}
