using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class ComplexFormTypeValidationTests
{
    private readonly ComplexFormTypeValidator _validator = new();

    [Fact]
    public void FailsForEmptyName()
    {
        var complexFormType = new ComplexFormType() { Name = new MultiString() };
        _validator.TestValidate(complexFormType).ShouldHaveValidationErrorFor(c => c.Name);
    }
    [Fact]
    public void FailsForNameWithEmptyStringValue()
    {
        var complexFormType = new ComplexFormType() { Name = new(){ { "en", string.Empty } } };
        _validator.TestValidate(complexFormType).ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void FailsForNonNullDeletedAt()
    {
        var complexFormType = new ComplexFormType()
        {
            Name = new() { { "en", "test" } }, DeletedAt = DateTimeOffset.UtcNow
        };
        _validator.TestValidate(complexFormType).ShouldHaveValidationErrorFor(c => c.DeletedAt);
    }

    [Fact]
    public void Succeeds()
    {
        var complexFormType = new ComplexFormType()
        {
            Name = new() { { "en", "test" } },
            DeletedAt = null
        };
        _validator.TestValidate(complexFormType).ShouldNotHaveAnyValidationErrors();
    }
}
