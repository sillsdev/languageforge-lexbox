using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public record MiniLcmValidators(
    IValidator<ComplexFormType> ComplexFormTypeValidator,
    IValidator<Entry> EntryValidator,
    IValidator<Sense> SenseValidator,
    IValidator<ExampleSentence> ExampleSentenceValidator,
    IValidator<WritingSystem> WritingSystemValidator,
    IValidator<PartOfSpeech> PartOfSpeechValidator,
    IValidator<SemanticDomain> SemanticDomainValidator)
{
    public async Task ValidateAndThrowAsync(ComplexFormType value)
    {
        await ComplexFormTypeValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrowAsync(Entry value)
    {
        await EntryValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrowAsync(Sense value)
    {
        await SenseValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrowAsync(ExampleSentence value)
    {
        await ExampleSentenceValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrowAsync(WritingSystem value)
    {
        await WritingSystemValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrowAsync(PartOfSpeech value)
    {
        await PartOfSpeechValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrowAsync(SemanticDomain value)
    {
        await SemanticDomainValidator.ValidateAndThrowAsync(value);
    }

    public void ValidateAndThrow(ComplexFormType value)
    {
        ComplexFormTypeValidator.ValidateAndThrow(value);
    }

    public void ValidateAndThrow(Entry value)
    {
        EntryValidator.ValidateAndThrow(value);
    }

    public void ValidateAndThrow(Sense value)
    {
        SenseValidator.ValidateAndThrow(value);
    }

    public void ValidateAndThrow(ExampleSentence value)
    {
        ExampleSentenceValidator.ValidateAndThrow(value);
    }

    public void ValidateAndThrow(WritingSystem value)
    {
        WritingSystemValidator.ValidateAndThrow(value);
    }

    public void ValidateAndThrow(PartOfSpeech value)
    {
        PartOfSpeechValidator.ValidateAndThrow(value);
    }

    public void ValidateAndThrow(SemanticDomain value)
    {
        SemanticDomainValidator.ValidateAndThrow(value);
    }

}

public static class MiniLcmValidatorsExtensions
{
    public static IServiceCollection AddMiniLcmValidators(this IServiceCollection services)
    {
        services.AddTransient<MiniLcmValidators>();
        services.AddTransient<IValidator<ComplexFormType>, ComplexFormTypeValidator>();
        services.AddTransient<IValidator<Entry>, EntryValidator>();
        services.AddTransient<IValidator<Sense>, SenseValidator>();
        services.AddTransient<IValidator<ExampleSentence>, ExampleSentenceValidator>();
        services.AddTransient<IValidator<WritingSystem>, WritingSystemValidator>();
        services.AddTransient<IValidator<PartOfSpeech>, PartOfSpeechValidator>();
        services.AddTransient<IValidator<SemanticDomain>, SemanticDomainValidator>();
        return services;
    }
}
