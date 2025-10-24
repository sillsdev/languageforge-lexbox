using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class UpdateEntryTests(ProjectLoaderFixture fixture) : UpdateEntryTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("update-entry-test", "en", "en"));
    }

    protected override bool ApiUsesImplicitOrdering => true;

    [Fact]
    public async Task UpdateEntry_WithNullLexemeFormOA_CreatesNewLexemeForm()
    {
        // Arrange
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString { { "en", "test" } }
        });

        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwApi.EntriesRepository.GetObject(entry.Id);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Set LexemeFormOA to null",
            "Restore LexemeFormOA",
            fwApi.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                lexEntry.LexemeFormOA = null;
            });

        // Act
        var updatedEntry = await Api.UpdateEntry(entry, entry with
        {
            LexemeForm = new MultiString { { "en", "updated test" } }
        });

        // Assert
        updatedEntry.LexemeForm.Should().NotBeNull();
        updatedEntry.LexemeForm["en"].Should().Be("updated test");
    }

    [Fact]
    public async Task UpdateEntry_CanUpdateExampleSentenceTranslations_WhenNoTranslationsExists()
    {
        // Arrange
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = { { "en", "test" } },
            Note = { { "en", new RichString("this is a test note") } },
            CitationForm = { { "en", "test" } },
            LiteralMeaning = { { "en", new RichString("test") } },
            Senses =
            [
                new Sense
                {
                    Gloss = { { "en", "test" } },
                    Definition = { { "en", new RichString("test") } },
                    ExampleSentences =
                    [
                        new()
                        {
                            Sentence = { { "en", new RichString("testing is good") } },
                            Translations = [],
                        }
                    ]
                }
            ]
        });

        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwApi.EntriesRepository.GetObject(entry.Id);
        lexEntry.SensesOS[0].ExamplesOS[0].TranslationsOC.Should().BeEmpty();

        var before = entry.Copy();
        var exampleSentence = entry.Senses[0].ExampleSentences[0];
        exampleSentence.Translations = [new() { Text = { { "en", new RichString("updated") } } }];

        // Act
        var updatedEntry = await Api.UpdateEntry(before, entry);
        var updatedExampleSentence = updatedEntry.Senses[0].ExampleSentences[0];

        // Assert
        var translation = updatedExampleSentence.Translations.Should().ContainSingle().Subject;
        translation.Text["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
        updatedEntry.Should().BeEquivalentTo(entry, options => options);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateEntry_MoveSenseBetweenSubsenses(bool useNext)
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var senseId1 = Guid.NewGuid();
        var senseId2 = Guid.NewGuid();
        var senseId1_1 = Guid.NewGuid();
        var senseId1_2 = Guid.NewGuid();
        await Api.CreateEntry(new Entry
        {
            Id = entryId,
            LexemeForm = new MultiString { { "en", "entry" } },
            Senses =
            [
                new Sense { Id = senseId1, Gloss = new MultiString { { "en", "sense 1" } }},
                new Sense { Id = senseId2, Gloss = new MultiString { { "en", "sense 2" } }},
            ]
        });

        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwApi.EntriesRepository.GetObject(entryId);
        var senseFactory = fwApi.Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Add subsenses to sense 1",
            "Remove subsenses from sense 1",
            fwApi.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var sense1 = lexEntry.SensesOS[0];
                sense1.SensesOS.Add(senseFactory.Create(senseId1_1));
                sense1.SensesOS.Add(senseFactory.Create(senseId1_2));
            });

        var entry = await Api.GetEntry(entryId);
        entry.Should().NotBeNull();
        entry.Senses.Select(s => s.Id).Should()
            .BeEquivalentTo([senseId1, senseId1_1, senseId1_2, senseId2],
            options => options.WithStrictOrdering());

        // Act
        var between = useNext
            ? new BetweenPosition(null, senseId1_2)
            : new BetweenPosition(senseId1_1, null);
        await Api.MoveSense(entryId, senseId2, between);

        // Assert
        var updatedEntry = await Api.GetEntry(entryId);
        updatedEntry.Should().NotBeNull();
        updatedEntry.Senses.Select(s => s.Id).Should()
            .BeEquivalentTo([senseId1, senseId1_1, senseId2, senseId1_2],
            options => options.WithStrictOrdering());

        lexEntry = fwApi.EntriesRepository.GetObject(entryId);
        lexEntry.SensesOS.Select(s => s.Id.Guid).Should()
            .BeEquivalentTo([senseId1]);
        lexEntry.SensesOS[0].SensesOS.Select(s => s.Id.Guid).Should()
            .BeEquivalentTo([senseId1_1, senseId2, senseId1_2]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateEntry_MoveSenseAfterSubsenses(bool useNext)
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var senseId1 = Guid.NewGuid();
        var senseId2 = Guid.NewGuid();
        var senseId3 = Guid.NewGuid();
        var senseId1_1 = Guid.NewGuid();
        var senseId1_2 = Guid.NewGuid();
        await Api.CreateEntry(new Entry
        {
            Id = entryId,
            LexemeForm = new MultiString { { "en", "entry" } },
            Senses =
            [
                new Sense { Id = senseId1, Gloss = new MultiString { { "en", "sense 1" } }},
                new Sense { Id = senseId2, Gloss = new MultiString { { "en", "sense 2" } }},
                new Sense { Id = senseId3, Gloss = new MultiString { { "en", "sense 3" } }},
            ]
        });

        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwApi.EntriesRepository.GetObject(entryId);
        var senseFactory = fwApi.Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Add subsenses to sense 1",
            "Remove subsenses from sense 1",
            fwApi.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var sense1 = lexEntry.SensesOS[0];
                sense1.SensesOS.Add(senseFactory.Create(senseId1_1));
                sense1.SensesOS.Add(senseFactory.Create(senseId1_2));
            });

        var entry = await Api.GetEntry(entryId);
        entry.Should().NotBeNull();
        entry.Senses.Select(s => s.Id).Should()
            .BeEquivalentTo([senseId1, senseId1_1, senseId1_2, senseId2, senseId3],
            options => options.WithStrictOrdering());

        // Act
        var between = useNext
            ? new BetweenPosition(null, senseId2)
            : new BetweenPosition(senseId1_2, null);
        await Api.MoveSense(entryId, senseId3, between);

        // Assert
        var updatedEntry = await Api.GetEntry(entryId);
        updatedEntry.Should().NotBeNull();
        updatedEntry.Senses.Select(s => s.Id).Should()
            .BeEquivalentTo([senseId1, senseId1_1, senseId1_2, senseId3, senseId2],
            options => options.WithStrictOrdering());

        lexEntry = fwApi.EntriesRepository.GetObject(entryId);
        lexEntry.SensesOS.Select(s => s.Id.Guid).Should()
            .BeEquivalentTo([senseId1, senseId3, senseId2],
            options => options.WithStrictOrdering());
        lexEntry.SensesOS[0].SensesOS.Select(s => s.Id.Guid).Should()
            .BeEquivalentTo([senseId1_1, senseId1_2],
            options => options.WithStrictOrdering());
    }
}
