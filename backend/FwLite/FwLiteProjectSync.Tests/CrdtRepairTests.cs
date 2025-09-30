using FwDataMiniLcmBridge.Api;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

#pragma warning disable CS0618 // Type or member is obsolete
[Trait("Category", "Integration")]
public class CrdtRepairTests(SyncFixture fixture) : IClassFixture<SyncFixture>, IAsyncLifetime
{
    private readonly SyncFixture _fixture = fixture;
    private CrdtMiniLcmApi CrdtApi => _fixture.CrdtApi;
    private FwDataMiniLcmApi FwDataApi => _fixture.FwDataApi;
    private CrdtFwdataProjectSyncService SyncService => _fixture.SyncService;
    private static Entry TestEntry => new()
    {
        Senses =
        [
            new()
            {
                ExampleSentences =
                [
                    new()
                    {
                        Translations =
                        [
                            new()
                            {
                                Text = { { "en", new RichString("translation") } }
                            }
                        ]
                    }
                ]
            }
        ]
    };

    public async Task InitializeAsync()
    {
        await SyncService.Sync(CrdtApi, FwDataApi);
    }

    public async Task DisposeAsync()
    {
        await foreach (var entry in FwDataApi.GetAllEntries())
        {
            await FwDataApi.DeleteEntry(entry.Id);
        }
        foreach (var entry in await CrdtApi.GetAllEntries().ToArrayAsync())
        {
            await CrdtApi.DeleteEntry(entry.Id);
        }
        _fixture.DeleteSyncSnapshot();
    }

    [Fact]
    public async Task CrdtEntryMissingTranslationId_NoChanges_SyncMissingTranslationIds()
    {
        // arrange
        var (fwEntry, crdtEntry, snapshotEntry) = await CreateSyncedEntryMissingTranslationId();
        var fwTranslationId = fwEntry.SingleTranslation().Id;

        // act
        await CrdtRepairs.SyncMissingTranslationIds([snapshotEntry], FwDataApi, CrdtApi);

        // assert - the fwdata ID is now used everywhere
        var updatedFwEntry = await FwDataApi.GetEntry(crdtEntry.Id);
        var updatedCrdtEntry = await CrdtApi.GetEntry(crdtEntry.Id);
        updatedFwEntry.Should().NotBeNull();
        updatedFwEntry.SingleTranslation().Id.Should().Be(fwTranslationId);
        updatedCrdtEntry.Should().NotBeNull();
        updatedCrdtEntry.SingleTranslation().Id.Should().Be(fwTranslationId);
        // And the snapshot was updated to mirror the crdt change, so the sync makes sense
        snapshotEntry.SingleTranslation().Id.Should().Be(fwTranslationId);
    }

    [Fact]
    public async Task CrdtEntryMissingTranslationId_NoChanges_FullSync()
    {
        // arrange
        var (fwEntry, crdtEntry, _) = await CreateSyncedEntryMissingTranslationId();
        var fwTranslationId = fwEntry.SingleTranslation().Id;

        // act
        await SyncService.Sync(CrdtApi, FwDataApi);

        // assert - the fwdata ID is now used everywhere
        var updatedFwEntry = await FwDataApi.GetEntry(crdtEntry.Id);
        var updatedCrdtEntry = await CrdtApi.GetEntry(crdtEntry.Id);
        updatedFwEntry.Should().NotBeNull();
        updatedFwEntry.SingleTranslation().Id.Should().Be(fwTranslationId);
        updatedCrdtEntry.Should().NotBeNull();
        updatedCrdtEntry.SingleTranslation().Id.Should().Be(fwTranslationId);
        updatedCrdtEntry.Should().BeEquivalentTo(updatedFwEntry, SyncTests.SyncExclusions);
        // And the snapshot was updated to mirror the crdt change
        var updatedSnapshotEntry = (await GetSnapshotEntries()).Single();
        updatedSnapshotEntry.SingleTranslation().Id.Should().Be(fwTranslationId);
    }

    [Fact]
    public async Task CrdtEntryMissingTranslationId_MultipleFwTranslations_FullSync()
    {
        // arrange
        var (fwEntry, crdtEntry, _) = await CreateSyncedEntryMissingTranslationId();
        var fwTranslationId = fwEntry.SingleTranslation().Id;
        await FwDataApi.AddTranslation(fwEntry.Id, fwEntry.Senses.Single().Id, fwEntry.SingleExampleSentence().Id, new Translation
        {
            Text = { { "en", new RichString("another translation") } }
        });
        fwEntry = await FwDataApi.GetEntry(fwEntry.Id);

        // act
        await SyncService.Sync(CrdtApi, FwDataApi);

        // assert - the fwdata ID is now used everywhere
        var updatedFwEntry = await FwDataApi.GetEntry(crdtEntry.Id);
        var updatedCrdtEntry = await CrdtApi.GetEntry(crdtEntry.Id);
        updatedFwEntry.Should().NotBeNull();
        updatedFwEntry.SingleExampleSentence().Translations.First().Id.Should().Be(fwTranslationId);
        updatedCrdtEntry.Should().NotBeNull();
        updatedCrdtEntry.SingleExampleSentence().Translations.First().Id.Should().Be(fwTranslationId);
        updatedCrdtEntry.Should().BeEquivalentTo(updatedFwEntry, SyncTests.SyncExclusions);
        updatedCrdtEntry.SingleExampleSentence().Translations.Count.Should().Be(2);
        var updatedSnapshotEntry = (await GetSnapshotEntries()).Single();
        updatedSnapshotEntry.Should().BeEquivalentTo(updatedCrdtEntry, SyncTests.SyncExclusions);
    }

