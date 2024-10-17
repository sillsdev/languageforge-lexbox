using FwLiteProjectSync.SyncHelpers;
using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm.Models;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace FwLiteProjectSync.Tests;

public class UpdateDiffTests
{
    private readonly AutoFaker _autoFaker = new(new AutoFakerConfig()
    {
        Overrides = [new MultiStringOverride()]
    });

    [Fact]
    public void EntryDiffShouldUpdateAllFields()
    {
        var before = new Entry();
        var after = _autoFaker.Generate<Entry>();
        var entryDiffToUpdate = EntrySync.EntryDiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(entryDiffToUpdate);
        entryDiffToUpdate.Apply(before);
        before.Should().BeEquivalentTo(after, options => options.Excluding(x => x.Id)
            .Excluding(x => x.Senses)
            .Excluding(x => x.Components)
            .Excluding(x => x.ComplexForms)
            .Excluding(x => x.ComplexFormTypes));
    }

    [Fact]
    public async Task SenseDiffShouldUpdateAllFields()
    {
        var before = new Sense();
        var after = _autoFaker.Generate<Sense>();
        var senseDiffToUpdate = await SenseSync.SenseDiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(senseDiffToUpdate);
        senseDiffToUpdate.Apply(before);
        before.Should().BeEquivalentTo(after, options => options.Excluding(x => x.Id).Excluding(x => x.ExampleSentences));
    }

    [Fact]
    public void ExampleSentenceDiffShouldUpdateAllFields()
    {
        var before = new ExampleSentence();
        var after = _autoFaker.Generate<ExampleSentence>();
        var exampleSentenceDiffToUpdate = ExampleSentenceSync.DiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(exampleSentenceDiffToUpdate);
        exampleSentenceDiffToUpdate.Apply(before);
        before.Should().BeEquivalentTo(after, options => options.Excluding(x => x.Id));
    }
}
