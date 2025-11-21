using System.Diagnostics;
using Bogus;
using LcmCrdt.FullTextSearch;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Culture;
using Xunit.Abstractions;

namespace LcmCrdt.Tests.MiniLcmTests;

public class QueryEntryTests(ITestOutputHelper outputHelper) : QueryEntryTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync();
        var api = _fixture.Api;
        return api;
    }

    private async Task DeleteAllEntries()
    {
        foreach (var entry in await _fixture.Api.GetEntries().ToArrayAsync())
        {
            await _fixture.Api.DeleteEntry(entry.Id);
        }
    }

    private async Task SimpleBulkAdd(Entry[] entries, EntrySearchService entrySearchService)
    {
        await using var linqToDbContext = _fixture.DbContext.CreateLinqToDBContext();
        await linqToDbContext.GetTable<Entry>().BulkCopyAsync(entries);
        await entrySearchService.RegenerateEntrySearchTable();
    }

    [Theory]
    [InlineData(50_000)]
    //disabled because it takes too long to run
    [InlineData(100_000)]
    public async Task QueryPerformanceTesting(int count)
    {
        await DeleteAllEntries();
        var faker = new Faker { Random = new Randomizer(8675309) };
        var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToHashSet();
        var entries = ids.Select(id => new Entry { Id = id, LexemeForm = { ["en"] = faker.Name.FirstName() } }).ToArray();
        var entrySearchService = _fixture.GetService<EntrySearchServiceFactory>().CreateSearchService(_fixture.DbContext);
        await SimpleBulkAdd(entries, entrySearchService);
        entrySearchService.EntrySearchRecords.Should().HaveCount(count);
        _fixture.DbContext.Entries.Should().HaveCount(count);
        var searchString = "tes";
        var expectedResultCount = entries.Count(e => e.LexemeForm["en"].ContainsDiacriticMatch(searchString));

        var testIterations = 10;
        var startTimestamp = Stopwatch.GetTimestamp();
        for (int i = 0; i < testIterations; i++)
        {
            //search should not match anything as we only want to test the match performance
            var results = await Api.SearchEntries(searchString).ToArrayAsync();
            results.Should().HaveCount(expectedResultCount);
        }

        var totalRuntime = Stopwatch.GetElapsedTime(startTimestamp);
        var queryTime = totalRuntime / testIterations;
        var timePerEntry = queryTime / count;
        outputHelper.WriteLine(
            $"Total query time: {queryTime.TotalMilliseconds}ms, time per entry: {timePerEntry.TotalMicroseconds}microseconds");
        //Kevin H:  1   -- on my machine I got 0.2, so this is a safe margin
        //Tim H:    1.3 -- bumped, because on CI we got 1 and then 1.1 (new gha Windows runner)
        timePerEntry.TotalMicroseconds.Should().BeLessThan(1.3);
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }
}

public class NullAndEmptyQueryEntryTests : NullAndEmptyQueryEntryTestsBase
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
}
