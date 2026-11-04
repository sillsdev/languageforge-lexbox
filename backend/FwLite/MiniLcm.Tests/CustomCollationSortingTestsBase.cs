namespace MiniLcm.Tests;

public abstract class CustomCollationSortingTestsBase : MiniLcmTestBase
{
    /// <summary>
    /// Creates a writing system with the collation set on it. FwData only imports collation from FLEx, so its
    /// implementation sets the collation on the LCM writing system directly.
    /// </summary>
    protected virtual async Task CreateWritingSystemWithCollation(WritingSystem writingSystem)
    {
        await Api.CreateWritingSystem(writingSystem);
    }

    private static WritingSystem NewWs(WritingSystemId wsId) => new()
    {
        Id = Guid.NewGuid(),
        Type = WritingSystemType.Vernacular,
        WsId = wsId,
        Name = "Collation test",
        Abbreviation = "Ct",
        Font = "Arial",
    };

    private async Task<string[]> SortedHeadwords(WritingSystemId wsId, params string[] lexemeForms)
    {
        var ids = new HashSet<Guid>();
        foreach (var lexemeForm in lexemeForms)
        {
            var entry = await Api.CreateEntry(new() { LexemeForm = { { wsId, lexemeForm } } });
            ids.Add(entry.Id);
        }

        return await Api
            .GetEntries(new QueryOptions(new SortOptions(SortField.Headword, wsId)))
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.LexemeForm[wsId])
            .ToArrayAsync();
    }

    [Fact]
    public async Task HeadwordSort_UsesIcuCollationRules()
    {
        WritingSystemId wsId = "en-x-icu-test";
        await CreateWritingSystemWithCollation(NewWs(wsId) with { IcuCollationRules = "&z < a" });

        var headwords = await SortedHeadwords(wsId, "apple", "zebra");

        headwords.Should().Equal("zebra", "apple");
    }

    [Fact]
    public async Task HeadwordSort_UsesSystemCollationLocale()
    {
        WritingSystemId wsId = "cs";
        await CreateWritingSystemWithCollation(NewWs(wsId) with { SystemCollationLocale = "cs" });

        // English sorts c before h; Czech treats "ch" as a letter after h.
        var headwords = await SortedHeadwords(wsId, "cha", "ha");

        headwords.Should().Equal("ha", "cha");
    }
}
