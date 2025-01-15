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
        var example = new ExampleSentence() { Id = Guid.NewGuid(), DeletedAt = DateTimeOffset.UtcNow };
        _validator.TestValidate(example).ShouldHaveValidationErrorFor("DeletedAt");
    }

    [Theory]
    [InlineData(nameof(ExampleSentence.Sentence))]
    [InlineData(nameof(ExampleSentence.Translation))]
    public void Succeeds_WhenNonEmptyFieldIsPresent(string fieldName)
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid() };
        SetProperty(example, fieldName, "content");
        _validator.TestValidate(example).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(nameof(ExampleSentence.Sentence))]
    [InlineData(nameof(ExampleSentence.Translation))]
    public void Succeeds_WhenNonEmptyFieldHasNoContent(string fieldName)
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid() };
        MakePropertyEmpty(example, fieldName);
        _validator.TestValidate(example).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(nameof(ExampleSentence.Sentence))]
    [InlineData(nameof(ExampleSentence.Translation))]
    public void Fails_WhenNonEmptyFieldHasWsWithEmptyContent(string fieldName)
    {
        var example = new ExampleSentence() { Id = Guid.NewGuid() };
        SetProperty(example, fieldName, "");
        _validator.TestValidate(example).ShouldHaveValidationErrorFor(fieldName);
    }

    [Fact]
    public void Fails_WhenOrderIsSet()
    {
        var exampleSentence = new ExampleSentence() { Id = Guid.NewGuid(), Order = 3 };
        _validator.TestValidate(exampleSentence).ShouldHaveValidationErrorFor(nameof(IOrderable.Order));
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
