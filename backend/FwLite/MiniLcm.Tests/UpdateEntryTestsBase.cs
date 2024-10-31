﻿namespace MiniLcm.Tests;

public abstract class UpdateEntryTestsBase : MiniLcmTestBase
{
    protected readonly Guid Entry1Id = new Guid("a3f5aa5a-578f-4181-8f38-eaaf27f01f1c");
    protected readonly Guid Entry2Id = new Guid("2de6c334-58fa-4844-b0fd-0bc2ce4ef835");

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreateWritingSystem(WritingSystemType.Analysis,
            new WritingSystem()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Analysis,
                WsId = "en",
                Name = "English",
                Abbreviation = "En",
                Font = "Arial",
                Exemplars = []
            });
        await Api.CreateWritingSystem(WritingSystemType.Vernacular,
            new WritingSystem()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Vernacular,
                WsId = "en",
                Name = "English",
                Abbreviation = "En",
                Font = "Arial",
                Exemplars = []
            });
        await Api.CreateEntry(new Entry
        {
            Id = Entry1Id,
            LexemeForm = { Values = { { "en", "Kevin" } } },
            Note = { Values = { { "en", "this is a test note from Kevin" } } },
            CitationForm = { Values = { { "en", "Kevin" } } },
            LiteralMeaning = { Values = { { "en", "Kevin" } } },
            Senses =
            [
                new Sense
                {
                    Gloss = { Values = { { "en", "Kevin" } } },
                    Definition = { Values = { { "en", "Kevin" } } },
                    ExampleSentences =
                    [
                        new ExampleSentence { Sentence = { Values = { { "en", "Kevin is a good guy" } } } }
                    ]
                }
            ]
        });
        await Api.CreateEntry(new()
        {
            Id = Entry2Id,
            LexemeForm = { Values = { { "en", "apple" } } },
            Senses =
            [
                new Sense
                {
                    Gloss = { Values = { { "en", "fruit" } } },
                    Definition = { Values = { { "en", "a round fruit, red or yellow" } } },
                }
            ],
        });
    }

    [Fact]
    public async Task UpdateEntry_Works()
    {
        var entry = await Api.GetEntry(Entry1Id);
        ArgumentNullException.ThrowIfNull(entry);
        entry.LexemeForm["en"] = "updated";
        var updatedEntry = await Api.UpdateEntry(entry);
        updatedEntry.LexemeForm["en"].Should().Be("updated");
        updatedEntry.Should().BeEquivalentTo(entry, options => options.Excluding(e => e.Version));
    }

    [Fact]
    public async Task UpdateEntry_SupportsSenseChanges()
    {
        var entry = await Api.GetEntry(Entry1Id);
        ArgumentNullException.ThrowIfNull(entry);
        entry.Senses[0].Gloss["en"] = "updated";
        var updatedEntry = await Api.UpdateEntry(entry);
        updatedEntry.Senses[0].Gloss["en"].Should().Be("updated");
        updatedEntry.Should().BeEquivalentTo(entry, options => options.Excluding(e => e.Version));
    }
}
