using Argon;
using Crdt.Core;
using CrdtSample.Changes;
using CrdtSample.Models;
using CrdtLib.Changes;
using CrdtLib.Db;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Tests;

public class DataModelSimpleChanges : DataModelTestBase
{
    private readonly Guid _entity1Id = Guid.NewGuid();
    private readonly Guid _entity2Id = Guid.NewGuid();

    [Fact]
    public async Task WritingAChangeMakesASnapshot()
    {
        await WriteNextChange(SetWord(_entity1Id, "test-value"));
        var snapshot = DbContext.Snapshots.Should().ContainSingle().Subject;
        snapshot.Entity.Is<Word>().Text.Should().Be("test-value");

        await Verify(AllData());
    }

    [Fact]
    public async Task CanUpdateTheNoteField()
    {
        await WriteNextChange(SetWord(_entity1Id, "test-value"));
        await WriteNextChange(new SetWordNoteChange(_entity1Id, "a word note"));
        var word = await DataModel.GetLatest<Word>(_entity1Id);
        word!.Text.Should().Be("test-value");
        word.Note.Should().Be("a word note");
    }

    [Fact]
    public async Task WritingA2ndChangeDoesNotEffectTheFirstSnapshot()
    {
        await WriteNextChange(SetWord(_entity1Id, "change1"));
        await WriteNextChange(SetWord(_entity1Id, "change2"));

        DbContext.Snapshots.Should()
            .SatisfyRespectively(
                snap1 => snap1.Entity.Is<Word>().Text.Should().Be("change1"),
                snap2 => snap2.Entity.Is<Word>().Text.Should().Be("change2")
            );

        await Verify(AllData());
    }

    [Fact]
    public async Task WritingACommitWithMultipleChangesWorks()
    {
        await WriteNextChange([
            SetWord(_entity1Id, "first"),
            SetWord(_entity2Id, "second")
        ]);
        await Verify(AllData());
    }

    [Fact]
    public async Task WriteMultipleCommits()
    {
        await WriteNextChange(SetWord(Guid.NewGuid(), "change 1"));
        await WriteNextChange(SetWord(Guid.NewGuid(), "change 2"));
        DbContext.Snapshots.Should().HaveCount(2);
        await Verify(DbContext.Commits);

        await WriteNextChange(SetWord(Guid.NewGuid(), "change 3"));
        DbContext.Snapshots.Should().HaveCount(3);
        DataModel.GetLatestObjects<Word>().Should().HaveCount(3);
    }

    [Fact]
    public async Task WritingNoChangesWorks()
    {
        await WriteNextChange(SetWord(_entity1Id, "test-value"));
        await DataModel.AddRange(Array.Empty<Commit>());

        var snapshot = DbContext.Snapshots.Should().ContainSingle().Subject;
        snapshot.Entity.Is<Word>().Text.Should().Be("test-value");
    }

    [Fact]
    public async Task Writing2ChangesSecondOverwritesFirst()
    {
        await WriteNextChange(SetWord(_entity1Id, "first"));
        await WriteNextChange(SetWord(_entity1Id, "second"));
        var snapshot = await DbContext.Snapshots.DefaultOrder().LastAsync();
        snapshot.Entity.Is<Word>().Text.Should().Be("second");
    }

    [Fact]
    public async Task Writing2ChangesSecondOverwritesFirstWithLocalFirst()
    {
        var firstDate = DateTimeOffset.Now;
        var secondDate = DateTimeOffset.UtcNow.AddSeconds(1);
        await WriteChange(_localClientId, firstDate, SetWord(_entity1Id, "first"));
        await WriteChange(_localClientId, secondDate, SetWord(_entity1Id, "second"));
        var snapshot = await DbContext.Snapshots.DefaultOrder().LastAsync();
        snapshot.Entity.Is<Word>().Text.Should().Be("second");
    }

    [Fact]
    public async Task Writing2ChangesSecondOverwritesFirstWithUtcFirst()
    {
        var firstDate = DateTimeOffset.UtcNow;
        var secondDate = DateTimeOffset.Now.AddSeconds(1);
        await WriteChange(_localClientId, firstDate, SetWord(_entity1Id, "first"));
        await WriteChange(_localClientId, secondDate, SetWord(_entity1Id, "second"));
        var snapshot = await DbContext.Snapshots.DefaultOrder().LastAsync();
        snapshot.Entity.Is<Word>().Text.Should().Be("second");
    }

    [Fact]
    public async Task Writing2ChangesAtOnceWithMergedHistory()
    {
        await WriteNextChange(SetWord(_entity1Id, "first"));
        var second = await WriteNextChange(SetWord(_entity1Id, "second"));
        //add range has some additional logic that depends on proper commit ordering
        await DataModel.AddRange(new[]
        {
            await WriteChangeBefore(second, new SetWordNoteChange(_entity1Id, "a word note"), false),
            await WriteNextChange(SetWord(_entity1Id, "third"), false)
        });
        var word = await DataModel.GetLatest<Word>(_entity1Id);
        word!.Text.Should().Be("third");
        word.Note.Should().Be("a word note");

        await Verify(AllData());
    }

    [Fact]
    public async Task ChangeInsertedInTheMiddleOfHistoryWorks()
    {
        var first = await WriteNextChange(SetWord(_entity1Id, "first"));
        await WriteNextChange(SetWord(_entity1Id, "second"));

        await WriteChangeAfter(first, new SetWordNoteChange(_entity1Id, "a word note"));
        var word = await DataModel.GetLatest<Word>(_entity1Id);
        word!.Text.Should().Be("second");
        word.Note.Should().Be("a word note");
    }


    [Fact]
    public async Task CanTrackMultipleEntries()
    {
        await WriteNextChange(SetWord(_entity1Id, "entity1"));
        await WriteNextChange(SetWord(_entity2Id, "entity2"));

        (await DataModel.GetLatest<Word>(_entity1Id))!.Text.Should().Be("entity1");
        (await DataModel.GetLatest<Word>(_entity2Id))!.Text.Should().Be("entity2");
    }

    [Fact]
    public async Task CanCreate2EntriesOutOfOrder()
    {
        var commit1 = await WriteNextChange(SetWord(_entity1Id, "entity1"));
        await WriteChangeBefore(commit1, SetWord(_entity2Id, "entity2"));
    }

    [Fact]
    public async Task CanDeleteAnEntry()
    {
        await WriteNextChange(SetWord(_entity1Id, "test-value"));
        var deleteCommit = await WriteNextChange(new DeleteChange<Word>(_entity1Id));
        var snapshot = await DbContext.Snapshots.DefaultOrder().LastAsync();
        snapshot.Entity.DeletedAt.Should().Be(deleteCommit.DateTime);
    }


    [Fact]
    public async Task CanGetEntryLinq2Db()
    {
        await WriteNextChange(SetWord(_entity1Id, "test-value"));

        var entries = await DataModel.GetLatestObjects<Word>().ToArrayAsyncLinqToDB();
        entries.Should().ContainSingle();
    }
}
