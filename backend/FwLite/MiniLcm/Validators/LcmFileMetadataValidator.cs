using FluentValidation;
using MiniLcm.Media;

namespace MiniLcm.Validators;

public class LcmFileMetadataValidator : AbstractValidator<LcmFileMetadata>
{
    public LcmFileMetadataValidator()
    {
        RuleFor(x => x.Filename).Must(MediaFilename.IsValid).WithMessage("'{PropertyValue}' is not a valid file name");
        RuleFor(x => x.MimeType).NotEmpty();
    }
}