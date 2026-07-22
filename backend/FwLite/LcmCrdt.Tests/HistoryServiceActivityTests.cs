using LcmCrdt.Changes;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests;

public class HistoryServiceActivityTests : HistoryServiceActivityTestsBase
{
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
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPublicationCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPartOfSpeechCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });

        var changeTypes = await Service.ListActivityChangeTypes();

        changeTypes.Should().Contain(t => t.Key == nameof(CreateEntryChange) && t.CommitCount >= 2);
        changeTypes.Should().Contain(t => t.Key == nameof(CreatePublicationChange) && t.CommitCount >= 1);
        changeTypes.Should().Contain(t => t.Key == nameof(CreatePartOfSpeechChange) && t.CommitCount >= 1);
    }

    [Fact]
    public async Task ProjectActivity_FiltersByAuthorId()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Bob", AuthorId = "bob-id" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(AuthorFilterKeys: ["alice-id"]));

        activities.Should().OnlyContain(a => a.Metadata.AuthorId == "alice-id");
        activities.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ProjectActivity_AuthorFilterKeys_ExcludesUnselectedAuthors()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "FieldWorks" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(AuthorFilterKeys: ["alice-id"]));

        activities.Should().NotContain(a => a.Metadata.AuthorName == "FieldWorks");
        activities.Should().Contain(a => a.Metadata.AuthorName == "Alice");
    }

    [Fact]
    public async Task ProjectActivity_SortsOldestFirst()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "First", AuthorId = "first" }, "alpha");
        await Task.Delay(5);
        await AddEntryCommit(new CommitMetadata { AuthorName = "Second", AuthorId = "second" }, "beta");

        var activities = await Service.ProjectActivity(0, 1000, new ActivityQuery(Sort: ActivitySort.OldestFirst));
        var firstIndex = Array.FindIndex(activities, a => a.Metadata.AuthorId == "first");
        var secondIndex = Array.FindIndex(activities, a => a.Metadata.AuthorId == "second");
        firstIndex.Should().BeGreaterThanOrEqualTo(0);
        secondIndex.Should().BeGreaterThan(firstIndex);
    }

    [Fact]
    public async Task ProjectActivity_SyncedNewestFirst_PlacesUnsyncedFirst()
    {
        var syncedCommit = await AddEntryCommit(new CommitMetadata { AuthorName = "Synced", AuthorId = "synced" }, "synced-entry");
        await SetSyncDate(syncedCommit.Id, new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
        await AddEntryCommit(new CommitMetadata { AuthorName = "Unsynced", AuthorId = "unsynced" }, "unsynced-entry");

        var activities = await Service.ProjectActivity(0, 1000, new ActivityQuery(Sort: ActivitySort.SyncedNewestFirst));
        var commitAuthors = activities.Select(a => a.Metadata.AuthorId).Where(a => a is not null);
        commitAuthors.Should().ContainInOrder(["unsynced", "synced"]);
    }

    [Fact]
    public async Task ProjectActivity_SyncedOldestFirst_PlacesUnsyncedLast()
    {
        var syncedCommit = await AddEntryCommit(new CommitMetadata { AuthorName = "Synced", AuthorId = "synced" }, "synced-entry");
        await SetSyncDate(syncedCommit.Id, new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
        await AddEntryCommit(new CommitMetadata { AuthorName = "Unsynced", AuthorId = "unsynced" }, "unsynced-entry");

        var activities = await Service.ProjectActivity(0, 1000, new ActivityQuery(Sort: ActivitySort.SyncedOldestFirst));
        var commitAuthors = activities.Select(a => a.Metadata.AuthorId).Where(a => a is not null);
        commitAuthors.Should().ContainInOrder(["synced", "unsynced"]);
    }

    [Fact]
    public async Task ProjectActivity_PaginationRespectsFilters()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddEntryCommit(new CommitMetadata { AuthorName = "Bob", AuthorId = "bob-id" });

        var page = await Service.ProjectActivity(0, 1, new ActivityQuery(AuthorFilterKeys: ["alice-id"]));

        page.Should().HaveCount(1);
        page[0].Metadata.AuthorId.Should().Be("alice-id");
    }

    [Fact]
    public async Task ProjectActivity_FiltersByChangeTypeKeys()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPublicationCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(ChangeTypeKeys: [nameof(CreateEntryChange)]));

        activities.Should().OnlyContain(a => a.Changes.Any(c => c.Entity.Change is CreateEntryChange));
        activities.Should().HaveCountGreaterThanOrEqualTo(1);
        activities.Should().NotContain(a => a.Changes.Any(c => c.Entity.Change is CreatePublicationChange));
    }

    [Fact]
    public async Task ProjectActivity_ChangeTypeKeys_FiltersMultipleTypes()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPublicationCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        await AddNewPartOfSpeechCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });
        (await Service.ProjectActivity(0, 100, new ActivityQuery()))
            .Should().Contain(a => a.Changes.Any(c => c.Entity.Change is CreatePartOfSpeechChange));

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery(ChangeTypeKeys: [nameof(CreateEntryChange), nameof(CreatePublicationChange)]));

        activities.Should().HaveCountGreaterThanOrEqualTo(2); // the entry + publication commits, so the filter can't pass vacuously on an empty result
        activities.Should().OnlyContain(a => a.Changes.Any(c => c.Entity.Change is CreateEntryChange || c.Entity.Change is CreatePublicationChange));
        activities.Should().NotContain(a => a.Changes.Any(c => c.Entity.Change is CreatePartOfSpeechChange));
    }

    [Fact]
    public async Task ProjectActivity_IncludesChanges()
    {
        await AddEntryCommit(new CommitMetadata { AuthorName = "Alice", AuthorId = "alice-id" });

        var activity = (await Service.ProjectActivity(0, 1)).Single();

        activity.Changes.Should().ContainSingle(c => c.Entity.Change is CreateEntryChange);
    }
}
