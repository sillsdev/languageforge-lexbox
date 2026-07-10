using FluentValidation.TestHelper;
using MiniLcm.Validators;
using SystemTextJsonPatch;

namespace MiniLcm.Tests.Validators;

public class VariantUpdateValidationTests
{
    private readonly VariantUpdateValidator _validator = new();

    private static UpdateObjectInput<Variant> Patch(Action<JsonPatchDocument<Variant>> build)
    {
        var document = new JsonPatchDocument<Variant>();
        build(document);
        return new UpdateObjectInput<Variant>(document);
    }

    [Fact]
    public void AllowsHideMinorEntryAndComment()
    {
        var update = Patch(doc =>
        {
            doc.Replace(v => v.HideMinorEntry, true);
            doc.Replace(v => v.Comment, new RichMultiString { { "en", new RichString("note") } });
        });
        _validator.TestValidate(update).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RejectsEmptyPatch()
    {
        _validator.TestValidate(Patch(_ => { })).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsTypesReplacement()
    {
        var update = Patch(doc => doc.Replace(v => v.Types, []));
        _validator.TestValidate(update).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsTypesPatchByIndex()
    {
        var update = Patch(doc => doc.Operations.Add(
            new SystemTextJsonPatch.Operations.Operation<Variant>("remove", "/Types/0", null)));
        _validator.TestValidate(update).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(nameof(Variant.VariantEntryId))]
    [InlineData(nameof(Variant.MainEntryId))]
    [InlineData(nameof(Variant.MainSenseId))]
    [InlineData(nameof(Variant.VariantHeadword))]
    [InlineData(nameof(Variant.MainHeadword))]
    [InlineData(nameof(Variant.DeletedAt))]
    [InlineData(nameof(Variant.Id))]
    public void RejectsProtectedProperties(string property)
    {
        var update = Patch(doc => doc.Operations.Add(
            new SystemTextJsonPatch.Operations.Operation<Variant>("replace", $"/{property}", null, Guid.NewGuid())));
        _validator.TestValidate(update).IsValid.Should().BeFalse();
    }
}
