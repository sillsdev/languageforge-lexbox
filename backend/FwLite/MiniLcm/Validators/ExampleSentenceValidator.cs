using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class ExampleSentenceValidator : AbstractValidator<ExampleSentence>
{
    public ExampleSentenceValidator()
    {
        RuleFor(es => es.DeletedAt).Null();
        RuleFor(es => es.Sentence).NoEmptyValues();
        RuleFor(es => es.Translation).NoEmptyValues();
    }

    public ExampleSentenceValidator(Sense sense) : this()
    {
        //it's ok if SenseId is an Empty guid
        RuleFor(es => es.SenseId).Equal(sense.Id).When(es => es.SenseId != Guid.Empty).WithMessage(examplesentence => $"ExampleSentence (Id: {examplesentence.Id}) EntryId must match Sense {sense.Id}, but instead was {examplesentence.SenseId}");
    }
}
