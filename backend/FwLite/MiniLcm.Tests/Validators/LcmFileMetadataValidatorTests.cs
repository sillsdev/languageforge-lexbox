using FluentValidation.TestHelper;
using MiniLcm.Media;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class LcmFileMetadataValidatorTests
{
    private readonly LcmFileMetadataValidator _validator = new();

    [Fact]
    public void Succeeds_ForAPlainFilename()
    {
        _validator.TestValidate(new LcmFileMetadata("photo.jpg", "image/jpeg")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_ForAFilenameWindowsCannotStore_NamingTheFile()
    {
        _validator.TestValidate(new LcmFileMetadata("what?.jpg", "image/jpeg"))
            .ShouldHaveValidationErrorFor(x => x.Filename)
            .WithErrorMessage("'what?.jpg' can't be used as a file name. Rename the file and try again.");
    }
}
