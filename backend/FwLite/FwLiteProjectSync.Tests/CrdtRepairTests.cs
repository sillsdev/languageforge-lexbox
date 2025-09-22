using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

public class CrdtRepairTests : IClassFixture<SyncFixture>, IAsyncLifetime
{
    private readonly SyncFixture _fixture;

    public CrdtRepairTests(SyncFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.EnsureDefaultVernacularWritingSystemExistsInCrdt();
    }

    public async Task DisposeAsync()
    {
        await foreach (var entry in _fixture.FwDataApi.GetAllEntries())
        {
            await _fixture.FwDataApi.DeleteEntry(entry.Id);
        }
        foreach (var entry in await _fixture.CrdtApi.GetAllEntries().ToArrayAsync())
        {
            await _fixture.CrdtApi.DeleteEntry(entry.Id);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncMissingTranslationId_IntoSnapshotAndCrdt()
    {
        // arrange
        var fwdataEntry = await _fixture.FwDataApi.CreateEntry(new Entry()
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
        });
        var crdtEntry = fwdataEntry.Copy();
        crdtEntry.Senses.Single().ExampleSentences.Single().Translations.First().Id = Translation.MissingTranslationId;
        crdtEntry = await _fixture.CrdtApi.CreateEntry(crdtEntry);
        // make sure the constant Guid was actually persisted
        crdtEntry.Senses.Single().ExampleSentences.Single().Translations.First().Id.Should().Be(Translation.MissingTranslationId);
        crdtEntry.Should().NotBeEquivalentTo(fwdataEntry);
        var snapshotEntry = crdtEntry.Copy();

        // act
        await CrdtRepairs.SyncMissingTranslationIds([snapshotEntry], _fixture.FwDataApi, _fixture.CrdtApi, dryRun: false);

        // assert
        var updatedCrdtEntry = await _fixture.CrdtApi.GetEntry(crdtEntry.Id);
        updatedCrdtEntry.Should().BeEquivalentTo(fwdataEntry, SyncTests.SyncExclusions);
        updatedCrdtEntry.Should().BeEquivalentTo(snapshotEntry);
        updatedCrdtEntry.Senses.Single().ExampleSentences.Single().Translations.First().Id.Should().NotBe(Translation.MissingTranslationId);
    }
}
