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

    [Fact]
    public async Task ComplexForms_SortByDefaultVernacularCollation()
    {
        WritingSystemId wsId = "de";
        await CreateWritingSystemWithCollation(NewWs(wsId) with { IcuCollationRules = "&z < a" });
        // the default vernacular writing system is the first one
        var vernacular = (await Api.GetWritingSystems()).Vernacular.First();
        await Api.MoveWritingSystem(wsId, WritingSystemType.Vernacular, new(null, vernacular.WsId));

        var component = await Api.CreateEntry(new() { LexemeForm = { { wsId, "base" } } });
        var apple = await Api.CreateEntry(new() { LexemeForm = { { wsId, "apple" } } });
        var zebra = await Api.CreateEntry(new() { LexemeForm = { { wsId, "zebra" } } });
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(apple, component));
        await Api.CreateComplexFormComponent(ComplexFormComponent.FromEntries(zebra, component));

        var entry = await Api.GetEntry(component.Id);

        entry!.ComplexForms.Select(c => c.ComplexFormHeadword).Should().Equal("zebra", "apple");
    }
}
