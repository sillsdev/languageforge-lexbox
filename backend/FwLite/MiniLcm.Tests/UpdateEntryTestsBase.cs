using System.Globalization;

namespace MiniLcm.Tests;

public abstract class UpdateEntryTestsBase : MiniLcmTestBase
{
    protected readonly Guid Entry1Id = new Guid("a3f5aa5a-578f-4181-8f38-eaaf27f01f1c");
    protected readonly Guid Entry2Id = new Guid("2de6c334-58fa-4844-b0fd-0bc2ce4ef835");

    protected virtual bool ApiUsesImplicitOrdering => false;

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

    [Theory]
    [InlineData("1,2", "1,2,3,4", "1,2,3,4")] // append
    [InlineData("1,2", "3,1,2", "0,1,2")] // single prepend
    [InlineData("1,2", "4,3,1,2", "0,0.5,1,2")] // multi prepend
    [InlineData("1,2,3,4", "4,1,2,3", "0,1,2,3")] // move to back
    [InlineData("1,2,3,4", "2,3,4,1", "2,3,4,5")] // move to front
    [InlineData("1,2,3,4,5", "1,2,5,3,4", "1,2,2.5,3,4")] // move to middle
    [InlineData("1,2,3", "3,2,1", "3,4,5")] // reverse
    [InlineData("1,2,3,4", "4,2,3,1", "1,2,3,4")] // swap
    public async Task UpdateEntry_CanReorderSenses(string before, string after, string expectedOrderValues)
    {
        // arrange
        var entryId = Guid.NewGuid();
        var senseIds = before.Split(',').Concat(after.Split(',')).Distinct()
            .ToDictionary(i => i, _ => Guid.NewGuid());
        var beforeSenses = before.Split(',').Select(i => new Sense() { Id = senseIds[i], EntryId = entryId, Gloss = { { "en", i } } }).ToList();
        var afterSenses = after.Split(',').Select(i => new Sense() { Id = senseIds[i], EntryId = entryId, Gloss = { { "en", i } } }).ToList();

        var beforeEntry = await Api.CreateEntry(new()
        {
            Id = entryId,
            LexemeForm = { { "en", "order" } },
            Senses = beforeSenses,
        });

        var afterEntry = beforeEntry!.Copy();
        afterEntry.Senses = afterSenses;

        // sanity checks
        beforeEntry.Senses.Should().BeEquivalentTo(beforeSenses, options => options.WithStrictOrdering());
        if (!ApiUsesImplicitOrdering)
        {
            beforeEntry.Senses.Select(s => s.Order).Should()
                .BeEquivalentTo(Enumerable.Range(1, beforeSenses.Count), options => options.WithStrictOrdering());
        }

        // act
        await Api.UpdateEntry(beforeEntry, afterEntry);
        var actual = await Api.GetEntry(afterEntry.Id);

        // assert
        actual.Should().NotBeNull();
        actual.Senses.Should().BeEquivalentTo(afterEntry.Senses,
            options => options.WithStrictOrdering().Excluding(s => s.Order));

        if (!ApiUsesImplicitOrdering)
        {
            var actualOrderValues = string.Join(',', actual.Senses.Select(s => s.Order.ToString(CultureInfo.GetCultureInfo("en-US"))));
            actualOrderValues.Should().Be(expectedOrderValues);
        }
    }
}
