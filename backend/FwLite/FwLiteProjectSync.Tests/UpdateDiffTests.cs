using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace FwLiteProjectSync.Tests;

public class UpdateDiffTests
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.Config);

    [Fact]
    public void EntryDiffShouldUpdateAllFields()
    {
        var before = new Entry();
        var after = AutoFaker.Generate<Entry>();
        var entryDiffToUpdate = EntrySync.EntryDiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(entryDiffToUpdate);
        entryDiffToUpdate.Apply(before);
        before.Should().BeEquivalentTo(after, options =>
        {
            return options.Excluding(x => x.Id)
                .Excluding(x => x.DeletedAt).Excluding(x => x.Senses)
                .Excluding(x => x.Components)
                .Excluding(x => x.ComplexForms)
                .Excluding(x => x.ComplexFormTypes)
                .Excluding(x => x.PublishIn);
        });
    }

    [Fact]
    public void SenseDiffShouldUpdateAllFields()
    {
        var before = new Sense();
        var after = AutoFaker.Generate<Sense>();
        var senseDiffToUpdate = SenseSync.SenseDiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(senseDiffToUpdate);
        senseDiffToUpdate.Apply(before);
        before.Should().BeEquivalentTo(after, options => options.Excluding(x => x.Id)
            .Excluding(x => x.EntryId)
            .Excluding(x => x.DeletedAt)
            .Excluding(x => x.ExampleSentences)
            .Excluding(x => x.SemanticDomains)
            .Excluding(x => x.PartOfSpeech)
            .Excluding(x => x.PartOfSpeechId));
    }

    [Fact]
    public void ExampleSentenceDiffShouldUpdateAllFields()
    {
        var before = new ExampleSentence();
        var after = AutoFaker.Generate<ExampleSentence>();
        var exampleSentenceDiffToUpdate = ExampleSentenceSync.DiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(exampleSentenceDiffToUpdate);
        exampleSentenceDiffToUpdate.Apply(before);
        before.Should().BeEquivalentTo(after, options => options
            .Excluding(x => x.Id)
            .Excluding(x => x.DefaultFirstTranslationId)
            .Excluding(x => x.SenseId)
            .Excluding(x => x.Translations)
            .Excluding(x => x.DeletedAt));
    }

    [Fact]
    public void TranslationDiffShouldUpdateAllFields()
    {
        var before = new Translation();
        var after = AutoFaker.Generate<Translation>();
        var translationDiffToUpdate = ExampleSentenceSync.DiffToUpdate(before, after);
        ArgumentNullException.ThrowIfNull(translationDiffToUpdate);
        translationDiffToUpdate.Apply(before);
        before.Should().BeEquivalentTo(after, options => options
            .Excluding(x => x.Id));
    }
}
