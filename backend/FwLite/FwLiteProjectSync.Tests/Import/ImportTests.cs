using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests.Import;

public class ImportTests : IClassFixture<SyncFixture>
{
    private readonly SyncFixture _fixture;
    private MiniLcmImport ImportService => _fixture.Services.GetRequiredService<MiniLcmImport>();

    public ImportTests(SyncFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ImportsANewlyCreatedEntry()
    {
        var entryId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var exampleId = Guid.NewGuid();
        var entry = await _fixture.FwDataApi.CreateEntry(new Entry()
        {
            Id = entryId,
            LexemeForm = { ["en"] = "Test Entry" },
            Senses =
            [
                new Sense()
                {
                    Id = senseId,
                    EntryId = entryId,
                    Gloss = { ["en"] = "Test Sense" },
                    Definition = { ["en"] = new RichString("This is a test definition.", "en") },
                    Order = 1,
                    ExampleSentences =
                    [
                        new ExampleSentence()
                        {
                            Id = exampleId,
                            SenseId = senseId,
                            Order = 1,
                            Sentence = { ["en"] = new RichString("This is a test example sentence.", "en") },
                            Translations =
                            [
                                new()
                                {
                                    Text =
                                    {
                                        ["en"] = new RichString("Ceci est une phrase d'exemple de test.", "en")
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        });
        await ImportService.ImportProject(_fixture.CrdtApi, _fixture.FwDataApi, 1);
        var importedEntry = await _fixture.CrdtApi.GetEntry(entry.Id);
        importedEntry.Should().BeEquivalentTo(entry, c => c.For(e => e.Senses).Exclude(s => s.Order).For(e => e.Senses).For(s => s.ExampleSentences).Exclude(e => e.Order));
    }

    [Fact]
    public async Task ImportsANewlyCreatedWritingSystem()
    {
        var ws = new WritingSystem
        {
            Id = Guid.NewGuid(),
            Name = "fr",
            WsId = "fr",
            Abbreviation = "Fr",
            Font = "Charis SIL",
            Type = WritingSystemType.Vernacular,
            Exemplars = WritingSystem.LatinExemplars,
            Order = 1.0
        };
        await _fixture.FwDataApi.CreateWritingSystem(ws);
        await ImportService.ImportProject(_fixture.CrdtApi, _fixture.FwDataApi, 1);
        var importedWritingSystems = await _fixture.CrdtApi.GetWritingSystems();
        var importedWritingSystem = importedWritingSystems.Vernacular.SingleOrDefault(vws => vws.WsId == ws.WsId);
        importedWritingSystem.Should().BeEquivalentTo(ws, c => c
        .Excluding(w => w.Id)//Id is not mapped from FW to Crdt as FW does not have an ID for writing systems
        .Excluding(w => w.Order)
        .Excluding(w => w.Exemplars));
    }

    [Fact]
    public async Task ImportsANewlyCreatedPartOfSpeech()
    {
        var pos = new PartOfSpeech { Id = Guid.NewGuid(), Name = { ["en"] = "Test POS" } };
        await _fixture.FwDataApi.CreatePartOfSpeech(pos);
        await ImportService.ImportProject(_fixture.CrdtApi, _fixture.FwDataApi, 1);
        var importedPos = await _fixture.CrdtApi.GetPartOfSpeech(pos.Id);
        importedPos.Should().BeEquivalentTo(pos);
    }

    [Fact]
    public async Task ImportsANewlyCreatedComplexFormType()
    {
        var cft = new ComplexFormType { Id = Guid.NewGuid(), Name = new(){ ["en"] = "Test CFT" } };
        await _fixture.FwDataApi.CreateComplexFormType(cft);
        await ImportService.ImportProject(_fixture.CrdtApi, _fixture.FwDataApi, 1);
        var importedCft = await _fixture.CrdtApi.GetComplexFormType(cft.Id);
        importedCft.Should().BeEquivalentTo(cft);
    }

    [Fact]
    public async Task ImportsANewlyCreatedSemanticDomain()
    {
        var sd = new SemanticDomain { Id = Guid.NewGuid(), Name = { ["en"] = "Test SD" }, Code = "TSD"};
        await _fixture.FwDataApi.CreateSemanticDomain(sd);
        await ImportService.ImportProject(_fixture.CrdtApi, _fixture.FwDataApi, 1);
        var importedSd = await _fixture.CrdtApi.GetSemanticDomain(sd.Id);
        importedSd.Should().BeEquivalentTo(sd);
    }
}
