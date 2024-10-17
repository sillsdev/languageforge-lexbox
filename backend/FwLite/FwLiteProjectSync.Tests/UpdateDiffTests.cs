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
        var previous = new Entry();
        var current = _autoFaker.Generate<Entry>();
        var entryDiffToUpdate = EntrySync.EntryDiffToUpdate(previous, current);
        ArgumentNullException.ThrowIfNull(entryDiffToUpdate);
        entryDiffToUpdate.Apply(previous);
        previous.Should().BeEquivalentTo(current, options => options.Excluding(x => x.Id)
            .Excluding(x => x.Senses)
            .Excluding(x => x.Components)
            .Excluding(x => x.ComplexForms)
            .Excluding(x => x.ComplexFormTypes));
    }

    [Fact]
    public async Task SenseDiffShouldUpdateAllFields()
    {
        var previous = new Sense();
        var current = _autoFaker.Generate<Sense>();
        var senseDiffToUpdate = await SenseSync.SenseDiffToUpdate(previous, current);
        ArgumentNullException.ThrowIfNull(senseDiffToUpdate);
        senseDiffToUpdate.Apply(previous);
        previous.Should().BeEquivalentTo(current, options => options.Excluding(x => x.Id).Excluding(x => x.ExampleSentences));
    }

    [Fact]
    public void ExampleSentenceDiffShouldUpdateAllFields()
    {
        var previous = new ExampleSentence();
        var current = _autoFaker.Generate<ExampleSentence>();
        var exampleSentenceDiffToUpdate = ExampleSentenceSync.DiffToUpdate(previous, current);
        ArgumentNullException.ThrowIfNull(exampleSentenceDiffToUpdate);
        exampleSentenceDiffToUpdate.Apply(previous);
        previous.Should().BeEquivalentTo(current, options => options.Excluding(x => x.Id));
    }
}
