using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Exceptions;
using MiniLcm.Media;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Tests;

namespace FwLiteProjectSync.Tests;

public class CrdtEntryMoveSyncTests(ExtraWritingSystemsSyncFixture fixture) : EntryMoveSyncTestsBase(fixture)
{
    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.CrdtApi;
    }

    [Fact]
    public async Task SyncFull_ExampleSentenceMovedToSenseDeletedInCrdt_DoesNotThrow()
    {
        var exampleId = Guid.NewGuid();
        var sourceSense = new Sense
        {
            Id = Guid.NewGuid(),
            Gloss = { { "en", "source" } },
            ExampleSentences = [new ExampleSentence { Id = exampleId, Sentence = { { "en", new RichString("example") } } }]
        };
        var targetSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "target" } } };
        var entry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "victim" } },
            Senses = [sourceSense, targetSense]
        });
        await Api.DeleteSense(entry.Id, targetSense.Id);

        var after = entry.Copy();
        var movedExample = after.Senses.Single(s => s.Id == sourceSense.Id).ExampleSentences.Single();
        after.Senses.Single(s => s.Id == sourceSense.Id).ExampleSentences.Clear();
        movedExample.SenseId = targetSense.Id;
        after.Senses.Single(s => s.Id == targetSense.Id).ExampleSentences.Add(movedExample);

        await EntrySync.SyncFull(entry, after, Api);

        // the move's target sense was already deleted in CRDT: the example must be gone, not orphaned on a dead sense
        var actual = await Api.GetEntry(entry.Id);
        actual.Should().NotBeNull();
        actual.Senses.Select(s => s.Id).Should().Equal(sourceSense.Id);
        actual.Senses[0].ExampleSentences.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncFull_SenseMovedToEntryDeletedInCrdt_DoesNotThrow()
    {
        var senseId = Guid.NewGuid();
        var sourceEntry = await Api.CreateEntry(new() { LexemeForm = { { "en", "source" } }, Senses = [new() { Id = senseId }] });
        var targetEntry = await Api.CreateEntry(new() { LexemeForm = { { "en", "target" } } });
        await Api.DeleteEntry(targetEntry.Id);

        var sourceEntryAfter = sourceEntry.Copy();
        sourceEntryAfter.Senses.Clear();
        var targetEntryAfter = targetEntry.Copy();
        targetEntryAfter.Senses.Add(new Sense { Id = senseId, EntryId = targetEntry.Id });

        await EntrySync.SyncFull([sourceEntry, targetEntry], [sourceEntryAfter, targetEntryAfter], Api);

        // the move's target entry was already deleted in CRDT: the sense must be gone, not orphaned on a dead entry
        (await Api.GetEntry(targetEntry.Id)).Should().BeNull();
        (await Api.GetEntry(sourceEntry.Id))!.Senses.Should().BeEmpty();
        var tryGetSense = () => Api.GetSense(targetEntry.Id, senseId);
        (await tryGetSense.Should().NotThrowAsync()).Which.Should().BeNull();
    }
}

public class FwDataEntryMoveSyncTests(ExtraWritingSystemsSyncFixture fixture) : EntryMoveSyncTestsBase(fixture)
{
    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.FwDataApi;
    }
}

