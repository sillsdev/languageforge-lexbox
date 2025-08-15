using FluentValidation;
using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class SenseValidatorTests
{
    private readonly SenseValidator _validator = new();

    [Fact]
    public void Succeeds_WhenDeletedAtIsNull()
    {
        var sense = new Sense() { Id = Guid.NewGuid(), DeletedAt = null };
        _validator.TestValidate(sense).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenDeletedAtIsNotNull()
    {
        var sense = new Sense() { Id = Guid.NewGuid(), DeletedAt = DateTimeOffset.UtcNow };
        _validator.TestValidate(sense).ShouldHaveValidationErrorFor("DeletedAt");
    }

    [Fact]
    public async Task Fails_WithIdInException()
    {
        var sense = new Sense() { Id = Guid.NewGuid(), DeletedAt = DateTimeOffset.UtcNow };
        var act = () => _validator.ValidateAndThrowAsync(sense);
        (await act.Should().ThrowAsync<ValidationException>()).Which.Message.Should().Contain(sense.Id.ToString());
    }

    [Theory]
    [InlineData("Definition")]
    [InlineData("Gloss")]
    public void Succeeds_WhenNonEmptyFieldIsPresent(string fieldName)
    {
        var sense = new Sense() { Id = Guid.NewGuid() };
        SetProperty(sense, fieldName, "content");
        _validator.TestValidate(sense).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Definition")]
    [InlineData("Gloss")]
    public void Succeeds_WhenNonEmptyFieldHasNoContent(string fieldName)
    {
        var sense = new Sense() { Id = Guid.NewGuid() };
        MakePropertyEmpty(sense, fieldName);
        _validator.TestValidate(sense).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Definition")]
    [InlineData("Gloss")]
    public void Fails_WhenNonEmptyFieldHasWsWithEmptyContent(string fieldName)
    {
        var sense = new Sense() { Id = Guid.NewGuid() };
        SetProperty(sense, fieldName, "");
        _validator.TestValidate(sense).ShouldHaveValidationErrorFor(fieldName);
    }


    private void SetProperty(Sense sense, string propName, string content)
    {
        var propInfo = typeof(Sense).GetProperty(propName);
        if (propInfo is null) return;
        if (propInfo.PropertyType == typeof(MultiString))
        {
            propInfo.SetValue(sense, new MultiString() { { "en", content } });
            return;
        }

        if (propInfo.PropertyType == typeof(RichMultiString))
        {
            propInfo.SetValue(sense, new RichMultiString() { { "en", new RichString(content) } });
            return;
        }

        throw new NotImplementedException($"Property type {propInfo.PropertyType} not supported");
    }

    private void MakePropertyEmpty(Sense sense, string propName)
    {
        var propInfo = typeof(Sense).GetProperty(propName);
        if (propInfo is null) return;
        if (propInfo.PropertyType == typeof(MultiString))
        {
            propInfo.SetValue(sense, new MultiString());
            return;
        }

        if (propInfo.PropertyType == typeof(RichMultiString))
        {
            propInfo.SetValue(sense, new RichMultiString());
            return;
        }

        throw new NotImplementedException($"Property type {propInfo.PropertyType} not supported");
    }
}
