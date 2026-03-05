using FluentValidation.TestHelper;
using MiniLcm.SyncHelpers;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class MorphTypeUpdateValidatorTests
{
    private readonly MorphTypeUpdateValidator _validator = new();

    private UpdateObjectInput<MorphType> NewUpdate()
    {
        return new UpdateObjectInput<MorphType>();
    }

    [Fact]
    public void Succeeds_WhenUpdatingPrefix()
    {
        var update = NewUpdate();
        update.Set(mt => mt.Prefix, "-");
        _validator.TestValidate(update).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenTryingToUpdateKind()
    {
        var update = NewUpdate();
        update.Set(mt => mt.Kind, MorphTypeKind.Suffix);
        _validator.TestValidate(update).ShouldHaveValidationErrorFor(nameof(MorphType.Kind));
    }

    [Fact]
    public void Fails_WhenTryingToUpdateDeletedAt()
    {
        var update = NewUpdate();
        update.Set(mt => mt.DeletedAt, DateTimeOffset.UtcNow);
        _validator.TestValidate(update).ShouldHaveValidationErrorFor(nameof(MorphType.DeletedAt));
    }

    [Fact]
    public void Fails_WhenThereAreNoChanges()
    {
        var update = NewUpdate();
        _validator.TestValidate(update).ShouldHaveAnyValidationError();
    }

    [Fact]
    public void MorphTypeDiffToUpdate_Throws_WhenKindDiffers()
    {
        var id = Guid.NewGuid();
        var before = new MorphType { Id = id, Kind = MorphTypeKind.Stem };
        var after = new MorphType { Id = id, Kind = MorphTypeKind.Suffix };

        var act = () => MorphTypeSync.MorphTypeDiffToUpdate(before, after);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*immutable Kind*");
    }
}
