using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class ExampleSentenceValidatorTests
{
    private readonly ExampleSentenceValidator _validator = new();

    [Fact]
    public void Succeeds_WhenDeletedAtIsNull()
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid(), Sentence = new(){{"en", new RichString("sentence")}}, DeletedAt = null };
        _validator.TestValidate(example).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenDeletedAtIsNotNull()
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid(), DeletedAt = DateTimeOffset.UtcNow };
        _validator.TestValidate(example).ShouldHaveValidationErrorFor("DeletedAt");
    }

    [Theory]
    [InlineData(nameof(ExampleSentence.Sentence))]
    public void Succeeds_WhenNonEmptyFieldIsPresent(string fieldName)
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid() };
        SetProperty(example, fieldName, "content");
        _validator.TestValidate(example).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(nameof(ExampleSentence.Sentence))]
    public void Succeeds_WhenNonEmptyFieldHasNoContent(string fieldName)
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid() };
        MakePropertyEmpty(example, fieldName);
        _validator.TestValidate(example).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(nameof(ExampleSentence.Sentence))]
    public void Fails_WhenNonEmptyFieldHasWsWithEmptyContent(string fieldName)
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid() };
        SetProperty(example, fieldName, "");
        _validator.TestValidate(example).ShouldHaveValidationErrorFor(fieldName);
    }

    private void SetProperty(ExampleSentence example, string propName, string content)
    {
        var propInfo = typeof(ExampleSentence).GetProperty(propName);
        propInfo?.SetValue(example, new RichMultiString(){{"en", new RichString(content)}});
    }

    private void MakePropertyEmpty(ExampleSentence example, string propName)
    {
        var propInfo = typeof(ExampleSentence).GetProperty(propName);
        propInfo?.SetValue(example, new RichMultiString());
    }
}
