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
    /// Validates that HeadwordSearchValue translates to SQL correctly when
    /// leading/trailing come from real column references (via a JOIN to
    /// the WritingSystem table), not string literals.
    /// This simulates the future MorphTypeData JOIN pattern — both have
    /// plain string columns that get inlined as column references in SQL.
    /// </summary>
    [Theory]
    [InlineData("-", "", "-ing", true)]  // suffix: leading="-", query="-ing" matches lexeme "ing"
    [InlineData("", "-", "ing-", true)]  // prefix: trailing="-", query="ing-" matches lexeme "ing"
    [InlineData("-", "", "ing", true)]   // query without token still matches raw lexeme
    [InlineData("-", "", "-xyz", false)] // "-xyz" should not match lexeme "ing"
    [InlineData("", "", "ing", true)]    // no tokens, matches raw lexeme
    [InlineData("-", "", "-fra", true)]  // matches French lexeme "fra" with token
    public async Task HeadwordSearchValue_WithColumnReferenceTokens_MatchesCorrectly(
        string leading, string trailing, string query, bool shouldMatch)
    {
        var targetId = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry
        {
            Id = targetId,
            LexemeForm = { ["en"] = "ing", ["fr"] = "fra" },
            MorphType = MorphType.Suffix
        });
        await _context.SaveChangesAsync();

        // Use a WritingSystem row to provide leading/trailing as real DB columns.
        // We repurpose Name=leading, Font=trailing (both are plain string columns).
        var tokenWs = new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = "de",
            Name = leading,
            Abbreviation = "de",
            Font = trailing,
            Type = WritingSystemType.Analysis
        };
        await fixture.Api.CreateWritingSystem(tokenWs);

        var entries = _context.GetTable<Entry>();
        var writingSystems = _context.GetTable<WritingSystem>();

        // JOIN WritingSystem to get token values from column references
        var results = await (
            from entry in entries
            from ws in writingSystems.InnerJoin(ws => ws.WsId == new WritingSystemId("de"))
            where entry.Id == targetId
                && entry.HeadwordSearchValue(ws.Name, ws.Font, query)
            select entry
        ).ToListAsyncLinqToDB();

        if (shouldMatch)
            results.Should().ContainSingle(e => e.Id == targetId);
        else
            results.Should().BeEmpty();
    }

    /// <summary>
    /// CitationForm takes priority — if CitationForm matches, it should match
    /// even when morph tokens don't apply to it.
    /// Token values come from a JOIN, not constants.
    /// </summary>
    [Fact]
    public async Task HeadwordSearchValue_CitationFormMatchesWithoutTokens()
    {
        var targetId = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry
        {
            Id = targetId,
            CitationForm = { ["en"] = "running" },
            LexemeForm = { ["en"] = "run" },
            MorphType = MorphType.Stem
        });
        await _context.SaveChangesAsync();

        var tokenWs = new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = "pt",
            Name = "-",       // leading
            Abbreviation = "pt",
            Font = "",        // trailing
            Type = WritingSystemType.Analysis
        };
        await fixture.Api.CreateWritingSystem(tokenWs);

        var entries = _context.GetTable<Entry>();
        var writingSystems = _context.GetTable<WritingSystem>();

        var results = await (
            from entry in entries
            from ws in writingSystems.InnerJoin(ws => ws.WsId == new WritingSystemId("pt"))
            where entry.Id == targetId
                && entry.HeadwordSearchValue(ws.Name, ws.Font, "running")
            select entry
        ).ToListAsyncLinqToDB();

        results.Should().ContainSingle(e => e.Id == targetId);
    }

    /// <summary>
    /// Tests the scenario from code review: main WS is "es" but the match
    /// should come from "en" headword with morph tokens.
    /// Token values come from a real JOIN, not constants.
    /// </summary>
    [Fact]
    public async Task HeadwordSearchValue_MatchesNonPrimaryWs_ViaColumnReference()
    {
        var targetId = Guid.NewGuid();
        _context.Set<Entry>().Add(new Entry
        {
            Id = targetId,
            LexemeForm = { ["en"] = "ing", ["es"] = "abc" },
            MorphType = MorphType.Suffix
        });
        await _context.SaveChangesAsync();

        var tokenWs = new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = "ja",
            Name = "-",       // leading
            Abbreviation = "ja",
            Font = "",        // trailing
            Type = WritingSystemType.Analysis
        };
        await fixture.Api.CreateWritingSystem(tokenWs);

        var entries = _context.GetTable<Entry>();
        var writingSystems = _context.GetTable<WritingSystem>();

        var results = await (
            from entry in entries
            from ws in writingSystems.InnerJoin(ws => ws.WsId == new WritingSystemId("ja"))
            where entry.Id == targetId
                && entry.HeadwordSearchValue(ws.Name, ws.Font, "-ing")
            select entry
        ).ToListAsyncLinqToDB();

        results.Should().ContainSingle(e => e.Id == targetId);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await fixture.DisposeAsync();
    }
}
