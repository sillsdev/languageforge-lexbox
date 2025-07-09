﻿using FluentValidation;
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
    IValidator<SemanticDomain> SemanticDomainValidator,
    IValidator<UpdateObjectInput<WritingSystem>> WritingSystemUpdateValidator)
{
    public async Task ValidateAndThrow(ComplexFormType value)
    {
        await ComplexFormTypeValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrow(Entry value)
    {
        await EntryValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrow(Sense value)
    {
        await SenseValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrow(ExampleSentence value)
    {
        await ExampleSentenceValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrow(WritingSystem value)
    {
        await WritingSystemValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrow(PartOfSpeech value)
    {
        await PartOfSpeechValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrow(SemanticDomain value)
    {
        await SemanticDomainValidator.ValidateAndThrowAsync(value);
    }

    public async Task ValidateAndThrow(UpdateObjectInput<WritingSystem> update)
    {
        await WritingSystemUpdateValidator.ValidateAndThrowAsync(update);
    }
}

public static class MiniLcmValidatorsExtensions
{
    public static IServiceCollection AddMiniLcmValidators(this IServiceCollection services)
    {
        services.AddTransient<MiniLcmApiValidationWrapperFactory>();
        services.AddTransient<MiniLcmValidators>();
        services.AddTransient<IValidator<ComplexFormType>, ComplexFormTypeValidator>();
        services.AddTransient<IValidator<Entry>, EntryValidator>();
        services.AddTransient<IValidator<Sense>, SenseValidator>();
        services.AddTransient<IValidator<ExampleSentence>, ExampleSentenceValidator>();
        services.AddTransient<IValidator<WritingSystem>, WritingSystemValidator>();
        services.AddTransient<IValidator<PartOfSpeech>, PartOfSpeechValidator>();
        services.AddTransient<IValidator<SemanticDomain>, SemanticDomainValidator>();
        services.AddTransient<IValidator<UpdateObjectInput<WritingSystem>>, WritingSystemUpdateValidator>();
        return services;
    }
}
