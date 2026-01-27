using System.Diagnostics;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Xunit.Abstractions;

namespace LcmCrdt.Tests;

[Collection("Performance")]
public class BulkCreateEntriesTests(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(["en"]));
    private readonly MiniLcmApiFixture _fixture = new();

    public async Task InitializeAsync()
    {
        _fixture.LogTo(output);
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    private async IAsyncEnumerable<Entry> SeedData(int entryCount, IMiniLcmApi api)
    {
        // part of speech
        var posCount = 10;
        var partsOfSpeech = new List<PartOfSpeech>(posCount);
        for (var i = 0; i < posCount; i++)
        {
            partsOfSpeech.Add(await api.CreatePartOfSpeech(AutoFaker.Generate<PartOfSpeech>()));
        }

        output.WriteLine($"Created {posCount} parts of speech");
        var complexFormTypeCount = 5;
        var complexFormTypes = new List<ComplexFormType>(complexFormTypeCount);
        for (var i = 0; i < complexFormTypeCount; i++)
        {
            complexFormTypes.Add(await api.CreateComplexFormType(AutoFaker.Generate<ComplexFormType>()));
        }

        output.WriteLine($"Created {complexFormTypeCount} complex form types");
        // semantic domains
        var domainCount = 10;
        var domains = new List<SemanticDomain>(domainCount);
        for (var i = 0; i < domainCount; i++)
        {
            domains.Add(await api.CreateSemanticDomain(AutoFaker.Generate<SemanticDomain>()));
        }

        output.WriteLine($"Created {domainCount} semantic domains");
        for (var i = 0; i < entryCount; i++)
        {
            yield return new Entry()
            {
                Id = Guid.NewGuid(),
                LexemeForm = { ["en"] = AutoFaker.Faker.Hacker.Noun() },
                ComplexFormTypes = [.. AutoFaker.Faker.Random.ListItems(complexFormTypes)],
                Senses =
                [
                    ..Enumerable
                    .Range(0, AutoFaker.Faker.Random.Int(1, 3))
                    .Select(_ => new Sense
                    {
                        Id = Guid.NewGuid(),
                        Gloss = { ["en"] = AutoFaker.Faker.Lorem.Word() },
                        Definition = { ["en"] = new RichString(AutoFaker.Faker.Lorem.Sentence(), "en") },
                        PartOfSpeech = AutoFaker.Faker.Random.ListItem(partsOfSpeech),
                        SemanticDomains =
                            AutoFaker.Faker.Random.ListItems(domains, AutoFaker.Faker.Random.Int(1, 3)),
                        ExampleSentences =
                        [
                            ..Enumerable.Range(0, AutoFaker.Faker.Random.Int(0, 2))
                            .Select(_ => new ExampleSentence
                            {
                                Id = Guid.NewGuid(),
                                Sentence = { ["en"] = new RichString(AutoFaker.Faker.Lorem.Sentence(), "en") },
                                Translations =
                                [
                                    new()
                                    {
                                        Text =
                                        {
                                            ["en"] = new RichString(AutoFaker.Faker.Lorem.Sentence(), "en")
                                        }
                                    }
                                ]
                            })
                        ]
                    })
                ]
            };
        }
    }

    [Fact]
    public async Task BulkCreateEntriesPerformance()
    {
        var entryCount = 20_000;
        // Arrange
        var entries = (await SeedData(entryCount, _fixture.Api).ToListAsync())
            .ToAsyncEnumerable();

        // Act
        GC.Collect();

        var sw = Stopwatch.StartNew();
        await _fixture.Api.BulkCreateEntries(entries);
        sw.Stop();

        // Assert
        var elapsed = sw.Elapsed;
        var perEntry = elapsed.TotalMilliseconds / entryCount;
        output.WriteLine($"Imported {entryCount:N0} entries in {elapsed.TotalSeconds:F2} seconds ({perEntry:F2} ms/entry)");
        perEntry.Should().BeLessThan(15,
            "Importing {1:N0} entries should take less than 15ms per entry, but took {0:F2}ms per entry",
            perEntry,
            entryCount);
    }
}
