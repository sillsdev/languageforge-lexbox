using FluentValidation;
using FluentValidation.Results;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class PublicationValidator : AbstractValidator<Publication>
{
    public PublicationValidator()
    {
        RuleFor(s => s.DeletedAt).Null();
        RuleFor(s => s.Name).NoEmptyValues(GetPublicationIdentifier);
    }

    private string GetPublicationIdentifier(Publication Publication)
    {
        var firstName = Publication.Name?.Values?.Values.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Publication.Id.ToString("D");
        }
        return $"{Publication.Name} - {Publication.Id}";
    }

    protected override void RaiseValidationException(ValidationContext<Publication> context, ValidationResult result)
    {
        throw new ValidationException(GetPublicationIdentifier(context.InstanceToValidate), result.Errors, true);
    }
}
