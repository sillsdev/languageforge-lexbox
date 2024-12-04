namespace MiniLcm.Tests;

public abstract class UpdateEntryTestsBase : MiniLcmTestBase
{
    protected readonly Guid Entry1Id = new Guid("a3f5aa5a-578f-4181-8f38-eaaf27f01f1c");
    protected readonly Guid Entry2Id = new Guid("2de6c334-58fa-4844-b0fd-0bc2ce4ef835");

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
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
        var before = entry.Copy();
        entry.LexemeForm["en"] = "updated";
        var updatedEntry = await Api.UpdateEntry(before, entry);
        updatedEntry.LexemeForm["en"].Should().Be("updated");
        updatedEntry.Should().BeEquivalentTo(entry, options => options);
    }

    [Fact]
    public async Task UpdateEntry_SupportsSenseChanges()
    {
        var entry = await Api.GetEntry(Entry1Id);
        ArgumentNullException.ThrowIfNull(entry);
        var before = entry.Copy();
        entry.Senses[0].Gloss["en"] = "updated";
        var updatedEntry = await Api.UpdateEntry(before, entry);
        updatedEntry.Senses[0].Gloss["en"].Should().Be("updated");
        updatedEntry.Should().BeEquivalentTo(entry, options => options);
    }

    [Fact]
    public async Task UpdateEntry_SupportsRemovingSenseChanges()
    {
        var entry = await Api.GetEntry(Entry1Id);
        ArgumentNullException.ThrowIfNull(entry);
        var before = entry.Copy();
        var senseCount = entry.Senses.Count;
        entry.Senses.RemoveAt(0);
        var updatedEntry = await Api.UpdateEntry(before, entry);
        updatedEntry.Senses.Should().HaveCount(senseCount - 1);
        updatedEntry.Should().BeEquivalentTo(entry, options => options);
    }

    [Fact]
    public async Task UpdateEntry_CanUseSameVersionMultipleTimes()
    {
        var original = await Api.GetEntry(Entry1Id);
        await Task.Delay(1000);
        ArgumentNullException.ThrowIfNull(original);
        var update1 = original.Copy();
        var update2 = original.Copy();

        update1.LexemeForm["en"] = "updated";
        var updatedEntry = await Api.UpdateEntry(original, update1);
        updatedEntry.LexemeForm["en"].Should().Be("updated");
        updatedEntry.Should().BeEquivalentTo(update1);


        update2.LexemeForm["es"] = "updated again";
        var updatedEntry2 = await Api.UpdateEntry(original, update2);
        updatedEntry2.LexemeForm["en"].Should().Be("updated");
        updatedEntry2.LexemeForm["es"].Should().Be("updated again");
        updatedEntry2.Should().BeEquivalentTo(update2,
            options => options.Excluding(e => e.LexemeForm));
    }
}
