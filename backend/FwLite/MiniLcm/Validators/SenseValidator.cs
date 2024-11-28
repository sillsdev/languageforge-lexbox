using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class SenseValidator : AbstractValidator<Sense>
{
    public SenseValidator()
    {
        //todo add validation for the other properties
    }

    public SenseValidator(Entry entry): this()
    {
        //it's ok if senses EntryId is an Empty guid
        RuleFor(s => s.EntryId).Equal(entry.Id).When(s => s.EntryId != Guid.Empty).WithMessage(sense => $"Sense (Id: {sense.Id}) EntryId must match Entry {entry.Id}, but instead was {sense.EntryId}");
    }
}
