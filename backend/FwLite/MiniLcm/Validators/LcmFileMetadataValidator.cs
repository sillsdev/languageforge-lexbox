using FluentValidation;
using MiniLcm.Media;

namespace MiniLcm.Validators;

public class LcmFileMetadataValidator : AbstractValidator<LcmFileMetadata>
{
    public LcmFileMetadataValidator()
    {
        RuleFor(x => x.Filename).NotEmpty();
        RuleFor(x => x.MimeType).NotEmpty();
    }
}