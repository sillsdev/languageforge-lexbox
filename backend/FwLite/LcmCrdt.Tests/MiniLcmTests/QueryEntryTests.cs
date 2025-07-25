﻿using System.Diagnostics;
using Bogus;
using LcmCrdt.FullTextSearch;
using LinqToDB;
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

    [Theory]
    [InlineData(50_000)]
    [InlineData(100_000)]
    public async Task QueryPerformanceTesting(int count)
    {
        await DeleteAllEntries();
        var faker = new Faker { Random = new Randomizer(8675309) };
        var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToHashSet();
        var entries = ids.Select(id => new Entry { Id = id, LexemeForm = { ["en"] = faker.Name.FirstName() } }).ToArray();
        await _fixture.Api.BulkCreateEntries(entries.ToAsyncEnumerable());
        var entrySearchService = _fixture.GetService<EntrySearchServiceFactory>().CreateSearchService(_fixture.DbContext);
        entrySearchService.EntrySearchRecords.Should().HaveCount(count);
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
        timePerEntry.TotalMicroseconds.Should().BeLessThan(1);//on my machine I got 0.2, so this is a safe margin
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }
}
