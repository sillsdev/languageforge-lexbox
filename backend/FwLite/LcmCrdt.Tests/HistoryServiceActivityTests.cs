using LcmCrdt.Changes;
using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony.Core;
using Soenneker.Utils.AutoBogus;

namespace LcmCrdt.Tests;

public class HistoryServiceActivityTests : IAsyncLifetime, IAsyncDisposable
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(["en"]));
    private MiniLcmApiFixture _fixture = null!;

    private HistoryService Service => _fixture.GetService<HistoryService>();
    private DataModel DataModel => _fixture.DataModel;
    private Guid ClientId => _fixture.GetService<CurrentProjectService>().ProjectData.ClientId;

    public async Task InitializeAsync()
    {
        _fixture = MiniLcmApiFixture.Create();
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();

    [Fact]
    public async Task ListActivityAuthors_ReturnsDistinctAuthorsWithCounts()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Bob", AuthorId = "bob-id" });

        var authors = await Service.ListActivityAuthors();

        authors.Should().Contain(a => a.AuthorId == "alice-id" && a.AuthorName == "Alice" && a.CommitCount == 2);
        authors.Should().Contain(a => a.AuthorId == "bob-id" && a.AuthorName == "Bob" && a.CommitCount == 1);
    }

    [Fact]
    public async Task ListActivityChangeTypes_IncludesCreateEntry()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });

        var changeTypes = await Service.ListActivityChangeTypes();

        changeTypes.Should().Contain(t => t.Key == "CreateEntryChange" && t.CommitCount >= 1);
        changeTypes.Single(t => t.Key == "CreateEntryChange").Label.Should().Be("Create entry");
    }

    [Fact]
    public async Task ProjectActivity_FiltersByAuthorId()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Bob", AuthorId = "bob-id" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(AuthorId: "alice-id")).ToArrayAsync();

        activities.Should().OnlyContain(a => a.Metadata.AuthorId == "alice-id");
        activities.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ProjectActivity_ExcludeFieldWorks_HidesFieldWorksCommits()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "FieldWorks" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(ExcludeFieldWorks: true)).ToArrayAsync();

        activities.Should().NotContain(a => a.Metadata.AuthorName == "FieldWorks");
        activities.Should().Contain(a => a.Metadata.AuthorName == "Alice");
    }

    [Fact]
    public async Task ProjectActivity_SortsOldestFirst()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "First", AuthorId = "first" }, "alpha");
        await Task.Delay(5);
        await AddEntryCommit(new CommitMetadata { AuthorName = "Second", AuthorId = "second" }, "beta");

        var activities = await Service.ProjectActivity(0, 1000, new ActivityQuery(Sort: ActivitySort.OldestFirst)).ToArrayAsync();
        var firstIndex = Array.FindIndex(activities, a => a.Metadata.AuthorId == "first");
        var secondIndex = Array.FindIndex(activities, a => a.Metadata.AuthorId == "second");
        firstIndex.Should().BeGreaterThanOrEqualTo(0);
        secondIndex.Should().BeGreaterThan(firstIndex);
    }

    [Fact]
    public async Task ProjectActivity_SyncedSort_PlacesUnsyncedLast()
    {
        var syncedCommit = await AddEntryCommit(new CommitMetadata { AuthorName = "Synced", AuthorId = "synced" }, "synced-entry");
        await SetSyncDate(syncedCommit.Id, new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
        await AddEntryCommit(new CommitMetadata { AuthorName = "Unsynced", AuthorId = "unsynced" }, "unsynced-entry");

        var activities = await Service.ProjectActivity(0, 1000, new ActivityQuery(Sort: ActivitySort.SyncedNewestFirst)).ToArrayAsync();
        var syncedIndex = Array.FindIndex(activities, a => a.Metadata.AuthorId == "synced");
        var unsyncedIndex = Array.FindIndex(activities, a => a.Metadata.AuthorId == "unsynced");
        syncedIndex.Should().BeGreaterThanOrEqualTo(0);
        unsyncedIndex.Should().BeGreaterThanOrEqualTo(0);
        syncedIndex.Should().BeLessThan(unsyncedIndex);
    }

    [Fact]
    public async Task ProjectActivity_PaginationRespectsFilters()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Bob", AuthorId = "bob-id" });

        var page = await Service.ProjectActivity(0, 1, new ActivityQuery(AuthorId: "alice-id")).ToArrayAsync();

        page.Should().HaveCount(1);
        page[0].Metadata.AuthorId.Should().Be("alice-id");
    }

    [Fact]
    public async Task ProjectActivity_IncludesChangeTypes()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });

        var activity = await Service.ProjectActivity(0, 1).SingleAsync();

        activity.ChangeTypes.Should().Contain("CreateEntryChange");
    }

    private async Task<Commit> AddEntryCommit(CommitMetadata metadata, string? headword = null)
    {
        var entry = headword is null
            ? await AutoFaker.EntryReadyForCreation(_fixture.Api)
            : new Entry { Id = Guid.NewGuid(), LexemeForm = new MultiString { ["en"] = headword } };
        return await DataModel.AddChange(ClientId, new CreateEntryChange(entry), metadata);
    }

    private async Task SetSyncDate(Guid commitId, DateTimeOffset syncDate)
    {
        var db = _fixture.DbContext;
        var commit = await db.Set<Commit>().SingleAsync(c => c.Id == commitId);
        commit.SetSyncDate(syncDate);
        db.Entry(commit).Property(c => c.Metadata).IsModified = true;
        await db.SaveChangesAsync();
    }
}
