using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class VariantTypeValidationTests
{
    private readonly VariantTypeValidator _validator = new();

    [Fact]
    public void FailsForEmptyName()
    {
        var variantType = new VariantType() { Name = new MultiString() };
        _validator.TestValidate(variantType).ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void FailsForNameWithEmptyStringValue()
    {
        var variantType = new VariantType() { Name = new() { { "en", string.Empty } } };
        _validator.TestValidate(variantType).ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void FailsForNonNullDeletedAt()
    {
        var variantType = new VariantType()
        {
            Name = new() { { "en", "test" } }, DeletedAt = DateTimeOffset.UtcNow
        };
        _validator.TestValidate(variantType).ShouldHaveValidationErrorFor(c => c.DeletedAt);
    }

    [Fact]
    public void Succeeds()
    {
        var variantType = new VariantType()
        {
            Name = new() { { "en", "test" } },
            DeletedAt = null
        };
        _validator.TestValidate(variantType).ShouldNotHaveAnyValidationErrors();
    }
}
