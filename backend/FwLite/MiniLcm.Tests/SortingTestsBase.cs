using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;

namespace MiniLcm.Tests;

public abstract class SortingTestsBase : MiniLcmTestBase
{
    protected static readonly AutoFaker Faker = new(AutoFakerDefault.Config);

    private Task CreateEntry(string headword)
    {
        return Api.CreateEntry(new() { LexemeForm = { { "en", headword } }, });
    }

    // ReSharper disable InconsistentNaming
    private const string Ru_A = "\u0410";
    private const string Ru_a = "\u0430";
    private const string Ru_Б = "\u0411";
    private const string Ru_б = "\u0431";
    private const string Ru_В = "\u0412";
    private const string Ru_в = "\u0432";
    // ReSharper restore InconsistentNaming

    [Theory]
    [InlineData("aa,ab,ac")]
    [InlineData("aa,Ab,ac")]
    [InlineData($"{Ru_a}{Ru_a},{Ru_a}{Ru_б},{Ru_a}{Ru_в}")]
    [InlineData($"{Ru_a}{Ru_a},{Ru_A}{Ru_б},{Ru_a}{Ru_в}")]
    public async Task EntriesAreSorted(string headwords)
    {
        var headwordList = headwords.Split(',');
        foreach (var headword in headwordList.OrderBy(h => Random.Shared.Next()))
        {
            await CreateEntry(headword);
        }
        var entries = await Api.GetEntries().Select(e => e.HeadwordText()).ToArrayAsync();
        entries.Should().Equal(headwordList);
    }

