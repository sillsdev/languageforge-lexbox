using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace FwLiteProjectSync.Tests;

public class UpdateDiffTests
{
    private static readonly AutoFaker AutoFaker = new(new AutoFakerConfig()
    {
        RepeatCount = 5,
        Overrides =
        [
            new MultiStringOverride(),
            new WritingSystemIdOverride(),
            new OrderableOverride(),
        ]
    });

    [Fact]
    public void EntryDiffShouldUpdateAllFields()
    {
        var before = new Entry();
        var after = AutoFaker.Generate<Entry>();
        var entryDiffToUpdate = EntrySync.EntryDiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(entryDiffToUpdate);
        entryDiffToUpdate.Apply(before);
         before.Should().BeEquivalentTo(after, options => options.Excluding(x => x.Id)
            .Excluding(x => x.DeletedAt).Excluding(x => x.Senses)
            .Excluding(x => x.Components)
            .Excluding(x => x.ComplexForms)
            .Excluding(x => x.ComplexFormTypes));
    }

    [Fact]
    public async Task SenseDiffShouldUpdateAllFields()
    {
        var before = new Sense();
        var after = AutoFaker.Generate<Sense>();
        var senseDiffToUpdate = await SenseSync.SenseDiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(senseDiffToUpdate);
        senseDiffToUpdate.Apply(before);
        before.Should().BeEquivalentTo(after, options => options.Excluding(x => x.Id).Excluding(x => x.EntryId).Excluding(x => x.DeletedAt).Excluding(x => x.ExampleSentences).Excluding(x => x.SemanticDomains));
    }

    [Fact]
    public void ExampleSentenceDiffShouldUpdateAllFields()
    {
        var before = new ExampleSentence();
        var after = AutoFaker.Generate<ExampleSentence>();
        var exampleSentenceDiffToUpdate = ExampleSentenceSync.DiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(exampleSentenceDiffToUpdate);
        exampleSentenceDiffToUpdate.Apply(before);
        before.Should().BeEquivalentTo(after, options => options.Excluding(x => x.Id).Excluding(x => x.SenseId).Excluding(x => x.DeletedAt));
    }
}
