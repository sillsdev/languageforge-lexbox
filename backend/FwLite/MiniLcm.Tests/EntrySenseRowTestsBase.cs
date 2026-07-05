namespace MiniLcm.Tests;

public abstract class EntrySenseRowTestsBase : MiniLcmTestBase
{
    private static readonly SortOptions GlossAsc = new(SortField.Gloss, SortOptions.DefaultWritingSystem);
    private static readonly SortOptions GlossDesc = new(SortField.Gloss, SortOptions.DefaultWritingSystem, Ascending: false);

    private Entry _apple = null!;
    private Entry _banana = null!;
    private Entry _cherry = null!;
    private Entry _date = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _apple = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "apple" } },
            Senses = [new Sense { Gloss = { { "en", "fruit" } } }]
        });
        _banana = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "banana" } },
            Senses =
            [
                new Sense { Gloss = { { "en", "yellow" } } },
                new Sense { Gloss = { { "en", "fruit" } } }
            ]
        });
        // a sense without any gloss
        _cherry = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "cherry" } },
            Senses = [new Sense()]
        });
        // an entry without any senses
        _date = await Api.CreateEntry(new() { LexemeForm = { { "en", "date" } } });
    }

    private static Guid SenseId(Entry entry, string gloss)
    {
        return entry.Senses.First(s => s.Gloss["en"] == gloss).Id;
    }

    private async Task<(string Headword, Guid? SenseId)[]> GetRows(QueryOptions? options = null, string? query = null)
    {
        var rows = await Api.GetEntrySenseRows(query, options ?? new QueryOptions(GlossAsc)).ToArrayAsync();
        return rows.Select(r => (r.Entry.Headword(), r.SenseId)).ToArray();
    }

    private (string Headword, Guid? SenseId)[] ExpectedAscending() =>
    [
        ("apple", SenseId(_apple, "fruit")),
        ("banana", SenseId(_banana, "fruit")),
        ("banana", SenseId(_banana, "yellow")),
        // rows without a gloss come last, sorted by headword
        ("cherry", _cherry.Senses[0].Id),
        ("date", null),
    ];

    [Fact]
    public async Task Rows_SortedByGloss_OneRowPerSense()
    {
        var rows = await GetRows();
        rows.Should().Equal(ExpectedAscending());
    }

    [Fact]
    public async Task Rows_SortedByGloss_Descending_KeepsGlosslessRowsLast()
    {
        var rows = await GetRows(new QueryOptions(GlossDesc));
        rows.Should().Equal([
            ("banana", SenseId(_banana, "yellow")),
            // equal glosses keep ascending headword order
            ("apple", SenseId(_apple, "fruit")),
            ("banana", SenseId(_banana, "fruit")),
            ("cherry", _cherry.Senses[0].Id),
            ("date", null),
        ]);
    }

    [Fact]
    public async Task Rows_DefaultOptions_SortByGloss()
    {
        var rows = await Api.GetEntrySenseRows().ToArrayAsync();
        rows.Select(r => (r.Entry.Headword(), r.SenseId)).Should().Equal(ExpectedAscending());
    }

    [Fact]
    public async Task Rows_EntriesAreFullyHydrated()
    {
        var rows = await GetRows();
        var hydratedRows = await Api.GetEntrySenseRows(options: new QueryOptions(GlossAsc)).ToArrayAsync();
        var bananaRows = hydratedRows.Where(r => r.Entry.Id == _banana.Id).ToArray();
        bananaRows.Should().HaveCount(2);
        foreach (var row in bananaRows)
        {
            row.Entry.Senses.Should().HaveCount(2);
        }
        rows.Should().HaveCount(hydratedRows.Length);
    }

    [Fact]
    public async Task Rows_Paging_ReturnsTheMatchingSlice()
    {
        var all = await GetRows();
        var page = await GetRows(new QueryOptions(GlossAsc, Count: 2, Offset: 1));
        page.Should().Equal(all[1..3]);
    }

    [Fact]
    public async Task Rows_GridifyFilter_Applies()
    {
        var options = new QueryOptions(GlossAsc, Filter: new() { GridifyFilter = "LexemeForm[en]^ban" });
        var rows = await GetRows(options);
        rows.Should().Equal([
            ("banana", SenseId(_banana, "fruit")),
            ("banana", SenseId(_banana, "yellow")),
        ]);
    }

    [Theory]
    [InlineData("fruit")] // FTS in CRDT
    [InlineData("fr")] // non-FTS
    public async Task Rows_SearchQuery_ReturnsAllSensesOfMatchingEntries(string query)
    {
        // "fruit" only matches a gloss of one banana sense, but rows are produced
        // for every sense of each matching entry, so banana's "yellow" row is included too
        var rows = await GetRows(query: query);
        rows.Should().Equal([
            ("apple", SenseId(_apple, "fruit")),
            ("banana", SenseId(_banana, "fruit")),
            ("banana", SenseId(_banana, "yellow")),
        ]);
    }

    [Fact]
    public async Task CountEntrySenseRows_MatchesRowCount()
    {
        var count = await Api.CountEntrySenseRows();
        count.Should().Be((await GetRows()).Length).And.Be(5);
    }

    [Fact]
    public async Task CountEntrySenseRows_MatchesRowCount_WithFilterAndQuery()
    {
        var filter = new FilterQueryOptions(Filter: new() { GridifyFilter = "LexemeForm[en]^ban" });
        (await Api.CountEntrySenseRows(options: filter)).Should().Be(2);
        (await Api.CountEntrySenseRows("fruit")).Should().Be(3);
    }

    [Fact]
    public async Task GetEntrySenseRowIndex_ReturnsFirstRowOfEntry()
    {
        var options = new IndexQueryOptions(GlossAsc);
        (await Api.GetEntrySenseRowIndex(_apple.Id, options: options)).Should().Be(0);
        (await Api.GetEntrySenseRowIndex(_banana.Id, options: options)).Should().Be(1);
        (await Api.GetEntrySenseRowIndex(_cherry.Id, options: options)).Should().Be(3);
        (await Api.GetEntrySenseRowIndex(_date.Id, options: options)).Should().Be(4);
        (await Api.GetEntrySenseRowIndex(Guid.NewGuid(), options: options)).Should().Be(-1);

        (await Api.GetEntrySenseRowIndex(_banana.Id, options: new IndexQueryOptions(GlossDesc))).Should().Be(0);
    }

    [Fact]
    public async Task GetEntrySenseRowIndex_RespectsFilterAndQuery()
    {
        var filtered = new IndexQueryOptions(GlossAsc, Filter: new() { GridifyFilter = "LexemeForm[en]^ban" });
        (await Api.GetEntrySenseRowIndex(_banana.Id, options: filtered)).Should().Be(0);
        (await Api.GetEntrySenseRowIndex(_apple.Id, options: filtered)).Should().Be(-1);

        // "fruit" matches apple and banana; apple's single row comes first
        (await Api.GetEntrySenseRowIndex(_banana.Id, "fruit", new IndexQueryOptions(GlossAsc))).Should().Be(1);
    }

    [Fact]
    public async Task Rows_SortByRequestedAnalysisWritingSystem()
    {
        await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Analysis,
            WsId = "fr",
            Name = "French",
            Abbreviation = "fr",
            Font = "Arial"
        });
        var elephant = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "elephant" } },
            Senses = [new Sense { Gloss = { { "en", "zebra" }, { "fr", "aardvark" } } }]
        });
        var fox = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "fox" } },
            Senses = [new Sense { Gloss = { { "en", "aardvark" }, { "fr", "zebra" } } }]
        });
        var ids = new[] { elephant.Id, fox.Id }.ToHashSet();

        async Task<string[]> HeadwordsSortedBy(string ws)
        {
            var rows = await Api.GetEntrySenseRows(options: new QueryOptions(new SortOptions(SortField.Gloss, ws)))
                .ToArrayAsync();
            return rows.Where(r => ids.Contains(r.Entry.Id)).Select(r => r.Entry.Headword()).ToArray();
        }

        (await HeadwordsSortedBy("fr")).Should().Equal("elephant", "fox");
        (await HeadwordsSortedBy("en")).Should().Equal("fox", "elephant");
    }

    [Fact]
    public async Task GetEntries_GlossSort_Throws()
    {
        var act = async () => await Api.GetEntries(new QueryOptions(new SortOptions(SortField.Gloss))).ToArrayAsync();
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetEntrySenseRows_NonGlossSort_Throws()
    {
        var act = async () => await Api
            .GetEntrySenseRows(options: new QueryOptions(new SortOptions(SortField.Headword)))
            .ToArrayAsync();
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
