using System.Diagnostics.CodeAnalysis;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using Moq;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests;

public class HistoryServiceTests
{
    [Fact]
    public void ChangeNameHelper_ReturnsNull_WhenChangeIsNull()
    {
        // Act
        var result = HistoryService.ChangeNameHelper(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ChangeNameHelper_ReturnsHumanizedName_ForJsonPatchChange()
    {
        // Arrange
        var change = new JsonPatchChange<Entry>(Guid.NewGuid());

        // Act
        var result = HistoryService.ChangeNameHelper(change);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("Change Entry");
    }

    [Fact]
    public void ChangeNameHelper_ReturnsDeleteName_ForDeleteChange()
    {
        // Arrange
        var change = new DeleteChange<Entry>(Guid.NewGuid());

        // Act
        var result = HistoryService.ChangeNameHelper(change);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("Delete Entry");
    }

    [Fact]
    public void ChangeNameHelper_ReturnsReorderName_ForSetOrderChange()
    {
        // Arrange
        var change = new SetOrderChange<Entry>(Guid.NewGuid(), 0);

        // Act
        var result = HistoryService.ChangeNameHelper(change);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("Reorder Entry");
    }

    [Fact]
    public void ChangeNameHelper_ReturnsHumanizedName_ForCreateChange()
    {
        // Arrange
        var entry = new Entry { Id = Guid.NewGuid() };
        var change = new CreateEntryChange(entry);

        // Act
        var result = HistoryService.ChangeNameHelper(change);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Create");
    }

    [Fact]
    public void ChangeNameHelper_RemovesChangeSuffix_FromTypeName()
    {
        // Arrange
        var entry = new Entry { Id = Guid.NewGuid() };
        var change = new CreateEntryChange(entry);

        // Act
        var result = HistoryService.ChangeNameHelper(change);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotEndWith(" Change");
    }

    [Fact]
    public void ChangesNameHelper_ReturnsNoChanges_WhenListIsEmpty()
    {
        // Arrange
        var changes = new List<ChangeEntity<IChange>>();

        // Act
        var result = HistoryService.ChangesNameHelper(changes);

        // Assert
        result.Should().Be("No changes");
    }

    [Fact]
    public void ChangesNameHelper_ReturnsSingleChangeName_WhenListHasOneItem()
    {
        // Arrange
        var entry = new Entry { Id = Guid.NewGuid() };
        var change = new CreateEntryChange(entry);
        var changes = new List<ChangeEntity<IChange>>
        {
            new ChangeEntity<IChange> { Change = change, CommitId = Guid.NewGuid(), Index = 0, EntityId = entry.Id }
        };

        // Act
        var result = HistoryService.ChangesNameHelper(changes);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBe("No changes");
        result.Should().NotContain("other change");
    }

    [Fact]
    public void ChangesNameHelper_ReturnsChangeCount_WhenListHasMoreThan10Items()
    {
        // Arrange
        var changes = new List<ChangeEntity<IChange>>();
        for (int i = 0; i < 15; i++)
        {
            var entry = new Entry { Id = Guid.NewGuid() };
            changes.Add(new ChangeEntity<IChange>
            {
                Change = new CreateEntryChange(entry),
                CommitId = Guid.NewGuid(),
                Index = i,
                EntityId = entry.Id
            });
        }

        // Act
        var result = HistoryService.ChangesNameHelper(changes);

        // Assert
        result.Should().Be("15 changes");
    }

    [Fact]
    public void ChangesNameHelper_ReturnsFirstChangeWithOthersCount_WhenListHas2To10Items()
    {
        // Arrange
        var changes = new List<ChangeEntity<IChange>>();
        for (int i = 0; i < 3; i++)
        {
            var entry = new Entry { Id = Guid.NewGuid() };
            changes.Add(new ChangeEntity<IChange>
            {
                Change = new CreateEntryChange(entry),
                CommitId = Guid.NewGuid(),
                Index = i,
                EntityId = entry.Id
            });
        }

        // Act
        var result = HistoryService.ChangesNameHelper(changes);

        // Assert
        result.Should().Contain("(+2 other changes)");
    }

    [Fact]
    public void ChangesNameHelper_UsesSingularForm_WhenTwoChanges()
    {
        // Arrange
        var changes = new List<ChangeEntity<IChange>>();
        for (int i = 0; i < 2; i++)
        {
            var entry = new Entry { Id = Guid.NewGuid() };
            changes.Add(new ChangeEntity<IChange>
            {
                Change = new CreateEntryChange(entry),
                CommitId = Guid.NewGuid(),
                Index = i,
                EntityId = entry.Id
            });
        }

        // Act
        var result = HistoryService.ChangesNameHelper(changes);

        // Assert
        result.Should().Contain("(+1 other change)");
        result.Should().NotContain("changes)");
    }

    [Fact]
    public void HistoryLineItem_Constructor_HandlesNullChange()
    {
        // Arrange
        var commitId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var snapshotId = Guid.NewGuid();

        // Act
        var item = new HistoryLineItem(
            commitId,
            entityId,
            timestamp,
            snapshotId,
            0,
            (IChange?)null,
            null,
            "Entry",
            "Test User");

        // Assert
        item.Should().NotBeNull();
        item.ChangeName.Should().BeNull();
        item.CommitId.Should().Be(commitId);
        item.EntityId.Should().Be(entityId);
    }

    [Fact]
    public void HistoryLineItem_Constructor_SetsChangeName_WhenChangeIsNotNull()
    {
        // Arrange
        var commitId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var entry = new Entry { Id = entityId };
        var change = new CreateEntryChange(entry);

        // Act
        var item = new HistoryLineItem(
            commitId,
            entityId,
            timestamp,
            null,
            0,
            change,
            null,
            "Entry",
            "Test User");

        // Assert
        item.Should().NotBeNull();
        item.ChangeName.Should().NotBeNull();
        item.ChangeName.Should().Contain("Create");
    }

    [Fact]
    public void HistoryLineItem_DirectConstructor_AllowsNullChangeName()
    {
        // Arrange
        var commitId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var item = new HistoryLineItem(
            commitId,
            entityId,
            timestamp,
            null,
            0,
            null, // changeName
            null, // entity
            null, // entityName
            "Test User");

        // Assert
        item.Should().NotBeNull();
        item.ChangeName.Should().BeNull();
    }

    [Theory]
    [InlineData("Create Entry")]
    [InlineData("Delete Sense")]
    [InlineData("Change Example Sentence")]
    public void HistoryLineItem_DirectConstructor_PreservesChangeName(string changeName)
    {
        // Arrange
        var commitId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var item = new HistoryLineItem(
            commitId,
            entityId,
            timestamp,
            null,
            0,
            changeName,
            null,
            null,
            "Test User");

        // Assert
        item.ChangeName.Should().Be(changeName);
    }

    [Fact]
    public void ChangeNameHelper_HandlesComplexJsonPatchChanges()
    {
        // Arrange
        var change = new JsonPatchChange<Sense>(Guid.NewGuid());

        // Act
        var result = HistoryService.ChangeNameHelper(change);

        // Assert
        result.Should().Be("Change Sense");
    }

    [Fact]
    public void ChangeNameHelper_HandlesComplexDeleteChanges()
    {
        // Arrange
        var change = new DeleteChange<ExampleSentence>(Guid.NewGuid());

        // Act
        var result = HistoryService.ChangeNameHelper(change);

        // Assert
        result.Should().Be("Delete Example Sentence");
    }

    [Fact]
    public void ChangesNameHelper_HandlesNullChangeInList()
    {
        // Arrange - This shouldn't happen in practice, but we test defensive coding
        var changes = new List<ChangeEntity<IChange>>
        {
            new ChangeEntity<IChange>
            {
                Change = null\!,
                CommitId = Guid.NewGuid(),
                Index = 0,
                EntityId = Guid.NewGuid()
            }
        };

        // Act
        var result = HistoryService.ChangesNameHelper(changes);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ProjectActivity_ChangeName_UsesChangesNameHelper()
    {
        // Arrange
        var commitId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var entry = new Entry { Id = Guid.NewGuid() };
        var changes = new List<ChangeEntity<IChange>>
        {
            new ChangeEntity<IChange>
            {
                Change = new CreateEntryChange(entry),
                CommitId = commitId,
                Index = 0,
                EntityId = entry.Id
            }
        };
        var metadata = new CommitMetadata { ClientId = Guid.NewGuid() };

        // Act
        var activity = new ProjectActivity(commitId, timestamp, changes, metadata);

        // Assert
        activity.ChangeName.Should().NotBeNull();
        activity.ChangeName.Should().NotBe("No changes");
    }
}