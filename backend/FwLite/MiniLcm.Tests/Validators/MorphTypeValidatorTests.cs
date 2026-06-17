using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class MorphTypeValidatorTests
{
    private readonly MorphTypeValidator _validator = new();

    private static MorphType NewCanonicalMorphType(MorphTypeKind kind = MorphTypeKind.Root)
    {
        return CanonicalMorphTypes.All[kind].Copy();
    }

    [Fact]
    public void Succeeds_WhenMorphTypeIsCanonical()
    {
        var morphType = NewCanonicalMorphType();
        _validator.TestValidate(morphType).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [MemberData(nameof(CanonicalKinds))]
    public void Succeeds_ForEveryCanonicalKind(MorphTypeKind kind)
    {
        var morphType = NewCanonicalMorphType(kind);
        _validator.TestValidate(morphType).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenIdIsNotCanonical()
    {
        var morphType = NewCanonicalMorphType();
        morphType.Id = Guid.NewGuid();
        _validator.TestValidate(morphType).ShouldHaveValidationErrorFor(mt => mt.Id);
    }

    [Fact]
    public void Fails_WhenIdIsEmpty()
    {
        var morphType = NewCanonicalMorphType();
        morphType.Id = Guid.Empty;
        _validator.TestValidate(morphType).ShouldHaveValidationErrorFor(mt => mt.Id);
    }

    [Fact]
    public void Fails_WhenDeletedAtIsSet()
    {
        var morphType = NewCanonicalMorphType();
        morphType.DeletedAt = DateTimeOffset.UtcNow;
        _validator.TestValidate(morphType).ShouldHaveValidationErrorFor(mt => mt.DeletedAt);
    }

    [Fact]
    public void Fails_WhenNameIsEmpty()
    {
        var morphType = NewCanonicalMorphType();
        morphType.Name = [];
        _validator.TestValidate(morphType).ShouldHaveValidationErrorFor(mt => mt.Name);
    }

    [Fact]
    public void Fails_WhenAbbreviationIsEmpty()
    {
        var morphType = NewCanonicalMorphType();
        morphType.Abbreviation = [];
        _validator.TestValidate(morphType).ShouldHaveValidationErrorFor(mt => mt.Abbreviation);
    }

    public static TheoryData<MorphTypeKind> CanonicalKinds()
    {
        var data = new TheoryData<MorphTypeKind>();
        foreach (var kind in CanonicalMorphTypes.All.Keys)
        {
            data.Add(kind);
        }
        return data;
    }
}
