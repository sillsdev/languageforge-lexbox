using FluentValidation;
using MiniLcm.Media;

namespace MiniLcm.Validators;

public class LcmFileMetadataValidator : AbstractValidator<LcmFileMetadata>
{
    public LcmFileMetadataValidator()
    {
        RuleFor(x => x.Filename).Must(MediaFilename.IsValid).WithMessage("'{PropertyValue}' can't be used as a file name. Rename the file and try again.");
        RuleFor(x => x.MimeType).NotEmpty();
    }
}