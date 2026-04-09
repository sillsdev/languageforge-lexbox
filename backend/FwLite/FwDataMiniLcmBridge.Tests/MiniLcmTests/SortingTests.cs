using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class SortingTests(ProjectLoaderFixture fixture) : SortingTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("sorting-test", "en", "en"));
    }

    [Theory]
    [InlineData("aaaa", SortField.Headword)] // FTS
    [InlineData("a", SortField.Headword)] // non-FTS
    [InlineData("aaaa", SortField.SearchRelevance)] // FTS
    [InlineData("a", SortField.SearchRelevance)] // non-FTS
    public async Task SecondaryOrder_DefaultsToStem(string query, SortField sortField)
    {
        var unknownMorphTypeEntryId = Guid.NewGuid();
        Entry[] expected = [
            new() { Id = unknownMorphTypeEntryId, LexemeForm = { ["en"] = "aaaa" }, MorphType = MorphTypeKind.Unknown }, // SecondaryOrder defaults to Stem = 0
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "aaaa" }, MorphType = MorphTypeKind.BoundStem }, // SecondaryOrder = 10
            new() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = "aaaa" }, MorphType = MorphTypeKind.Suffix }, // SecondaryOrder = 70
        ];

        var ids = expected.Select(e => e.Id).ToHashSet();

        foreach (var entry in Faker.Faker.Random.Shuffle(expected))
            await Api.CreateEntry(entry);

        var fwDataApi = (BaseApi as FwDataMiniLcmApi)!;
        await fwDataApi.Cache.DoUsingNewOrCurrentUOW("Clear morph type",
            "Revert morph type",
            () =>
            {
                // the fwdata api doesn't allow creating entries with MorphType.Other or Unknown, so we force it
                var unknownMorphTypeEntry = fwDataApi.EntriesRepository.GetObject(unknownMorphTypeEntryId);
                unknownMorphTypeEntry.LexemeFormOA.MorphTypeRA = null;
                return ValueTask.CompletedTask;
            });

        var results = (await Api.SearchEntries(query, new(new(sortField))).ToArrayAsync())
            .Where(e => ids.Contains(e.Id))
            .ToList();

        results.Should().BeEquivalentTo(expected,
            options => options.WithStrictOrdering());
    }
}
