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
            LexemeForm = { { "en", "Kevin" } },
            Note = { { "en", new RichString("this is a test note from Kevin") } },
            CitationForm = { { "en", "Kevin" } },
            LiteralMeaning = { { "en", new RichString("Kevin") } },
            Senses =
            [
                new Sense
                {
                    Gloss = { { "en", "Kevin" } },
                    Definition = { { "en", new RichString("Kevin") } },
                    ExampleSentences =
                    [
                        new ExampleSentence { Sentence = { { "en", new RichString("Kevin is a good guy") } } }
                    ]
                }
            ]
        });
        await Api.CreateEntry(new()
        {
            Id = Entry2Id,
            LexemeForm = { { "en", "apple" } },
            Senses =
            [
                new Sense
                {
                    Gloss = { { "en", "fruit" } },
                    Definition = { { "en", new RichString("a round fruit, red or yellow") } },
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
    public async Task UpdateEntry_SupportsRemovingAMultiStringWs()
    {
        var entry = await Api.GetEntry(Entry1Id);
        ArgumentNullException.ThrowIfNull(entry);
        var before = entry.Copy();
        entry.CitationForm.Remove("en");
        var updatedEntry = await Api.UpdateEntry(before, entry);
        updatedEntry.CitationForm.Values.Should().NotContainKey("en");
    }

    [Fact]
    public async Task UpdateEntry_SupportsRemovingARichMultiStringWs()
    {
        var entry = await Api.GetEntry(Entry1Id);
        ArgumentNullException.ThrowIfNull(entry);
        var before = entry.Copy();
        entry.Note.Remove("en");
        var updatedEntry = await Api.UpdateEntry(before, entry);
        updatedEntry.Note.Should().NotContainKey("en");
    }

    [Fact]
    public async Task UpdateEntry_RoundTripsEmptyStrings()
    {
        var entry = await Api.GetEntry(Entry1Id);
        ArgumentNullException.ThrowIfNull(entry);
        var before = entry.Copy();
        entry.CitationForm["en"] = string.Empty;
        var updatedEntry = await Api.UpdateEntry(before, entry);
        updatedEntry.CitationForm["en"].Should().Be(string.Empty);
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
    [InlineData("a,b", "a,b,c,d", "1,2,3,4")] // append
    [InlineData("a,b", "c,a,b", "0,1,2")] // single prepend
    [InlineData("a,b", "d,c,a,b", "0,0.5,1,2")] // multi prepend
    [InlineData("a,b,c,d", "d,a,b,c", "0,1,2,3")] // move to back
    [InlineData("a,b,c,d", "b,c,d,a", "2,3,4,5")] // move to front
    [InlineData("a,b,c,d,e", "a,b,e,c,d", "1,2,2.5,3,4")] // move to middle
    [InlineData("a,b,c", "c,b,a", "3,4,5")] // reverse
    [InlineData("a,b,c,d", "d,b,c,a", "1,2,3,4")] // swap
    public async Task UpdateEntry_CanReorderSenses(string before, string after, string expectedOrderValues)
    {
        // arrange
        var entryId = Guid.NewGuid();
        var senseIds = before.Split(',').Concat(after.Split(',')).Distinct()
            .ToDictionary(@char => @char, _ => Guid.NewGuid());
        var beforeSenses = before.Split(',').Select(@char => new Sense() { Id = senseIds[@char], EntryId = entryId, Gloss = { { "en", @char } } }).ToList();
        var afterSenses = after.Split(',').Select(@char => new Sense() { Id = senseIds[@char], EntryId = entryId, Gloss = { { "en", @char } } }).ToList();

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

    [Theory]
    [InlineData("a,b", "a,b,c,d", "1,2,3,4")] // append
    [InlineData("a,b", "c,a,b", "0,1,2")] // single prepend
    [InlineData("a,b", "d,c,a,b", "0,0.5,1,2")] // multi prepend
    [InlineData("a,b,c,d", "d,a,b,c", "0,1,2,3")] // move to back
    [InlineData("a,b,c,d", "b,c,d,a", "2,3,4,5")] // move to front
    [InlineData("a,b,c,d,e", "a,b,e,c,d", "1,2,2.5,3,4")] // move to middle
    [InlineData("a,b,c", "c,b,a", "3,4,5")] // reverse
    [InlineData("a,b,c,d", "d,b,c,a", "1,2,3,4")] // swap
    public async Task UpdateEntry_CanReorderExampleSentence(string before, string after, string expectedOrderValues)
    {
        // arrange
        var entryId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var exampleIds = before.Split(',').Concat(after.Split(',')).Distinct()
            .ToDictionary(@char => @char, _ => Guid.NewGuid());
        var beforeExamples = before.Split(',').Select(@char => new ExampleSentence() { Id = exampleIds[@char], SenseId = senseId, Sentence = { { "en", new RichString(@char, "en") } } }).ToList();
        var afterExamples = after.Split(',').Select(@char => new ExampleSentence() { Id = exampleIds[@char], SenseId = senseId, Sentence = { { "en", new RichString(@char, "en") } } }).ToList();

        var beforeEntry = await Api.CreateEntry(new()
        {
            Id = entryId,
            LexemeForm = { { "en", "order" } },
            Senses = [
                new Sense
                {
                    Id = senseId,
                    EntryId = entryId,
                    ExampleSentences = beforeExamples,
                }
            ]
        });
        var beforeSense = beforeEntry!.Senses[0];

        var afterEntry = beforeEntry!.Copy();
        var afterSense = afterEntry.Senses[0];
        afterSense.ExampleSentences = afterExamples;

        // sanity checks
        beforeSense.ExampleSentences.Should().BeEquivalentTo(beforeExamples, options => options.WithStrictOrdering());
        if (!ApiUsesImplicitOrdering)
        {
            beforeSense.ExampleSentences.Select(s => s.Order).Should()
                .BeEquivalentTo(Enumerable.Range(1, beforeExamples.Count), options => options.WithStrictOrdering());
        }

        // act
        await Api.UpdateEntry(beforeEntry, afterEntry);
        var actualEntry = await Api.GetEntry(afterEntry.Id);
        var actual = actualEntry!.Senses[0];

        // assert
        actual.Should().NotBeNull();
        actual.ExampleSentences.Should().BeEquivalentTo(afterSense.ExampleSentences,
            options => options.WithStrictOrdering().Excluding(s => s.Order));

        if (!ApiUsesImplicitOrdering)
        {
            var actualOrderValues = string.Join(',', actual.ExampleSentences.Select(s => s.Order.ToString(CultureInfo.GetCultureInfo("en-US"))));
            actualOrderValues.Should().Be(expectedOrderValues);
        }
    }

    [Theory]
    [InlineData("a,b", "a,b,c,d", "1,2,3,4")] // append
    [InlineData("a,b", "c,a,b", "0,1,2")] // single prepend
    [InlineData("a,b", "d,c,a,b", "0,0.5,1,2")] // multi prepend
    [InlineData("a,b,c,d", "d,a,b,c", "0,1,2,3")] // move to back
    [InlineData("a,b,c,d", "b,c,d,a", "2,3,4,5")] // move to front
    [InlineData("a,b,c,d,e", "a,b,e,c,d", "1,2,2.5,3,4")] // move to middle
    [InlineData("a,b,c", "c,b,a", "3,4,5")] // reverse
    [InlineData("a,b,c,d", "d,b,c,a", "1,2,3,4")] // swap
    public async Task UpdateEntry_CanReorderComponents(string before, string after, string expectedOrderValues)
    {
        // arrange
        var entryId = Guid.NewGuid();
        var componentHeadwordsToIds = before.Split(',').Concat(after.Split(',')).Distinct()
            .ToDictionary(i => i, _ => Guid.NewGuid());
        var componentHeadwordsToEntryIds = componentHeadwordsToIds.Keys.ToAsyncEnumerable().SelectAwait(async @char =>
        {
            var componentEntry = await Api.CreateEntry(new()
            {
                Id = Guid.NewGuid(),
                LexemeForm = { { "en", @char } },
            });
            return (Headword: @char, ComponentEntryId: componentEntry!.Id);
        }).ToBlockingEnumerable().ToDictionary(t => t.Headword, t => t.ComponentEntryId);
        var beforeComponents = before.Split(',').Select(@char => new ComplexFormComponent()
        {
            Id = componentHeadwordsToIds[@char],
            ComplexFormEntryId = entryId,
            ComplexFormHeadword = "complex-form",
            ComponentHeadword = @char,
            ComponentEntryId = componentHeadwordsToEntryIds[@char],
        }).ToList();
        var afterComponents = after.Split(',').Select(@char => new ComplexFormComponent()
        {
            Id = componentHeadwordsToIds[@char],
            ComplexFormEntryId = entryId,
            ComplexFormHeadword = "complex-form",
            ComponentHeadword = @char,
            ComponentEntryId = componentHeadwordsToEntryIds[@char],
        }).ToList();

        var beforeEntry = await Api.CreateEntry(new()
        {
            Id = entryId,
            LexemeForm = { { "en", "complex-form" } },
            Components = beforeComponents,
        });

        var afterEntry = beforeEntry!.Copy();
        afterEntry.Components = afterComponents;

        // sanity checks
        beforeEntry.Components.Should().BeEquivalentTo(beforeComponents, options => options
            .WithStrictOrdering()
            .Excluding(c => c.Id));
        if (!ApiUsesImplicitOrdering)
        {
            beforeEntry.Components.Select(s => s.Order).Should()
                .BeEquivalentTo(Enumerable.Range(1, beforeComponents.Count), options => options.WithStrictOrdering());
        }

        // act
        await Api.UpdateEntry(beforeEntry, afterEntry);
        var actual = await Api.GetEntry(afterEntry.Id);

        // assert
        actual.Should().NotBeNull();
        actual.Components.Should().BeEquivalentTo(afterEntry.Components, options => options
            .WithStrictOrdering()
            .Excluding(s => s.Order)
            .Excluding(c => c.Id));

        if (!ApiUsesImplicitOrdering)
        {
            var actualOrderValues = string.Join(',', actual.Components.Select(s => s.Order.ToString(CultureInfo.GetCultureInfo("en-US"))));
            actualOrderValues.Should().Be(expectedOrderValues);
        }
    }

    /// <summary>
    /// Tests that a deleted entry can be recreated.
    /// This is necessary if Chorus recreates an entry due to a merge conflict.
    /// </summary>
    [Fact]
    public async Task RecreateDeletedEntry()
    {
        var initial = await Api.GetEntry(Entry1Id);
        initial.Should().NotBeNull();

        await Api.DeleteEntry(Entry1Id);
        var deleted = await Api.GetEntry(Entry1Id);
        deleted.Should().BeNull();

        var recreated = await Api.CreateEntry(initial);
        recreated.Should().NotBeNull();
        recreated.Should().BeEquivalentTo(initial);
    }
}
