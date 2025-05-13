using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class WritingSystemUpdateValidatorTests
{
    private readonly WritingSystemUpdateValidator _validator = new();

    private UpdateObjectInput<WritingSystem> NewUpdate()
    {
        return new UpdateObjectInput<WritingSystem>();
    }

    [Fact]
    public void Succeeds_WhenUpdatingAbbreviation()
    {
        var update = NewUpdate();
        update.Set(ws => ws.Abbreviation, "new");
        _validator.TestValidate(update).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenTryingToUpdateWsId()
    {
        var update = NewUpdate();
        update.Set(ws => ws.WsId, (WritingSystemId)"new");
        _validator.TestValidate(update).ShouldHaveValidationErrorFor(nameof(WritingSystem.WsId));
    }

    [Fact]
    public void Fails_WhenTryingToUpdateType()
    {
        var update = NewUpdate();
        update.Set(ws => ws.Type, WritingSystemType.Analysis);
        _validator.TestValidate(update).ShouldHaveValidationErrorFor(nameof(WritingSystem.Type));
    }

    [Fact]
    public void Fails_WhenThereAreNoChanges()
    {
        var update = NewUpdate();
        _validator.TestValidate(update).ShouldHaveAnyValidationError();
    }
}
