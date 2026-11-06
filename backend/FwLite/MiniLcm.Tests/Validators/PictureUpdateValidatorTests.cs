using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class PictureUpdateValidatorTests
{
    private readonly PictureUpdateValidator _validator = new();

    [Fact]
    public void Succeeds_WhenUpdatingCaption()
    {
        var update = new UpdateObjectInput<Picture>().Set(p => p.Caption["en"], new RichString("caption", "en"));
        _validator.TestValidate(update).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WhenTryingToUpdateOrder()
    {
        var update = new UpdateObjectInput<Picture>().Set(p => p.Order, 5);
        _validator.TestValidate(update).ShouldHaveValidationErrorFor(nameof(Picture.Order));
    }
}
