using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public class ExampleSentenceValidator : AbstractValidator<ExampleSentence>
{
    public ExampleSentenceValidator()
    {
        RuleFor(es => es.DeletedAt).Null();
        RuleFor(es => es.Sentence).NoEmptyValues(GetExampleSentenceIdentifier)
            .NoDefaultWritingSystems(GetExampleSentenceIdentifier);
        RuleForEach(es => es.Translations).SetValidator(sentence => new ExampleSentenceTranslationValidator(sentence));
    }

    public ExampleSentenceValidator(Sense sense) : this()
    {
        //it's ok if SenseId is an Empty guid
        RuleFor(es => es.SenseId).Equal(sense.Id).When(es => es.SenseId != Guid.Empty).WithMessage(examplesentence => $"ExampleSentence (Id: {examplesentence.Id}) EntryId must match Sense {sense.Id}, but instead was {examplesentence.SenseId}");
    }

    private string GetExampleSentenceIdentifier(ExampleSentence example)
    {
        return example.Id.ToString("D");
    }
}

public class ExampleSentenceTranslationValidator : AbstractValidator<Translation>
{
    private readonly ExampleSentence? _exampleSentence;

    public ExampleSentenceTranslationValidator()
    {
        RuleFor(translation => translation.Text).NoEmptyValues(GetExampleSentenceTranslationIdentifier)
            .NoDefaultWritingSystems(GetExampleSentenceTranslationIdentifier);
    }

    /// <summary>The parent only sharpens the error message, so validating without one is fine.</summary>
    public ExampleSentenceTranslationValidator(ExampleSentence exampleSentence) : this()
    {
        _exampleSentence = exampleSentence;
    }

    private string GetExampleSentenceTranslationIdentifier(Translation translation)
    {
        if (_exampleSentence is null) return $"Translation {translation.Id:D}";
        return $"Example: {_exampleSentence.Id:D} Translation #{_exampleSentence.Translations.IndexOf(translation) + 1}";
    }
}
