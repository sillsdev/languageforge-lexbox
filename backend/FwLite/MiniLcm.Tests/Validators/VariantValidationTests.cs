using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class VariantValidationTests
{
    private readonly VariantValidator _validator = new();

    private static Variant NewVariant()
    {
        return new Variant
        {
            Id = Guid.NewGuid(),
            VariantEntryId = Guid.NewGuid(),
            MainEntryId = Guid.NewGuid(),
        };
    }

    [Fact]
    public void Succeeds()
    {
        _validator.TestValidate(NewVariant()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FailsForNonNullDeletedAt()
    {
        var variant = NewVariant() with { DeletedAt = DateTimeOffset.UtcNow };
        _validator.TestValidate(variant).ShouldHaveValidationErrorFor(v => v.DeletedAt);
    }

    [Fact]
    public void FailsForSelfReference()
    {
        var id = Guid.NewGuid();
        var variant = NewVariant() with { VariantEntryId = id, MainEntryId = id };
        _validator.TestValidate(variant).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SucceedsWhenVariantEntryIdIsEmpty()
    {
        // nested in an entry the variant-entry side may be inferred from the parent
        var variant = NewVariant() with { VariantEntryId = Guid.Empty };
        _validator.TestValidate(variant).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FailsForCommentWithEmptyStringValue()
    {
        var variant = NewVariant();
        variant.Comment = new() { { "en", new RichString(string.Empty) } };
        _validator.TestValidate(variant).ShouldHaveValidationErrorFor(v => v.Comment);
    }

    [Fact]
    public void FailsForTypeRefWithoutId()
    {
        var variant = NewVariant();
        variant.Types = [new VariantTypeRef { Id = Guid.Empty }];
        _validator.TestValidate(variant).IsValid.Should().BeFalse();
    }
}
