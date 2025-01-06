using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class ExampleSentenceValidatorTests
{
    private readonly ExampleSentenceValidator _validator = new();

    [Fact]
    public void Succeeds_WhenDeletedAtIsNull()
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid(), Sentence = new MultiString(){{"en", "sentence"}}, DeletedAt = null };
        _validator.TestValidate(example).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenDeletedAtIsNotNull()
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid(), Sentence = new MultiString(){{"en", "sentence"}}, DeletedAt = DateTimeOffset.UtcNow };
        _validator.TestValidate(example).ShouldHaveValidationErrorFor("DeletedAt");
    }

    [Fact]
    public void Succeeds_WhenSentenceIsPresent()
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid(), Sentence = new MultiString(){{"en", "sentence"}} };
        _validator.TestValidate(example).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenSentenceIsMissing()
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid() };
        _validator.TestValidate(example).ShouldHaveValidationErrorFor("Sentence");
    }

    [Fact]
    public void Fails_WhenSentenceHasNoContent()
    {
        // Technically the same as Fails_WhenSentenceIsMissing -- should we combine them?
        var example = new ExampleSentence() { Id = Guid.NewGuid(), Sentence = new MultiString() };
        _validator.TestValidate(example).ShouldHaveValidationErrorFor("Sentence");
    }

    [Fact]
    public void Fails_WhenSentenceHasWsWithEmptyContent()
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid(), Sentence = new MultiString(){{"en", ""}} };
        _validator.TestValidate(example).ShouldHaveValidationErrorFor("Sentence");
    }

    [Theory]
    [InlineData("Translation")]
    public void Succeeds_WhenNonEmptyFieldIsPresent(string fieldName)
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid(), Sentence = new MultiString(){{"en", "sentence"}} };
        SetProperty(example, fieldName, "content");
        _validator.TestValidate(example).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Translation")]
    public void Succeeds_WhenNonEmptyFieldHasNoContent(string fieldName)
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid(), Sentence = new MultiString(){{"en", "sentence"}} };
        MakePropertyEmpty(example, fieldName);
        _validator.TestValidate(example).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Translation")]
    public void Fails_WhenNonEmptyFieldHasWsWithEmptyContent(string fieldName)
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid(), Sentence = new MultiString(){{"en", "sentence"}} };
        SetProperty(example, fieldName, "");
        _validator.TestValidate(example).ShouldHaveValidationErrorFor(fieldName);
    }

    private void SetProperty(ExampleSentence example, string propName, string content)
    {
        var propInfo = typeof(ExampleSentence).GetProperty(propName);
        propInfo?.SetValue(example, new MultiString(){{"en", content}});
    }

    private void MakePropertyEmpty(ExampleSentence example, string propName)
    {
        var propInfo = typeof(ExampleSentence).GetProperty(propName);
        propInfo?.SetValue(example, new MultiString());
    }
}
