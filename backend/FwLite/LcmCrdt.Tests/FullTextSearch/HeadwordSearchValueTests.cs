using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LcmCrdt.Tests.FullTextSearch;

public class HeadwordSearchValueTests : IAsyncLifetime
{
    private MiniLcmApiFixture fixture = new();
    private LcmCrdtDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await fixture.InitializeAsync();
        _context = await fixture.GetService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
    }

    /// <summary>
    /// Validates that HeadwordSearchValue translates to SQL correctly,
    /// specifically that string concatenation with json_each values works.
    /// </summary>
    [Theory]
    [InlineData("-", "", "-ing", true)] // suffix: leading="-", query="-ing" matches lexeme "ing"
    [InlineData("", "-", "ing-", true)] // prefix: trailing="-", query="ing-" matches lexeme "ing"
    [InlineData("-", "", "ing", true)]  // query without token still matches raw lexeme
    [InlineData("-", "", "-xyz", false)] // "-xyz" should not match lexeme "ing"
    [InlineData("", "", "ing", true)]   // no tokens, matches raw lexeme
    [InlineData("-", "", "-fra", true)] // matches French lexeme "fra" with token
    public async Task HeadwordSearchValue_WithTokens_MatchesCorrectly(
        string leading, string trailing, string query, bool shouldMatch)
    {
        var id = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry
        {
            Id = id,
            LexemeForm = { ["en"] = "ing", ["fr"] = "fra" },
            MorphType = MorphType.Suffix
        });
        await _context.SaveChangesAsync();

        // Run the query using HeadwordSearchValue — this exercises the SQL translation
        var results = await _context.GetTable<Entry>()
            .Where(e => e.Id == id && e.HeadwordSearchValue(leading, trailing, query))
            .ToListAsyncLinqToDB();

        if (shouldMatch)
            results.Should().ContainSingle(e => e.Id == id);
        else
            results.Should().BeEmpty();
    }

    /// <summary>
    /// CitationForm takes priority — if CitationForm matches, it should match
    /// even without morph tokens.
    /// </summary>
    [Fact]
    public async Task HeadwordSearchValue_CitationFormMatchesWithoutTokens()
    {
        var id = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry
        {
            Id = id,
            CitationForm = { ["en"] = "running" },
            LexemeForm = { ["en"] = "run" },
            MorphType = MorphType.Stem
        });
        await _context.SaveChangesAsync();

        var results = await _context.GetTable<Entry>()
            .Where(e => e.Id == id && e.HeadwordSearchValue("-", "", "running"))
            .ToListAsyncLinqToDB();

        results.Should().ContainSingle(e => e.Id == id);
    }

    /// <summary>
    /// Tests the scenario from code review: main WS is "es" but the match
    /// should come from "en" headword with morph tokens.
    /// </summary>
    [Fact]
    public async Task HeadwordSearchValue_MatchesNonPrimaryWs()
    {
        var id = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry
        {
            Id = id,
            LexemeForm = { ["en"] = "ing", ["es"] = "abc" },
            MorphType = MorphType.Suffix
        });
        await _context.SaveChangesAsync();

        // "-ing" should match via English headword even if primary WS is Spanish
        var results = await _context.GetTable<Entry>()
            .Where(e => e.Id == id && e.HeadwordSearchValue("-", "", "-ing"))
            .ToListAsyncLinqToDB();

        results.Should().ContainSingle(e => e.Id == id);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await fixture.DisposeAsync();
    }
}
