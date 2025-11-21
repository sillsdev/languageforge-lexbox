using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class PartOfSpeechTests(ProjectLoaderFixture fixture) : PartOfSpeechTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("part-of-speech-test", "en", "en"));
    }

    [Fact]
    public async Task SetPartOfSpeech_WithNullMorphoSyntaxAnalysisRA_ToValidPos()
    {
        // Arrange
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString { { "en", "test" } },
            Senses =
            [
                new Sense { Gloss = new MultiString { { "en", "test gloss" } } }
            ]
        });

        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwApi.EntriesRepository.GetObject(entry.Id);
        var lexSense = lexEntry.SensesOS.First();
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Set MorphoSyntaxAnalysisRA to null",
            "Restore MorphoSyntaxAnalysisRA",
            fwApi.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                lexSense.MorphoSyntaxAnalysisRA = null;
                lexSense.MorphoSyntaxAnalysisRA.Should().BeNull();
            });

        // Act
        var pos = await Api.GetPartsOfSpeech().FirstAsync();
        await Api.SetSensePartOfSpeech(lexSense.Guid, pos.Id);

        // Assert
        var retrievedEntry = await Api.GetSense(entry.Id, lexSense.Guid);
        retrievedEntry.Should().NotBeNull();
        retrievedEntry!.PartOfSpeechId.Should().Be(pos.Id);
    }

    [Fact]
    public async Task SetPartOfSpeech_WithNullMorphoSyntaxAnalysisRA_ToNull()
    {
        // Arrange
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString { { "en", "test" } },
            Senses =
            [
                new Sense { Gloss = new MultiString { { "en", "test gloss" } } }
            ]
        });

        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwApi.EntriesRepository.GetObject(entry.Id);
        var lexSense = lexEntry.SensesOS.First();
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Set MorphoSyntaxAnalysisRA to null",
            "Restore MorphoSyntaxAnalysisRA",
            fwApi.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                lexSense.MorphoSyntaxAnalysisRA = null;
                lexSense.MorphoSyntaxAnalysisRA.Should().BeNull();
            });

        // Act
        await Api.SetSensePartOfSpeech(lexSense.Guid, null);

        // Assert
        var retrievedEntry = await Api.GetSense(entry.Id, lexSense.Guid);
        retrievedEntry.Should().NotBeNull();
        retrievedEntry!.PartOfSpeechId.Should().BeNull();
    }

    [Fact]
    public async Task GetPartsOfSpeech_DoesNotReturnReversalIndexPos()
    {
        // Arrange
        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var reversalIndexRepository = fwApi.Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
        var analysisWs = fwApi.Cache.DefaultAnalWs;

        var reversalPosGuid = Guid.NewGuid();
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Reversal Index POS",
            "Remove Reversal Index POS",
            fwApi.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var reversalIndex = reversalIndexRepository.FindOrCreateIndexForWs(analysisWs);
                var posFactory = fwApi.Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
                var reversalPos = posFactory.Create(reversalPosGuid, reversalIndex.PartsOfSpeechOA);
                reversalPos.Name.set_String(analysisWs, "Reversal Test POS");
            });

        // Act
        var partsOfSpeech = await Api.GetPartsOfSpeech().ToArrayAsync();

        // Assert
        partsOfSpeech.Should().NotContain(pos => pos.Id == reversalPosGuid);
    }
}
