using FluentValidation;
using FluentValidation.Results;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class SenseValidator : AbstractValidator<Sense>
{
    public SenseValidator()
    {
        RuleFor(s => s.DeletedAt).Null();
        RuleFor(s => s.Definition).NoEmptyValues(GetSenseIdentifier);
        RuleFor(s => s.Gloss).NoEmptyValues(GetSenseIdentifier);
        RuleFor(s => s.PartOfSpeech!).SetValidator(new PartOfSpeechValidator()).When(s => s.PartOfSpeech is not null);
        RuleFor(s => s.PartOfSpeechId).Equal(s => s.PartOfSpeech!.Id).When(s => s.PartOfSpeech is not null && s.PartOfSpeechId is not null);
        RuleForEach(s => s.SemanticDomains).SetValidator(new SemanticDomainValidator());
        RuleForEach(s => s.ExampleSentences).SetValidator(sense => new ExampleSentenceValidator(sense));
    }

    public SenseValidator(Entry entry): this()
    {
        //it's ok if senses EntryId is an Empty guid
        RuleFor(s => s.EntryId).Equal(entry.Id).When(s => s.EntryId != Guid.Empty).WithMessage(sense => $"Sense (Id: {sense.Id}) EntryId must match Entry {entry.Id}, but instead was {sense.EntryId}");
    }

    private string GetSenseIdentifier(Sense sense)
    {
        var firstGloss = sense.Gloss?.Values?.Values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstGloss))
        {
            return sense.Id.ToString("D");
        }
        return $"{sense.Gloss} - {sense.Id}";
    }

    protected override void RaiseValidationException(ValidationContext<Sense> context, ValidationResult result)
    {
        throw new ValidationException(GetSenseIdentifier(context.InstanceToValidate), result.Errors, true);
    }
}