    [Theory]
    [InlineData("lwl-Zxxx-x-minority2-audio")]
    //note this test does not ensure the sorting works, just that it doesn't crash when creating or querying the data
    public async Task CanUseValidWritingSystems(string wsId)
    {
        await Api.CreateWritingSystem(new()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = wsId,
            Name = "custom",
            Abbreviation = "Cs",
            Font = "Arial"
        });
        await Api.GetEntries(new QueryOptions(new SortOptions(SortField.Headword, wsId)))
            .ToArrayAsync();
    }

    [Theory]
    [InlineData("aaaa", SortField.Headword)] // FTS rank
    [InlineData("a", SortField.Headword)] // non-FTS rank
    [InlineData("aaaa", SortField.SearchRelevance)] // FTS rank
    [InlineData("a", SortField.SearchRelevance)] // non-FTS rank
    public async Task MorphTokens_DoNotAffectSortOrder(string query, SortField sortField)
    {
        MorphType[] morphTypes = [
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Root, Name = { ["en"] = "Root" }, SecondaryOrder = 1 },
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Prefix, Name = { ["en"] = "Prefix" }, Prefix = "-", SecondaryOrder = 3 },
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Suffix, Name = { ["en"] = "Suffix" }, Postfix = "-", SecondaryOrder = 6 },
        ];

        foreach (var morphType in morphTypes)
            await Api.CreateMorphType(morphType);

        // All three entries have LexemeForm "aaaa". Their headwords are:
        //   Root:   "aaaa"   (no tokens)
        //   Prefix: "-aaaa"  (leading token "-")
        //   Suffix: "aaaa-"  (trailing token "-")
        // Sort order should ignore morph tokens and differentiate only by SecondaryOrder.
        Entry[] expected = [
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "aaaa" }, MorphType = MorphTypeKind.Root }, // SecondaryOrder = 1
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "aaaa" }, MorphType = MorphTypeKind.Prefix }, // SecondaryOrder = 3
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "aaaa" }, MorphType = MorphTypeKind.Suffix }, // SecondaryOrder = 6
        ];

        var ids = expected.Select(e => e.Id).ToHashSet();

        foreach (var entry in Faker.Faker.Random.Shuffle(expected))
            await Api.CreateEntry(entry);

        var results = (await Api.SearchEntries(query, new(new(sortField))).ToArrayAsync())
            .Where(e => ids.Contains(e.Id))
            .ToList();

        results.Should().BeEquivalentTo(expected,
            options => options.Excluding(e => e.Headword).WithStrictOrdering());
    }

    [Theory]
    [InlineData("aaaa")] // FTS rank
    [InlineData("a")] // non-FTS rank
    public async Task SecondaryOrder_Relevance_LexemeForm(string searchTerm)
    {
        MorphType[] morphTypes = [
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Root, Name = { ["en"] = "Root" }, SecondaryOrder = 1 },
            // new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Stem, Name = { ["en"] = "Stem" }, SecondaryOrder = 1 },
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.BoundRoot, Name = { ["en"] = "BoundRoot" }, SecondaryOrder = 2 },
            // new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.BoundStem, Name = { ["en"] = "BoundStem" }, SecondaryOrder = 2 },
        ];

        foreach (var morphType in morphTypes)
            await Api.CreateMorphType(morphType);

        static Entry[] CreateSortedEntrySet(string headword)
        {
            return [
                // Root/Stem - SecondaryOrder: 1
                new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = headword }, MorphType = MorphTypeKind.Root/*, HomographNumber = 1*/ },
                // new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = lexeme }, MorphType = MorphTypeKind.Stem, HomographNumber = 2 },
                // BoundRoot/BoundStem - SecondaryOrder: 2
                new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = headword }, MorphType = MorphTypeKind.BoundRoot/*, HomographNumber = 1*/ },
                // new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = lexeme }, MorphType = MorphTypeKind.BoundStem, HomographNumber = 2 },
            ];
        }

        var exactMatches = CreateSortedEntrySet("aaaa");
        var firstShortestStartsWithMatches = CreateSortedEntrySet("aaaab");
        var lastShortestStartsWithMatches = CreateSortedEntrySet("aaaac");
        var firstLongestStartsWithMatches = CreateSortedEntrySet("aaaabb");
        var lastLongestStartsWithMatches = CreateSortedEntrySet("aaaacc");
        var firstShortestContainsMatches = CreateSortedEntrySet("baaaa");
        var lastShortestContainsMatches = CreateSortedEntrySet("caaaa");
        var firstLongestContainsMatches = CreateSortedEntrySet("bbaaaa");
        var lastLongestContainsMatches = CreateSortedEntrySet("ccaaaa");

        var entryId = Guid.NewGuid();
        Entry nonHeadwordMatch = new() { Id = entryId, Senses = [new() { EntryId = entryId, Gloss = { ["en"] = "aaaa" } }] };

        Entry[] expected = [
            .. exactMatches,
            .. firstShortestStartsWithMatches,
            .. lastShortestStartsWithMatches,
            .. firstLongestStartsWithMatches,
            .. lastLongestStartsWithMatches,
            .. firstShortestContainsMatches,
            .. lastShortestContainsMatches,
            .. firstLongestContainsMatches,
            .. lastLongestContainsMatches,
            nonHeadwordMatch,
        ];

        var ids = expected.Select(e => e.Id).ToHashSet();

        foreach (var entry in Faker.Faker.Random.Shuffle(expected))
            await Api.CreateEntry(entry);

        var results = (await Api.SearchEntries(searchTerm, new(new(SortField.SearchRelevance))).ToArrayAsync())
            .Where(e => ids.Contains(e.Id))
            .ToList();

        results.Should().BeEquivalentTo(expected,
            options => options.Excluding(e => e.Headword));
        results.Should().BeEquivalentTo(expected,
            options => options.Excluding(e => e.Headword).WithStrictOrdering());
    }

    [Theory]
    [InlineData("aaaa")] // FTS rank
    [InlineData("a")] // non-FTS rank
    public async Task SecondaryOrder_Relevance_CitationForm(string searchTerm)
    {
        MorphType[] morphTypes = [
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Root, Name = { ["en"] = "Root" }, SecondaryOrder = 1 },
            // new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Stem, Name = { ["en"] = "Stem" }, SecondaryOrder = 1 },
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.BoundRoot, Name = { ["en"] = "BoundRoot" }, SecondaryOrder = 2 },
            // new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.BoundStem, Name = { ["en"] = "BoundStem" }, SecondaryOrder = 2 },
        ];

        foreach (var morphType in morphTypes)
            await Api.CreateMorphType(morphType);

        static Entry[] CreateSortedEntrySet(string headword)
        {
            return [
                // Root/Stem - SecondaryOrder: 1
                new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = headword }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.Root/*, HomographNumber = 1*/ },
                // new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = headword }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.Stem, HomographNumber = 2 },
                // BoundRoot/BoundStem - SecondaryOrder: 2
                new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = headword }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.BoundRoot/*, HomographNumber = 1*/ },
                // new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = headword }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.BoundStem, HomographNumber = 2 },
            ];
        }

        var exactMatches = CreateSortedEntrySet("aaaa");
        var firstShortestStartsWithMatches = CreateSortedEntrySet("aaaab");
        var lastShortestStartsWithMatches = CreateSortedEntrySet("aaaac");
        var firstLongestStartsWithMatches = CreateSortedEntrySet("aaaabb");
        var lastLongestStartsWithMatches = CreateSortedEntrySet("aaaacc");
        var firstShortestContainsMatches = CreateSortedEntrySet("baaaa");
        var lastShortestContainsMatches = CreateSortedEntrySet("caaaa");
        var firstLongestContainsMatches = CreateSortedEntrySet("bbaaaa");
        var lastLongestContainsMatches = CreateSortedEntrySet("ccaaaa");

        var entryId = Guid.NewGuid();
        Entry nonHeadwordMatch = new() { Id = entryId, Senses = [new() { EntryId = entryId, Gloss = { ["en"] = "aaaa" } }] };

        Entry[] expected = [
            .. exactMatches,
            .. firstShortestStartsWithMatches,
            .. lastShortestStartsWithMatches,
            .. firstLongestStartsWithMatches,
            .. lastLongestStartsWithMatches,
            .. firstShortestContainsMatches,
            .. lastShortestContainsMatches,
            .. firstLongestContainsMatches,
            .. lastLongestContainsMatches,
            nonHeadwordMatch,
        ];

        var ids = expected.Select(e => e.Id).ToHashSet();

        foreach (var entry in Faker.Faker.Random.Shuffle(expected))
            await Api.CreateEntry(entry);

        var results = (await Api.SearchEntries(searchTerm, new(new(SortField.SearchRelevance))).ToArrayAsync())
            .Where(e => ids.Contains(e.Id))
            .ToList();

        results.Should().BeEquivalentTo(expected,
            options => options.Excluding(e => e.Headword));
        results.Should().BeEquivalentTo(expected,
            options => options.Excluding(e => e.Headword).WithStrictOrdering());
    }

    [Theory]
    [InlineData("baaa")] // FTS rank
    [InlineData("b")] // non-FTS rank
    public async Task SecondaryOrder_Headword_LexemeForm(string searchTerm)
    {
        MorphType[] morphTypes = [
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Root, Name = { ["en"] = "Root" }, SecondaryOrder = 1 },
            // new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Stem, Name = { ["en"] = "Stem" }, SecondaryOrder = 1 },
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.BoundRoot, Name = { ["en"] = "BoundRoot" }, SecondaryOrder = 2 },
            // new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.BoundStem, Name = { ["en"] = "BoundStem" }, SecondaryOrder = 2 },
        ];

        foreach (var morphType in morphTypes)
            await Api.CreateMorphType(morphType);

        Entry[] expected = [
            // Root/Stem - SecondaryOrder: 1
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "abaaa" }, MorphType = MorphTypeKind.Root/*, HomographNumber = 1*/ },
            // new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "abaaa" }, MorphType = MorphTypeKind.Stem, HomographNumber = 2 },
            // BoundRoot/BoundStem - SecondaryOrder: 2
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "abaaa" }, MorphType = MorphTypeKind.BoundRoot/*, HomographNumber = 1*/ },
            // new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "abaaa" }, MorphType = MorphTypeKind.BoundStem, HomographNumber = 2 },
            // Root/Stem - SecondaryOrder: 1
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "baaa" }, MorphType = MorphTypeKind.Root/*, HomographNumber = 1*/ },
            // new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "baaa" }, MorphType = MorphTypeKind.Stem, HomographNumber = 2 },
            // BoundRoot/BoundStem - SecondaryOrder: 2
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "baaa" }, MorphType = MorphTypeKind.BoundRoot/*, HomographNumber = 1*/ },
            // new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "baaa" }, MorphType = MorphTypeKind.BoundStem, HomographNumber = 2 },
        ];

        var ids = expected.Select(e => e.Id).ToHashSet();

        foreach (var entry in Faker.Faker.Random.Shuffle(expected))
            await Api.CreateEntry(entry);

        var results = (await Api.SearchEntries(searchTerm, new(new(SortField.Headword))).ToArrayAsync())
            .Where(e => ids.Contains(e.Id))
            .ToList();

        results.Should().BeEquivalentTo(expected,
            options => options.Excluding(e => e.Headword));
        results.Should().BeEquivalentTo(expected,
            options => options.Excluding(e => e.Headword).WithStrictOrdering());
    }

    [Theory]
    [InlineData("baaa")] // FTS rank
    [InlineData("b")] // non-FTS rank
    public async Task SecondaryOrder_Headword_CitationForm(string searchTerm)
    {
        MorphType[] morphTypes = [
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Root, Name = { ["en"] = "Root" }, SecondaryOrder = 1 },
            // new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Stem, Name = { ["en"] = "Stem" }, SecondaryOrder = 1 },
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.BoundRoot, Name = { ["en"] = "BoundRoot" }, SecondaryOrder = 2 },
            // new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.BoundStem, Name = { ["en"] = "BoundStem" }, SecondaryOrder = 2 },
        ];

        foreach (var morphType in morphTypes)
            await Api.CreateMorphType(morphType);

        Entry[] expected = [
            // Root/Stem - SecondaryOrder: 1
            new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = "abaaa" }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.Root/*, HomographNumber = 1*/ },
            // new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = "abaaa" }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.Stem, HomographNumber = 2 },
            // BoundRoot/BoundStem - SecondaryOrder: 2
            new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = "abaaa" }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.BoundRoot/*, HomographNumber = 1*/ },
            // new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = "abaaa" }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.BoundStem, HomographNumber = 2 },
            // Root/Stem - SecondaryOrder: 1
            new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = "baaa" }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.Root/*, HomographNumber = 1*/ },
            // new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = "baaa" }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.Stem, HomographNumber = 2 },
            // BoundRoot/BoundStem - SecondaryOrder: 2
            new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = "baaa" }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.BoundRoot/*, HomographNumber = 1*/ },
            // new() { Id = Guid.NewGuid(), CitationForm = { ["en"] = "baaa" }, LexemeForm = { ["en"] = "❌" }, MorphType = MorphTypeKind.BoundStem, HomographNumber = 2 },
        ];

        var ids = expected.Select(e => e.Id).ToHashSet();

        foreach (var entry in Faker.Faker.Random.Shuffle(expected))
            await Api.CreateEntry(entry);

        var results = (await Api.SearchEntries(searchTerm, new(new(SortField.Headword))).ToArrayAsync())
            .Where(e => ids.Contains(e.Id))
            .ToList();

        results.Should().BeEquivalentTo(expected,
            options => options.Excluding(e => e.Headword));
        results.Should().BeEquivalentTo(expected,
            options => options.Excluding(e => e.Headword).WithStrictOrdering());
    }
}
