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
        // RuleFor(s => s.PartOfSpeech).Empty(); // TODO: Comment out if we're not yet ready to move away from strings
        // RuleFor(s => s.PartOfSpeechId).SetValidator(new PartOfSpeechIdValidator()); // Can't do this statelessly, as we'd need a full PartOfSpeech object to check if it's predefined or not
        RuleForEach(s => s.SemanticDomains).SetValidator(new SemanticDomainValidator());
        RuleForEach(s => s.ExampleSentences).SetValidator(sense => new ExampleSentenceValidator(sense));
    }

    public SenseValidator(Entry entry): this()
    {
        //it's ok if senses EntryId is an Empty guid
        RuleFor(s => s.EntryId).Equal(entry.Id).When(s => s.EntryId != Guid.Empty).WithMessage(sense => $"Sense (Id: {sense.Id}) EntryId must match Entry {entry.Id}, but instead was {sense.EntryId}");
    }
}