    [Fact]
    public async Task CrdtEntryMissingTranslationId_FwTranslationRemoved_SyncMissingTranslationIds()
    {
        // arrange
        var (fwEntry, crdtEntry, snapshotEntry) = await CreateSyncedEntryMissingTranslationId();
        var entryId = fwEntry.Id;
        var senseId = fwEntry.Senses.Single().Id;
        var exampleSentenceId = fwEntry.SingleExampleSentence().Id;

        var fwExample = fwEntry.SingleExampleSentence();
        var fwTranslationId = fwExample.SingleTranslation().Id;
        var defaultFirstTranslationId = fwExample.DefaultFirstTranslationId;

        // act
        await FwDataApi.RemoveTranslation(entryId, senseId, exampleSentenceId, fwTranslationId);
        await CrdtRepairs.SyncMissingTranslationIds([snapshotEntry], FwDataApi, CrdtApi);

        // assert - the crdt ID was left untouched, which will result in its deletion
        var updatedCrdtEntry = await CrdtApi.GetEntry(crdtEntry.Id);
        updatedCrdtEntry.Should().NotBeNull();
        updatedCrdtEntry.SingleTranslation().Id.Should().Be(defaultFirstTranslationId);
    }

    [Fact]
    public async Task CrdtEntryMissingTranslationId_FwTranslationRemoved_FullSync()
    {
        // arrange
        var (fwEntry, crdtEntry, _) = await CreateSyncedEntryMissingTranslationId();
        var entryId = fwEntry.Id;
        var senseId = fwEntry.Senses.Single().Id;
        var exampleSentenceId = fwEntry.SingleExampleSentence().Id;
        var fwTranslationId = fwEntry.SingleTranslation().Id;

        // act
        await FwDataApi.RemoveTranslation(entryId, senseId, exampleSentenceId, fwTranslationId);
        var result = await SyncService.Sync(CrdtApi, FwDataApi);
        result.CrdtChanges.Should().Be(1, "the crdt translation was removed");

        // assert - the crdt translation was also removed
        var updatedCrdtEntry = await CrdtApi.GetEntry(crdtEntry.Id);
        updatedCrdtEntry!.SingleExampleSentence().Translations.Should().BeEmpty();
    }

    [Fact]
    public async Task CrdtEntryMissingTranslationId_CrdtTranslationRemoved_SyncMissingTranslationIds()
    {
        // arrange
        var (fwEntry, crdtEntry, snapshotEntry) = await CreateSyncedEntryMissingTranslationId();
        var entryId = fwEntry.Id;
        var senseId = fwEntry.Senses.Single().Id;
        var exampleSentenceId = fwEntry.SingleExampleSentence().Id;

        var fwExample = fwEntry.SingleExampleSentence();
        var fwTranslationId = fwExample.SingleTranslation().Id;

        // act
        await CrdtApi.RemoveTranslation(entryId, senseId, exampleSentenceId, fwExample.DefaultFirstTranslationId);
        await CrdtRepairs.SyncMissingTranslationIds([snapshotEntry], FwDataApi, CrdtApi);

        // assert - the fw translation ID was left untouched and will be deleted in the sync
        var updatedFwEntry = await FwDataApi.GetEntry(crdtEntry.Id);
        updatedFwEntry!.SingleTranslation().Id.Should().Be(fwTranslationId);
    }

    [Fact]
    public async Task CrdtEntryMissingTranslationId_CrdtTranslationRemoved_FullSync()
    {
        // arrange
        var (fwEntry, crdtEntry, _) = await CreateSyncedEntryMissingTranslationId();
        var entryId = fwEntry.Id;
        var senseId = fwEntry.Senses.Single().Id;
        var exampleSentenceId = fwEntry.SingleExampleSentence().Id;

        var fwExample = fwEntry.SingleExampleSentence();

        // act
        await CrdtApi.RemoveTranslation(entryId, senseId, exampleSentenceId, fwExample.DefaultFirstTranslationId);
        await SyncService.Sync(CrdtApi, FwDataApi);

        // assert - the fw translation was also removed
        var updatedFwEntry = await FwDataApi.GetEntry(crdtEntry.Id);
        updatedFwEntry!.SingleExampleSentence().Translations.Should().BeEmpty();
    }