public abstract class EntryMoveSyncTestsBase(ExtraWritingSystemsSyncFixture fixture) : IClassFixture<ExtraWritingSystemsSyncFixture>, IAsyncLifetime
{
    public Task InitializeAsync()
    {
        // Mirror production sync (CrdtFwdataProjectSyncService): validation only, no normalization,
        // because the data is already normalized on both sides.
        Api = TestMiniLcmWrappers.CreateValidationFactory().Create(GetApi(_fixture));
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected abstract IMiniLcmApi GetApi(SyncFixture fixture);

    private readonly SyncFixture _fixture = fixture;
    protected IMiniLcmApi Api = null!;

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CanSyncSenseMovedToDifferentEntry(bool sourceEntryDeleted, bool targetEntryCreated)
    {
        var senseId = Guid.NewGuid();
        var sourceEntry = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "source" } },
            Senses = [new() { Id = senseId, Gloss = { { "en", "original" } } }]
        });
        var sourceEntryAfter = sourceEntry.Copy();
        sourceEntryAfter.Senses.Clear(); // sense is moved from here

        var targetEntry = new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", "target" } } };
        if (!targetEntryCreated)
            targetEntry = await Api.CreateEntry(targetEntry);

        var targetEntryAfter = targetEntry.Copy();
        // moved AND edited in the same sync: the move must be followed by the field diff
        targetEntryAfter.Senses.Add(new Sense { Id = senseId, Gloss = { { "en", "edited" } } }); // sense is moved to here

        Entry[] before = targetEntryCreated
            ? [sourceEntry]
            : [sourceEntry, targetEntry];
        Entry[] after = sourceEntryDeleted
            ? [targetEntryAfter]
            : [sourceEntryAfter, targetEntryAfter];

        await EntrySync.SyncFull(before, after, Api);

        var actualSourceEntry = await Api.GetEntry(sourceEntry.Id);
        if (sourceEntryDeleted)
        {
            actualSourceEntry.Should().BeNull();
        }
        else
        {
            actualSourceEntry.Should().NotBeNull();
            actualSourceEntry.Senses.Should().BeEmpty();
        }

        var actualTargetEntry = await Api.GetEntry(targetEntry.Id);
        actualTargetEntry.Should().NotBeNull();
        actualTargetEntry.Senses.Should().HaveCount(1);
        actualTargetEntry.Senses[0].Id.Should().Be(senseId);

        var actualMovedSense = await Api.GetSense(actualTargetEntry.Id, senseId);
        actualMovedSense.Should().NotBeNull();
        actualMovedSense.EntryId.Should().Be(targetEntry.Id);
        actualMovedSense.Gloss["en"].Should().Be("edited");

        var tryGetSenseFromSource = () => Api.GetSense(sourceEntry.Id, senseId);
        await tryGetSenseFromSource.Should().ThrowAsync<NotFoundException>().WithMessage("*does not belong to the expected entry*");
    }

    [Fact]
    public async Task CanSyncSensesMovedIntoPositionInTargetEntry()
    {
        var moved1Id = Guid.NewGuid();
        var moved2Id = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var lastId = Guid.NewGuid();
        var source1 = await Api.CreateEntry(new() { LexemeForm = { { "en", "source1" } }, Senses = [new() { Id = moved1Id, Gloss = { { "en", "m1" } } }] });
        var source2 = await Api.CreateEntry(new() { LexemeForm = { { "en", "source2" } }, Senses = [new() { Id = moved2Id, Gloss = { { "en", "m2" } } }] });
        var target = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "target" } },
            Senses = [new() { Id = firstId, Gloss = { { "en", "first" } } }, new() { Id = lastId, Gloss = { { "en", "last" } } }]
        });

        var source1After = source1.Copy();
        var source2After = source2.Copy();
        var targetAfter = target.Copy();
        var moved1 = source1After.Senses.Single();
        source1After.Senses.Clear();
        var moved2 = source2After.Senses.Single();
        source2After.Senses.Clear();
        moved1.EntryId = target.Id;
        moved2.EntryId = target.Id;
        targetAfter.Senses.Insert(1, moved1);
        targetAfter.Senses.Insert(2, moved2);

        await EntrySync.SyncFull([source1, source2, target], [source1After, source2After, targetAfter], Api);

        var actualTarget = await Api.GetEntry(target.Id);
        actualTarget.Should().NotBeNull();
        actualTarget.Senses.Select(s => s.Id).Should().Equal(firstId, moved1Id, moved2Id, lastId);
        (await Api.GetEntry(source1.Id))!.Senses.Should().BeEmpty();
        (await Api.GetEntry(source2.Id))!.Senses.Should().BeEmpty();
    }

    [Theory]
    // (sameEntry, sourceSenseFirst); sourceSenseFirst determines whether the sync processes the remove (source sense) or the add (target sense) first.
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task CanSyncExampleSentenceMovedToDifferentSense(bool sameEntry, bool sourceSenseFirst)
    {
        var exampleId = Guid.NewGuid();
        var sourceSense = new Sense
        {
            Id = Guid.NewGuid(),
            Gloss = { { "en", "source" } },
            ExampleSentences =
            [
                new ExampleSentence
                {
                    Id = exampleId,
                    Sentence = { { "en", new RichString("example") } },
                    Translations = [new Translation { Id = Guid.NewGuid(), Text = { { "en", new RichString("translation") } } }]
                }
            ]
        };
        var targetSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "target" } } };

        Entry[] before = sameEntry ? await OneSharedEntry() : await TwoEntries();
        var sourceEntryId = before.Single(e => e.Senses.Any(s => s.Id == sourceSense.Id)).Id;
        var targetEntryId = before.Single(e => e.Senses.Any(s => s.Id == targetSense.Id)).Id;

        async Task<Entry[]> OneSharedEntry()
        {
            return
            [
                await Api.CreateEntry(new()
                {
                    LexemeForm = { { "en", "entry" } },
                    Senses = sourceSenseFirst ? [sourceSense, targetSense] : [targetSense, sourceSense]
                })
            ];
        }

        async Task<Entry[]> TwoEntries()
        {
            return sourceSenseFirst
                ? [await CreateEntryWith("source-entry", sourceSense), await CreateEntryWith("target-entry", targetSense)]
                : [await CreateEntryWith("target-entry", targetSense), await CreateEntryWith("source-entry", sourceSense)];
        }

        Task<Entry> CreateEntryWith(string lexemeForm, Sense sense) =>
            Api.CreateEntry(new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", lexemeForm } }, Senses = [sense] });

        var after = before.Select(e => e.Copy()).ToArray();
        var sourceSenseAfter = after.SelectMany(e => e.Senses).Single(s => s.Id == sourceSense.Id);
        var targetSenseAfter = after.SelectMany(e => e.Senses).Single(s => s.Id == targetSense.Id);
        var movedExample = sourceSenseAfter.ExampleSentences.Single();
        sourceSenseAfter.ExampleSentences.Clear(); // example is moved from here
        movedExample.SenseId = targetSense.Id;
        // moved AND edited in the same sync: the move must be followed by the field diff
        movedExample.Sentence["en"] = new RichString("edited example", "en");
        movedExample.Translations[0].Text["en"] = new RichString("edited translation", "en");
        targetSenseAfter.ExampleSentences.Add(movedExample); // example is moved to here

        await EntrySync.SyncFull(before, after, Api);

        var actualTargetSense = await Api.GetSense(targetEntryId, targetSense.Id);
        actualTargetSense.Should().NotBeNull();
        actualTargetSense.ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);

        var actualSourceSense = await Api.GetSense(sourceEntryId, sourceSense.Id);
        actualSourceSense.Should().NotBeNull();
        actualSourceSense.ExampleSentences.Should().BeEmpty();

        var actualExample = await Api.GetExampleSentence(targetEntryId, targetSense.Id, exampleId);
        actualExample.Should().NotBeNull();
        actualExample.Sentence["en"].Should().BeEquivalentTo(new RichString("edited example", "en"));
        actualExample.Translations.Should().ContainSingle()
            .Which.Text["en"].Should().BeEquivalentTo(new RichString("edited translation", "en"));
    }

    [Fact]
    public async Task CanSyncExampleSentencesMovedIntoPositionInTargetSense()
    {
        var moved1Id = Guid.NewGuid();
        var moved2Id = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var lastId = Guid.NewGuid();
        var source1 = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "source1" } }, ExampleSentences = [new ExampleSentence { Id = moved1Id, Sentence = { { "en", new RichString("m1") } } }] };
        var source2 = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "source2" } }, ExampleSentences = [new ExampleSentence { Id = moved2Id, Sentence = { { "en", new RichString("m2") } } }] };
        var target = new Sense
        {
            Id = Guid.NewGuid(),
            Gloss = { { "en", "target" } },
            ExampleSentences =
            [
                new ExampleSentence { Id = firstId, Sentence = { { "en", new RichString("first") } } },
                new ExampleSentence { Id = lastId, Sentence = { { "en", new RichString("last") } } }
            ]
        };
        var entry = await Api.CreateEntry(new() { LexemeForm = { { "en", "entry" } }, Senses = [source1, source2, target] });

        var after = entry.Copy();
        var afterSource1 = after.Senses.Single(s => s.Id == source1.Id);
        var afterSource2 = after.Senses.Single(s => s.Id == source2.Id);
        var afterTarget = after.Senses.Single(s => s.Id == target.Id);
        var moved1 = afterSource1.ExampleSentences.Single();
        afterSource1.ExampleSentences.Clear();
        var moved2 = afterSource2.ExampleSentences.Single();
        afterSource2.ExampleSentences.Clear();
        moved1.SenseId = target.Id;
        moved2.SenseId = target.Id;
        afterTarget.ExampleSentences.Insert(1, moved1);
        afterTarget.ExampleSentences.Insert(2, moved2);

        await EntrySync.SyncFull(entry, after, Api);

        var actualTarget = await Api.GetSense(entry.Id, target.Id);
        actualTarget.Should().NotBeNull();
        actualTarget.ExampleSentences.Select(e => e.Id).Should().Equal(firstId, moved1Id, moved2Id, lastId);
        (await Api.GetSense(entry.Id, source1.Id))!.ExampleSentences.Should().BeEmpty();
        (await Api.GetSense(entry.Id, source2.Id))!.ExampleSentences.Should().BeEmpty();
    }

    [Theory]
    // the source entry is listed first, so its delete is processed before the target's add: the cascade-hazard order
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanSyncExampleSentenceMovedOutOfDeletedParent(bool wholeEntryDeleted)
    {
        var exampleId = Guid.NewGuid();
        var sourceEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "source-entry" } },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = { { "en", "source" } },
                    ExampleSentences = [new ExampleSentence { Id = exampleId, Sentence = { { "en", new RichString("example") } } }]
                }
            ]
        });
        var targetSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "target" } } };
        var targetEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "target-entry" } },
            Senses = [targetSense]
        });

        var sourceEntryAfter = sourceEntry.Copy();
        sourceEntryAfter.Senses.Clear(); // source sense is deleted, but its example survives elsewhere
        var targetEntryAfter = targetEntry.Copy();
        var movedExample = sourceEntry.Senses[0].ExampleSentences[0].Copy();
        movedExample.SenseId = targetSense.Id;
        targetEntryAfter.Senses[0].ExampleSentences.Add(movedExample);

        Entry[] before = [sourceEntry, targetEntry];
        Entry[] after = wholeEntryDeleted ? [targetEntryAfter] : [sourceEntryAfter, targetEntryAfter];

        await EntrySync.SyncFull(before, after, Api);

        var actualTargetSense = await Api.GetSense(targetEntry.Id, targetSense.Id);
        actualTargetSense.Should().NotBeNull();
        actualTargetSense.ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);
        if (wholeEntryDeleted)
        {
            (await Api.GetEntry(sourceEntry.Id)).Should().BeNull();
        }
        else
        {
            var actualSourceEntry = await Api.GetEntry(sourceEntry.Id);
            actualSourceEntry.Should().NotBeNull();
            actualSourceEntry.Senses.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task CanSyncExampleSentenceMovedToCreatedSense()
    {
        var exampleId = Guid.NewGuid();
        var entry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "entry" } },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = { { "en", "source" } },
                    ExampleSentences = [new ExampleSentence { Id = exampleId, Sentence = { { "en", new RichString("example") } } }]
                }
            ]
        });

        var after = entry.Copy();
        var movedExample = after.Senses[0].ExampleSentences[0];
        after.Senses[0].ExampleSentences.Clear();
        var createdSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "created target" } } };
        movedExample.SenseId = createdSense.Id;
        createdSense.ExampleSentences.Add(movedExample);
        after.Senses.Add(createdSense);

        await EntrySync.SyncFull(entry, after, Api);

        var actualCreatedSense = await Api.GetSense(entry.Id, createdSense.Id);
        actualCreatedSense.Should().NotBeNull();
        // the sense is created without its moved-in examples and filled in afterward; its own fields must survive that split
        actualCreatedSense.Gloss["en"].Should().Be("created target");
        actualCreatedSense.ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);
        actualCreatedSense.ExampleSentences[0].Sentence["en"]!.Spans.Should().ContainSingle().Which.Text.Should().Be("example");
        var actualSourceSense = await Api.GetSense(entry.Id, entry.Senses[0].Id);
        actualSourceSense.Should().NotBeNull();
        actualSourceSense.ExampleSentences.Should().BeEmpty();
    }

    [Fact]
    public async Task CanSyncExampleSentenceMovedToCreatedEntry()
    {
        var exampleId = Guid.NewGuid();
        var sourceEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "source-entry" } },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = { { "en", "source" } },
                    ExampleSentences = [new ExampleSentence { Id = exampleId, Sentence = { { "en", new RichString("example") } } }]
                }
            ]
        });

        var sourceEntryAfter = sourceEntry.Copy();
        var movedExample = sourceEntryAfter.Senses[0].ExampleSentences[0];
        sourceEntryAfter.Senses[0].ExampleSentences.Clear();
        var createdSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "created target" } } };
        movedExample.SenseId = createdSense.Id;
        createdSense.ExampleSentences.Add(movedExample);
        var createdEntry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "created-entry" } },
            Senses = [createdSense]
        };

        await EntrySync.SyncFull([sourceEntry], [sourceEntryAfter, createdEntry], Api);

        var actualCreatedEntry = await Api.GetEntry(createdEntry.Id);
        actualCreatedEntry.Should().NotBeNull();
        // the entry is created without its moved-in examples and filled in afterward; its own fields must survive that split
        actualCreatedEntry.LexemeForm["en"].Should().Be("created-entry");
        var actualCreatedSense = await Api.GetSense(createdEntry.Id, createdSense.Id);
        actualCreatedSense.Should().NotBeNull();
        actualCreatedSense.ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);
        actualCreatedSense.ExampleSentences[0].Sentence["en"]!.Spans.Should().ContainSingle().Which.Text.Should().Be("example");
        var actualSourceSense = await Api.GetSense(sourceEntry.Id, sourceEntry.Senses[0].Id);
        actualSourceSense.Should().NotBeNull();
        actualSourceSense.ExampleSentences.Should().BeEmpty();
    }

    [Fact]
    public async Task CanSyncExampleSentenceMovedFromDeletedEntryToCreatedEntry()
    {
        var exampleId = Guid.NewGuid();
        var sourceEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "source-entry" } },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = { { "en", "source" } },
                    ExampleSentences = [new ExampleSentence { Id = exampleId, Sentence = { { "en", new RichString("example") } } }]
                }
            ]
        });

        var createdSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "created target" } } };
        var movedExample = sourceEntry.Senses[0].ExampleSentences[0].Copy();
        movedExample.SenseId = createdSense.Id;
        createdSense.ExampleSentences.Add(movedExample);
        var createdEntry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "created-entry" } },
            Senses = [createdSense]
        };

        // the example's old parent is deleted with its whole entry while the new parent is created, all in one sync
        await EntrySync.SyncFull([sourceEntry], [createdEntry], Api);

        (await Api.GetEntry(sourceEntry.Id)).Should().BeNull();
        var actualCreatedSense = await Api.GetSense(createdEntry.Id, createdSense.Id);
        actualCreatedSense.Should().NotBeNull();
        actualCreatedSense.ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);
    }

    [Fact]
    public async Task CanSyncSenseWithChildrenMovedToCreatedEntry()
    {
        var sourceEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "source-entry" } },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = { { "en", "moving sense" } },
                    Pictures = [new Picture { Id = Guid.NewGuid(), MediaUri = new MediaUri(Guid.NewGuid(), "localhost") }],
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Id = Guid.NewGuid(),
                            Sentence = { { "en", new RichString("example") } },
                            Translations = [new Translation { Id = Guid.NewGuid(), Text = { { "en", new RichString("translation") } } }]
                        }
                    ]
                }
            ]
        });

        // the moved sense brings its picture, example and translation along; they must not be mistaken for their own moves
        var sourceEntryAfter = sourceEntry.Copy();
        var movedSense = sourceEntryAfter.Senses[0];
        sourceEntryAfter.Senses.Clear();
        var createdEntry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "created-entry" } },
            Senses = [movedSense]
        };

        await EntrySync.SyncFull([sourceEntry], [sourceEntryAfter, createdEntry], Api);

        var actualCreatedEntry = await Api.GetEntry(createdEntry.Id);
        actualCreatedEntry.Should().NotBeNull();
        actualCreatedEntry.Senses.Select(s => s.Id).Should().Equal(movedSense.Id);
        actualCreatedEntry.Senses[0].Pictures.Select(p => p.Id).Should().Equal(movedSense.Pictures[0].Id);
        var actualExample = actualCreatedEntry.Senses[0].ExampleSentences.Should().ContainSingle().Which;
        actualExample.Id.Should().Be(movedSense.ExampleSentences[0].Id);
        actualExample.Translations.Should().ContainSingle()
            .Which.Id.Should().Be(movedSense.ExampleSentences[0].Translations[0].Id);
        (await Api.GetEntry(sourceEntry.Id))!.Senses.Should().BeEmpty();
    }

    [Theory]
    // (sourceEntryFirst, targetEntryCreated); sourceEntryFirst decides whether the example's old sense (a remove) or the moved sense (an add) is diffed first
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task CanSyncExampleSentenceMovedIntoSenseThatMovedToDifferentEntry(bool sourceEntryFirst, bool targetEntryCreated)
    {
        var exampleId = Guid.NewGuid();
        var movingSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "moving" } } };
        var stayingSense = new Sense
        {
            Id = Guid.NewGuid(),
            Gloss = { { "en", "staying" } },
            ExampleSentences = [new ExampleSentence { Id = exampleId, Sentence = { { "en", new RichString("example") } } }]
        };
        var sourceEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "source-entry" } },
            Senses = [movingSense, stayingSense]
        });
        var targetEntry = new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", "target-entry" } } };
        if (!targetEntryCreated)
            targetEntry = await Api.CreateEntry(targetEntry);

        var sourceEntryAfter = sourceEntry.Copy();
        var targetEntryAfter = targetEntry.Copy();
        var movedSense = sourceEntryAfter.Senses.Single(s => s.Id == movingSense.Id);
        sourceEntryAfter.Senses.Remove(movedSense);
        var movedExample = sourceEntryAfter.Senses.Single().ExampleSentences.Single();
        sourceEntryAfter.Senses.Single().ExampleSentences.Clear();
        movedSense.EntryId = targetEntry.Id;
        movedExample.SenseId = movedSense.Id;
        movedSense.ExampleSentences.Add(movedExample);
        targetEntryAfter.Senses.Add(movedSense);

        Entry[] before = targetEntryCreated ? [sourceEntry] : sourceEntryFirst ? [sourceEntry, targetEntry] : [targetEntry, sourceEntry];
        Entry[] after = sourceEntryFirst ? [sourceEntryAfter, targetEntryAfter] : [targetEntryAfter, sourceEntryAfter];
        await EntrySync.SyncFull(before, after, Api);

        var actualMovedSense = await Api.GetSense(targetEntry.Id, movingSense.Id);
        actualMovedSense.Should().NotBeNull();
        actualMovedSense.ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);
        var actualSourceEntry = await Api.GetEntry(sourceEntry.Id);
        actualSourceEntry.Should().NotBeNull();
        actualSourceEntry.Senses.Select(s => s.Id).Should().Equal(stayingSense.Id);
        actualSourceEntry.Senses[0].ExampleSentences.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanSyncExampleSentenceMovedOutOfSenseThatMovedToDifferentEntry(bool sourceEntryFirst)
    {
        var exampleId = Guid.NewGuid();
        var movingSense = new Sense
        {
            Id = Guid.NewGuid(),
            Gloss = { { "en", "moving" } },
            ExampleSentences = [new ExampleSentence { Id = exampleId, Sentence = { { "en", new RichString("example") } } }]
        };
        var stayingSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "staying" } } };
        var sourceEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "source-entry" } },
            Senses = [movingSense, stayingSense]
        });
        var targetEntry = await Api.CreateEntry(new() { Id = Guid.NewGuid(), LexemeForm = { { "en", "target-entry" } } });

        // the sense leaves for another entry, but its example stays behind in a sibling sense
        var sourceEntryAfter = sourceEntry.Copy();
        var targetEntryAfter = targetEntry.Copy();
        var movedSense = sourceEntryAfter.Senses.Single(s => s.Id == movingSense.Id);
        sourceEntryAfter.Senses.Remove(movedSense);
        var movedExample = movedSense.ExampleSentences.Single();
        movedSense.ExampleSentences.Clear();
        movedSense.EntryId = targetEntry.Id;
        movedExample.SenseId = stayingSense.Id;
        sourceEntryAfter.Senses.Single().ExampleSentences.Add(movedExample);
        targetEntryAfter.Senses.Add(movedSense);

        Entry[] before = sourceEntryFirst ? [sourceEntry, targetEntry] : [targetEntry, sourceEntry];
        Entry[] after = sourceEntryFirst ? [sourceEntryAfter, targetEntryAfter] : [targetEntryAfter, sourceEntryAfter];
        await EntrySync.SyncFull(before, after, Api);

        var actualMovedSense = await Api.GetSense(targetEntry.Id, movingSense.Id);
        actualMovedSense.Should().NotBeNull();
        actualMovedSense.ExampleSentences.Should().BeEmpty();
        var actualStayingSense = await Api.GetSense(sourceEntry.Id, stayingSense.Id);
        actualStayingSense.Should().NotBeNull();
        actualStayingSense.ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanSyncExampleSentenceMovedIntoSenseMovedOutOfDeletedEntry(bool deletedEntryFirst)
    {
        // FLEx "merge entry": the survivor takes a sense, and an example of a sense that dies with the entry is rescued into it
        var exampleId = Guid.NewGuid();
        var rescuedSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "rescued" } } };
        var dyingSense = new Sense
        {
            Id = Guid.NewGuid(),
            Gloss = { { "en", "dying" } },
            ExampleSentences = [new ExampleSentence { Id = exampleId, Sentence = { { "en", new RichString("example") } } }]
        };
        var deletedEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "deleted-entry" } },
            Senses = [rescuedSense, dyingSense]
        });
        var survivingEntry = await Api.CreateEntry(new() { Id = Guid.NewGuid(), LexemeForm = { { "en", "surviving-entry" } } });

        var survivingEntryAfter = survivingEntry.Copy();
        var movedSense = rescuedSense.Copy();
        movedSense.EntryId = survivingEntry.Id;
        var movedExample = dyingSense.ExampleSentences[0].Copy();
        movedExample.SenseId = movedSense.Id;
        movedSense.ExampleSentences.Add(movedExample);
        survivingEntryAfter.Senses.Add(movedSense);

        Entry[] before = deletedEntryFirst ? [deletedEntry, survivingEntry] : [survivingEntry, deletedEntry];
        await EntrySync.SyncFull(before, [survivingEntryAfter], Api);

        (await Api.GetEntry(deletedEntry.Id)).Should().BeNull();
        var actualSurvivingEntry = await Api.GetEntry(survivingEntry.Id);
        actualSurvivingEntry.Should().NotBeNull();
        actualSurvivingEntry.Senses.Select(s => s.Id).Should().Equal(rescuedSense.Id);
        actualSurvivingEntry.Senses[0].ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);
    }

    [Fact]
    public async Task CanSyncSensesSwappedBetweenEntries()
    {
        var senseA = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "a" } } };
        var senseB = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "b" } } };
        var entryA = await Api.CreateEntry(new() { Id = Guid.NewGuid(), LexemeForm = { { "en", "entry-a" } }, Senses = [senseA] });
        var entryB = await Api.CreateEntry(new() { Id = Guid.NewGuid(), LexemeForm = { { "en", "entry-b" } }, Senses = [senseB] });

        var entryAAfter = entryA.Copy();
        var entryBAfter = entryB.Copy();
        (entryAAfter.Senses, entryBAfter.Senses) = (entryBAfter.Senses, entryAAfter.Senses);
        entryAAfter.Senses[0].EntryId = entryA.Id;
        entryBAfter.Senses[0].EntryId = entryB.Id;

        await EntrySync.SyncFull([entryA, entryB], [entryAAfter, entryBAfter], Api);

        (await Api.GetEntry(entryA.Id))!.Senses.Select(s => s.Id).Should().Equal(senseB.Id);
        (await Api.GetEntry(entryB.Id))!.Senses.Select(s => s.Id).Should().Equal(senseA.Id);
    }

    [Fact]
    public async Task CanSyncExampleSentencesSwappedBetweenSenses()
    {
        var exampleA = new ExampleSentence { Id = Guid.NewGuid(), Sentence = { { "en", new RichString("a") } } };
        var exampleB = new ExampleSentence { Id = Guid.NewGuid(), Sentence = { { "en", new RichString("b") } } };
        var senseA = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "a" } }, ExampleSentences = [exampleA] };
        var senseB = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "b" } }, ExampleSentences = [exampleB] };
        var entry = await Api.CreateEntry(new() { Id = Guid.NewGuid(), LexemeForm = { { "en", "entry" } }, Senses = [senseA, senseB] });

        var after = entry.Copy();
        var afterSenseA = after.Senses.Single(s => s.Id == senseA.Id);
        var afterSenseB = after.Senses.Single(s => s.Id == senseB.Id);
        (afterSenseA.ExampleSentences, afterSenseB.ExampleSentences) = (afterSenseB.ExampleSentences, afterSenseA.ExampleSentences);
        afterSenseA.ExampleSentences[0].SenseId = senseA.Id;
        afterSenseB.ExampleSentences[0].SenseId = senseB.Id;

        await EntrySync.SyncFull(entry, after, Api);

        (await Api.GetSense(entry.Id, senseA.Id))!.ExampleSentences.Select(e => e.Id).Should().Equal(exampleB.Id);
        (await Api.GetSense(entry.Id, senseB.Id))!.ExampleSentences.Select(e => e.Id).Should().Equal(exampleA.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanSyncExampleSentenceMovedFromDeletedSenseToCreatedSense(bool deletedSenseFirst)
    {
        var exampleId = Guid.NewGuid();
        var deletedSense = new Sense
        {
            Id = Guid.NewGuid(),
            Gloss = { { "en", "deleted" } },
            ExampleSentences = [new ExampleSentence { Id = exampleId, Sentence = { { "en", new RichString("example") } } }]
        };
        var otherSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "other" } } };
        var entry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "entry" } },
            Senses = deletedSenseFirst ? [deletedSense, otherSense] : [otherSense, deletedSense]
        });

        var after = entry.Copy();
        after.Senses.RemoveAll(s => s.Id == deletedSense.Id);
        var movedExample = deletedSense.ExampleSentences[0].Copy();
        var createdSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "created" } }, ExampleSentences = [movedExample] };
        movedExample.SenseId = createdSense.Id;
        after.Senses.Add(createdSense);

        await EntrySync.SyncFull(entry, after, Api);

        var actual = await Api.GetEntry(entry.Id);
        actual.Should().NotBeNull();
        actual.Senses.Select(s => s.Id).Should().Equal(otherSense.Id, createdSense.Id);
        actual.Senses[1].ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);
    }

    [Fact]
    public async Task CanSyncExampleSentenceWithTranslationMovedToCreatedSense()
    {
        var exampleId = Guid.NewGuid();
        var translationId = Guid.NewGuid();
        var entry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "entry" } },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = { { "en", "source" } },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Id = exampleId,
                            Sentence = { { "en", new RichString("example") } },
                            Translations = [new Translation { Id = translationId, Text = { { "en", new RichString("translation") } } }]
                        }
                    ]
                }
            ]
        });

        // the moved example brings its translation along; it must not be mistaken for its own move
        var after = entry.Copy();
        var movedExample = after.Senses[0].ExampleSentences[0];
        after.Senses[0].ExampleSentences.Clear();
        var createdSense = new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "created target" } } };
        movedExample.SenseId = createdSense.Id;
        createdSense.ExampleSentences.Add(movedExample);
        after.Senses.Add(createdSense);

        await EntrySync.SyncFull(entry, after, Api);

        var actualCreatedSense = await Api.GetSense(entry.Id, createdSense.Id);
        actualCreatedSense.Should().NotBeNull();
        actualCreatedSense.ExampleSentences.Select(e => e.Id).Should().Equal(exampleId);
        actualCreatedSense.ExampleSentences[0].Translations.Should().ContainSingle()
            .Which.Id.Should().Be(translationId);
    }

    // Reparenting Pictures and translations MIGHT be supported in FieldWorks, but we don't support it.
    // So, we detect and throw for those early.

    [Fact]
    public async Task SyncThrows_PictureMovedToDifferentSense()
    {
        var picture = new Picture
        {
            Id = Guid.NewGuid(),
            MediaUri = new MediaUri(Guid.NewGuid(), "test"),
            Caption = new RichMultiString { { "en", new RichString("caption") } }
        };
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "entry" } },
            Senses =
            [
                new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "source" } }, Pictures = [picture] },
                new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "target" } } }
            ]
        };

        var after = entry.Copy();
        var movedPicture = after.Senses[0].Pictures[0];
        after.Senses[0].Pictures.Clear();
        after.Senses[1].Pictures.Add(movedPicture);

        var act = () => EntrySync.SyncFull(entry, after, Api);
        await act.Should().ThrowAsync<MoveNotSupportedException>().WithMessage($"*{picture.Id}*");
    }

    [Fact]
    public async Task SyncThrows_PictureMovedIntoCreatedSense()
    {
        var picture = new Picture
        {
            Id = Guid.NewGuid(),
            MediaUri = new MediaUri(Guid.NewGuid(), "test"),
            Caption = new RichMultiString { { "en", new RichString("caption") } }
        };
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "entry" } },
            Senses = [new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "source" } }, Pictures = [picture] }]
        };

        var after = entry.Copy();
        var movedPicture = after.Senses[0].Pictures[0];
        after.Senses[0].Pictures.Clear();
        after.Senses.Add(new Sense { Id = Guid.NewGuid(), Gloss = { { "en", "created target" } }, Pictures = [movedPicture] });

        var act = () => EntrySync.SyncFull(entry, after, Api);
        await act.Should().ThrowAsync<MoveNotSupportedException>().WithMessage($"*{movedPicture.Id}*");
    }

    [Fact]
    public async Task SyncThrows_TranslationMovedToDifferentExampleSentence_AndWritesNothing()
    {
        var translationId = Guid.NewGuid();
        var example1Id = Guid.NewGuid();
        var example2Id = Guid.NewGuid();
        var sense = new Sense
        {
            Id = Guid.NewGuid(),
            Gloss = { { "en", "gloss" } },
            ExampleSentences =
            [
                new ExampleSentence
                {
                    Id = example1Id,
                    Sentence = { { "en", new RichString("one") } },
                    Translations = [new Translation { Id = translationId, Text = { { "en", new RichString("translation") } } }]
                },
                new ExampleSentence { Id = example2Id, Sentence = { { "en", new RichString("two") } } }
            ]
        };
        var entry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "entry" } },
            Senses = [sense]
        });

        var after = entry.Copy();
        var afterExample1 = after.Senses[0].ExampleSentences.Single(e => e.Id == example1Id);
        var movedTranslation = afterExample1.Translations.Single(t => t.Id == translationId);
        afterExample1.Translations.Clear();
        after.Senses[0].ExampleSentences.Single(e => e.Id == example2Id).Translations.Add(movedTranslation);
        // an edit that would be applied if the sync got past the move check
        after.LexemeForm["en"] = "edited";

        var act = () => EntrySync.SyncFull(entry, after, Api);
        await act.Should().ThrowAsync<MoveNotSupportedException>().WithMessage($"*{translationId}*");

        // all-or-nothing: nothing was written, the translation is still on its original example
        var actualEntry = await Api.GetEntry(entry.Id);
        actualEntry.Should().NotBeNull();
        actualEntry.LexemeForm["en"].Should().Be("entry");
        var actualExample1 = await Api.GetExampleSentence(entry.Id, sense.Id, example1Id);
        actualExample1!.Translations.Select(t => t.Id).Should().Equal(translationId);
        (await Api.GetExampleSentence(entry.Id, sense.Id, example2Id))!.Translations.Should().BeEmpty();
    }

    [Theory]
    // Old component still exists in `after`, new component already exists in `before`.
    [InlineData("complex-form,old-component,new-component", false, false)]
    [InlineData("complex-form,new-component,old-component", false, false)]
    [InlineData("old-component,complex-form,new-component", false, false)]
    [InlineData("old-component,new-component,complex-form", false, false)]
    [InlineData("new-component,complex-form,old-component", false, false)]
    [InlineData("new-component,old-component,complex-form", false, false)]
    // Old component is removed entirely in `after` (e.g. the now-empty entry was deleted in FLEx).
    [InlineData("complex-form,old-component,new-component", true, false)]
    [InlineData("complex-form,new-component,old-component", true, false)]
    [InlineData("old-component,complex-form,new-component", true, false)]
    [InlineData("old-component,new-component,complex-form", true, false)]
    [InlineData("new-component,complex-form,old-component", true, false)]
    [InlineData("new-component,old-component,complex-form", true, false)]
    // New component is created in this sync, so the moved sense lands on an entry that goes through AddAndGet instead of Replace.
    [InlineData("complex-form,old-component,new-component", false, true)]
    [InlineData("complex-form,new-component,old-component", false, true)]
    [InlineData("old-component,complex-form,new-component", false, true)]
    [InlineData("old-component,new-component,complex-form", false, true)]
    [InlineData("new-component,complex-form,old-component", false, true)]
    [InlineData("new-component,old-component,complex-form", false, true)]
    // Both: old component deleted AND new component created in the same sync.
    [InlineData("complex-form,old-component,new-component", true, true)]
    [InlineData("complex-form,new-component,old-component", true, true)]
    [InlineData("old-component,complex-form,new-component", true, true)]
    [InlineData("old-component,new-component,complex-form", true, true)]
    [InlineData("new-component,complex-form,old-component", true, true)]
    [InlineData("new-component,old-component,complex-form", true, true)]
    public async Task CanSyncComponentWhenSenseMovesToDifferentEntry(string entryOrderString, bool oldComponentDeleted, bool newComponentCreated)
    {
        var entryOrder = entryOrderString.Split(",", StringSplitOptions.TrimEntries);

        var senseId = Guid.NewGuid();
        var oldComponentEntry = await Api.CreateEntry(new() { LexemeForm = { { "en", "old-component" } }, Senses = [new() { Id = senseId }] });
        var oldComponentEntryAfter = oldComponentEntry.Copy();
        oldComponentEntryAfter.Senses.Clear(); // sense is moved from here

        var newComponentEntry = new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", "new-component" } } };
        if (!newComponentCreated)
            newComponentEntry = await Api.CreateEntry(newComponentEntry);

        var newComponentEntryAfter = newComponentEntry.Copy();
        newComponentEntryAfter.Senses.Add(new Sense() { Id = senseId }); // sense is moved to here

        var complexForm = new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "complex form" } },
        };
        complexForm.Components.Add(ComplexFormComponent.FromEntries(complexForm, oldComponentEntry, senseId));
        complexForm = await Api.CreateEntry(complexForm);

        var complexFormAfter = complexForm.Copy();
        complexFormAfter.Components =
        [
            ComplexFormComponent.FromEntries(complexForm, newComponentEntry, senseId)
        ];

        var before = entryOrder
            .Where(name => !(newComponentCreated && name == "new-component"))
            .Select(name =>
            {
                return name switch
                {
                    "complex-form" => complexForm,
                    "old-component" => oldComponentEntry,
                    "new-component" => newComponentEntry,
                    _ => throw new InvalidOperationException("Unknown entry name")
                };
            }).ToArray();
        var after = entryOrder
            .Where(name => !(oldComponentDeleted && name == "old-component"))
            .Select(name =>
            {
                return name switch
                {
                    "complex-form" => complexFormAfter,
                    "old-component" => oldComponentEntryAfter,
                    "new-component" => newComponentEntryAfter,
                    _ => throw new InvalidOperationException("Unknown entry name")
                };
            }).ToArray();

        await EntrySync.SyncFull(before, after, Api);

        var actualComplexForm = await Api.GetEntry(complexForm.Id);
        actualComplexForm.Should().NotBeNull();
        actualComplexForm.Components.Should().HaveCount(1);
        actualComplexForm.Components[0].ComponentEntryId.Should().Be(newComponentEntry.Id);
        actualComplexForm.Components[0].ComponentSenseId.Should().Be(senseId);

        var actualOldComponentEntry = await Api.GetEntry(oldComponentEntry.Id);
        if (oldComponentDeleted)
        {
            actualOldComponentEntry.Should().BeNull();
        }
        else
        {
            actualOldComponentEntry.Should().NotBeNull();
            actualOldComponentEntry.Senses.Should().BeEmpty();
            actualOldComponentEntry.ComplexForms.Should().BeEmpty();
        }

        var actualNewComponentEntry = await Api.GetEntry(newComponentEntry.Id);
        actualNewComponentEntry.Should().NotBeNull();
        actualNewComponentEntry.Senses.Should().HaveCount(1);
        actualNewComponentEntry.Senses[0].Id.Should().Be(senseId);
        actualNewComponentEntry.ComplexForms.Should().HaveCount(1);
        actualNewComponentEntry.ComplexForms[0].ComplexFormEntryId.Should().Be(complexForm.Id);
    }
}
