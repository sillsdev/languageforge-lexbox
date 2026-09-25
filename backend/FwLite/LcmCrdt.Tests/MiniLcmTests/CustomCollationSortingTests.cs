namespace LcmCrdt.Tests.MiniLcmTests;

public class CustomCollationSortingTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    [Fact]
    public async Task HeadwordSort_UsesIcuCollationRules()
    {
        const string wsId = "en-x-icu-test";
        await fixture.Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = wsId,
            Name = "Custom ICU",
            Abbreviation = "Ci",
            Font = "Arial",
            IcuCollationRules = "&z < a",
        });

        var apple = await fixture.Api.CreateEntry(new() { LexemeForm = { { wsId, "apple" } } });
        var zebra = await fixture.Api.CreateEntry(new() { LexemeForm = { { wsId, "zebra" } } });
        var ids = new[] { apple.Id, zebra.Id }.ToHashSet();

        var headwords = await fixture.Api
            .GetEntries(new QueryOptions(new SortOptions(SortField.Headword, wsId)))
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Headword())
            .ToArrayAsync();

        headwords.Should().Equal("zebra", "apple");
    }

    [Fact]
    public async Task HeadwordSort_UsesSystemCollationLocale()
    {
        const string wsId = "cs";
        await fixture.Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = wsId,
            Name = "Czech",
            Abbreviation = "Cs",
            Font = "Arial",
            SystemCollationLocale = "cs",
        });

        // English sorts c before h; Czech treats "ch" as a letter after h.
        var cha = await fixture.Api.CreateEntry(new() { LexemeForm = { { wsId, "cha" } } });
        var ha = await fixture.Api.CreateEntry(new() { LexemeForm = { { wsId, "ha" } } });
        var ids = new[] { cha.Id, ha.Id }.ToHashSet();

        var headwords = await fixture.Api
            .GetEntries(new QueryOptions(new SortOptions(SortField.Headword, wsId)))
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Headword())
            .ToArrayAsync();

        headwords.Should().Equal("ha", "cha");
    }
}
