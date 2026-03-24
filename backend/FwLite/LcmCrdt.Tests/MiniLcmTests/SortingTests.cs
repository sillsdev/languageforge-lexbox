namespace LcmCrdt.Tests.MiniLcmTests;

public class SortingTests : SortingTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync();
        var api = _fixture.Api;
        return api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Theory]
    [InlineData("aaaa", SortField.Headword)] // FTS rank
    [InlineData("a", SortField.Headword)] // non-FTS rank
    [InlineData("aaaa", SortField.SearchRelevance)] // FTS rank
    [InlineData("a", SortField.SearchRelevance)] // non-FTS rank
    public async Task SecondaryOrder_DefaultsToStem(string query, SortField sortField)
    {
        MorphType[] morphTypes = [
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Stem, Name = { ["en"] = "Stem" }, SecondaryOrder = 1 },
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.BoundStem, Name = { ["en"] = "BoundStem" }, SecondaryOrder = 2 },
            new() { Id = Guid.NewGuid(), Kind = MorphTypeKind.Suffix, Name = { ["en"] = "Suffix" }, Postfix = "-", SecondaryOrder = 6 },
        ];

        foreach (var morphType in morphTypes)
            await Api.CreateMorphType(morphType);

        Entry[] expected = [
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "aaaa" }, MorphType = MorphTypeKind.Unknown }, // SecondaryOrder defaults to Stem = 1
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "aaaa" }, MorphType = MorphTypeKind.BoundStem }, // SecondaryOrder = 2
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "aaaa" }, MorphType = MorphTypeKind.Suffix }, // SecondaryOrder = 6
        ];

        var ids = expected.Select(e => e.Id).ToHashSet();

        foreach (var entry in Faker.Faker.Random.Shuffle(expected))
            await Api.CreateEntry(entry);

        var results = (await Api.SearchEntries(query, new(new(sortField))).ToArrayAsync())
            .Where(e => ids.Contains(e.Id))
            .ToList();

        results.Should().BeEquivalentTo(expected,
            options => options.WithStrictOrdering());
    }
}
