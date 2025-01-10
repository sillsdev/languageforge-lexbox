using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class SenseValidator : AbstractValidator<Sense>
{
    public SenseValidator()
    {
        RuleFor(s => s.DeletedAt).Null();
        RuleFor(s => s.Definition).NoEmptyValues();
        RuleFor(s => s.Gloss).NoEmptyValues();
        RuleFor(s => s.PartOfSpeech!).SetValidator(new PartOfSpeechValidator()).When(s => s.PartOfSpeech is not null);
        RuleForEach(s => s.SemanticDomains).SetValidator(new SemanticDomainValidator());
        RuleForEach(s => s.ExampleSentences).SetValidator(sense => new ExampleSentenceValidator(sense));
    }

    public SenseValidator(Entry entry): this()
    {
        //it's ok if senses EntryId is an Empty guid
        RuleFor(s => s.EntryId).Equal(entry.Id).When(s => s.EntryId != Guid.Empty).WithMessage(sense => $"Sense (Id: {sense.Id}) EntryId must match Entry {entry.Id}, but instead was {sense.EntryId}");
    }
}
