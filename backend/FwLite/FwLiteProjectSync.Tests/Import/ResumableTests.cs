using LcmCrdt.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using Moq;

namespace FwLiteProjectSync.Tests.Import;

public class ResumableTests : IAsyncLifetime
{
    private readonly MiniLcmApiFixture _fixture = new();
    [Fact]
    public async Task ImportProject_IsResumable_AcrossRandomFailures()
    {
        // Arrange
        var random = new Random(42); // deterministic!
        var expectedEntries = Enumerable.Range(1, 10)
            .Select(i => new Entry() { Id = Guid.NewGuid(), LexemeForm = { ["en"] = $"entry{i}" } }).ToList();
        var expectedPartsOfSpeech = Enumerable.Range(1, 10)
            .Select(i => new PartOfSpeech { Id = Guid.NewGuid(), Name = { ["en"] = $"pos{i}" } }).ToList();

        var mockFrom = new Mock<IMiniLcmApi>();
        IMiniLcmApi mockTo = new UnreliableApi(
            api: _fixture.Api,
            random
        );

        mockFrom.Setup(f => f.GetWritingSystems()).ReturnsAsync(new WritingSystems()
        {
            Vernacular = [new()
            {
                Id = Guid.NewGuid(),
                WsId = new() { Code = "en" },
                Type = WritingSystemType.Vernacular,
                Name = "English",
                Abbreviation = "EN",
                Font = "Arial",
            }],
            Analysis = [new()
            {
                Id = Guid.NewGuid(),
                WsId = new() { Code = "en" },
                Type = WritingSystemType.Analysis,
                Name = "English",
                Abbreviation = "EN",
                Font = "Arial",
            }]
        });
        mockFrom.Setup(f => f.GetPartsOfSpeech())
            .Returns(MockAsyncEnumerable(expectedPartsOfSpeech));
        mockFrom.Setup(f => f.GetEntries(It.IsAny<QueryOptions>()))
            .Returns(MockAsyncEnumerable(expectedEntries));
        mockFrom.Setup(f => f.GetPublications()).Returns(MockAsyncEnumerable([new Publication(){
                Id = Guid.NewGuid(),
                Name = { ["en"] = "Test Publication" },
            }]));
        mockFrom.Setup(f => f.GetComplexFormTypes())
            .Returns(MockAsyncEnumerable([new ComplexFormType()
            {
                Id = Guid.NewGuid(),
                Name = new(){ ["en"] = "Test Complex Form Type" }
            }]));
        mockFrom.Setup(f => f.GetSemanticDomains())
            .Returns(MockAsyncEnumerable([new SemanticDomain()
            {
                Id = Guid.NewGuid(),
                Name = new() { ["en"] = "Test Semantic Domain" },
                Code = "TSD"
            }]));

        var import = new MiniLcmImport(
            logger: NullLogger<MiniLcmImport>.Instance,
            fwDataFactory: null!,
            crdtProjectsService: null!
        );

        // Act: retry until all are imported
        var maxTries = 20;
        for (var attempt = 0; attempt < maxTries; attempt++)
        {
            try
            {
                await import.ImportProject(mockTo, mockFrom.Object, expectedEntries.Count);
            }
            catch (SimulatedException)
            {
                // Suppress, simulate server crash.
            }
            if (await mockTo.CountEntries() == expectedEntries.Count) break;
        }

        var createdEntries = await mockTo.GetAllEntries().ToArrayAsync();
        var createdPartsOfSpeech = await mockTo.GetPartsOfSpeech().ToArrayAsync();

        // Assert
        createdPartsOfSpeech.Select(pos => pos.Name["en"]).Should().BeEquivalentTo(expectedPartsOfSpeech.Select(p => p.Name["en"]));
        createdEntries.Select(e => e.LexemeForm["en"]).Should().BeEquivalentTo(expectedEntries.Select(e => e.LexemeForm["en"]));
    }



    internal static void MaybeThrowRandom(Random random, double probability)
    {
        if (random.NextDouble() < probability)
            throw new SimulatedException("Injected random failure!");
    }

    private class SimulatedException(string message) : Exception(message);

    private static async IAsyncEnumerable<T> MockAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Delay(1);
            yield return item;
        }
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }
}
internal partial class UnreliableApi(IMiniLcmApi api, Random random) : IMiniLcmApi
{

    [BeaKona.AutoInterface(IncludeBaseInterfaces = true)]
    private readonly IMiniLcmApi _api = api;

    Task<PartOfSpeech> IMiniLcmWriteApi.CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        ResumableTests.MaybeThrowRandom(random, 0.2);
        return _api.CreatePartOfSpeech(partOfSpeech);
    }
    Task<Entry> IMiniLcmWriteApi.CreateEntry(Entry entry)
    {
        ResumableTests.MaybeThrowRandom(random, 0.2);
        return _api.CreateEntry(entry);
    }
    Task<ComplexFormComponent> IMiniLcmWriteApi.CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position)
    {
        ResumableTests.MaybeThrowRandom(random, 0.2);
        return _api.CreateComplexFormComponent(complexFormComponent, position);
    }
    Task<ComplexFormType> IMiniLcmWriteApi.CreateComplexFormType(ComplexFormType complexFormType)
    {
        ResumableTests.MaybeThrowRandom(random, 0.2);
        return _api.CreateComplexFormType(complexFormType);
    }
    Task<SemanticDomain> IMiniLcmWriteApi.CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        ResumableTests.MaybeThrowRandom(random, 0.2);
        return _api.CreateSemanticDomain(semanticDomain);
    }
    Task<Publication> IMiniLcmWriteApi.CreatePublication(Publication publication)
    {
        ResumableTests.MaybeThrowRandom(random, 0.2);
        return _api.CreatePublication(publication);
    }
    Task<WritingSystem> IMiniLcmWriteApi.CreateWritingSystem(WritingSystem writingSystems)
    {
        ResumableTests.MaybeThrowRandom(random, 0.2);
        return _api.CreateWritingSystem(writingSystems);
    }
}