    [Fact]
    public async Task CrdtEntryMissingTranslationId_NewCrdtEntry_SyncMissingTranslationIds()
    {
        // arrange
        var entry = TestEntry;
        entry.SingleTranslation().Id = Translation.MissingTranslationId;
        var crdtEntry = await CrdtApi.CreateEntry(entry);

        // act
        await CrdtRepairs.SyncMissingTranslationIds([], FwDataApi, CrdtApi);

        // assert - the default ID was left untouched and will be used in the new fwdata entry/translation
        var updatedCrdtEntry = await CrdtApi.GetEntry(crdtEntry.Id);
        updatedCrdtEntry.Should().NotBeNull();
        var exampleSentence = updatedCrdtEntry.SingleExampleSentence();
        updatedCrdtEntry.SingleTranslation().Id.Should().Be(exampleSentence.DefaultFirstTranslationId);
    }

    [Fact] // fails, because it's not currently possible to set an fw-translation's ID
    public async Task CrdtEntryMissingTranslationId_NewCrdtEntry_FullSync()
    {
        // arrange
        var entry = TestEntry;
        entry.SingleTranslation().Id = Translation.MissingTranslationId;
        var crdtEntry = await CrdtApi.CreateEntry(entry);

        // act
        await SyncService.Sync(CrdtApi, FwDataApi);

        // assert - the default ID was used in the new fw translation
        var fwEntry = await FwDataApi.GetEntry(crdtEntry.Id);
        fwEntry.Should().NotBeNull();
        var exampleSentence = crdtEntry.SingleExampleSentence();
        fwEntry.SingleTranslation().Id.Should().Be(exampleSentence.DefaultFirstTranslationId);
    }

    private async Task<(Entry FwEntry, Entry CrdtEntry, Entry SnapshotEntry)> CreateSyncedEntryMissingTranslationId()
    {
        // arrange
        var fwEntry = await FwDataApi.CreateEntry(TestEntry);
        var fwTranslationId = fwEntry.SingleTranslation().Id;
        var crdtEntry = fwEntry.Copy();
        crdtEntry.SingleTranslation().Id = Translation.MissingTranslationId;
        crdtEntry = await CrdtApi.CreateEntry(crdtEntry);
        var snapshot = await CrdtApi.TakeProjectSnapshot();
        var snapshotEntry = snapshot.Entries.Single(e => e.Id == crdtEntry.Id);
        snapshotEntry.SingleTranslation().Id = Translation.MissingTranslationId;
        await SyncService.SaveProjectSnapshot(FwDataApi.Project, snapshot);

        // assert
        // snapshot reflects the missing ID
        var snapshotTranslation = snapshotEntry.SingleTranslation();
        snapshotTranslation.Id.Should().Be(Translation.MissingTranslationId);
        // crdt mapped the missing ID to its example sentence's default
        var crdtExampleSentence = crdtEntry.SingleExampleSentence();
        var crdtTranslation = crdtExampleSentence.SingleTranslation();
        crdtTranslation.Id.Should().Be(crdtExampleSentence.DefaultFirstTranslationId);
        // fw entry has a real/valid ID
        var fwTranslation = fwEntry.SingleTranslation();
        fwTranslation.Id.Should().NotBe(crdtExampleSentence.DefaultFirstTranslationId);
        fwTranslation.Id.Should().NotBe(Translation.MissingTranslationId);
        fwTranslation.Id.Should().NotBe(Guid.Empty);

        // otherwise the entries are functionally identical
        crdtEntry.Should().BeEquivalentTo(fwEntry, options => SyncTests.SyncExclusions(options)
            .For(e => e.Senses).For(s => s.ExampleSentences).For(es => es.Translations).Exclude(t => t.Id));
        crdtEntry.Should().BeEquivalentTo(snapshotEntry, options => SyncTests.SyncExclusions(options)
            .For(e => e.Senses).For(s => s.ExampleSentences).For(es => es.Translations).Exclude(t => t.Id));
        return (fwEntry, crdtEntry, snapshotEntry);
    }

    private async Task<Entry[]> GetSnapshotEntries()
    {
        var snapshot = await CrdtFwdataProjectSyncService.GetProjectSnapshot(FwDataApi.Project);
        return snapshot?.Entries ?? [];
    }

}
#pragma warning restore CS0618 // Type or member is obsolete

static file class EntryHelpers
{
    public static ExampleSentence SingleExampleSentence(this Entry entry)
    {
        return entry.Senses.Single().ExampleSentences.Single();
    }

    public static Translation SingleTranslation(this Entry entry)
    {
        return entry.SingleExampleSentence().SingleTranslation();
    }

    public static Translation SingleTranslation(this ExampleSentence exampleSentence)
    {
        return exampleSentence.Translations.Single();
    }
}
