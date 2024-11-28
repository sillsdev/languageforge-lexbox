using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public record MiniLcmValidators(IValidator<ComplexFormType> ComplexFormTypeValidator)
{
    public async Task ValidateAndThrow(ComplexFormType value)
    {
        await ComplexFormTypeValidator.ValidateAndThrowAsync(value);
    }
}

public static class MiniLcmValidatorsExtensions
{
    public static IServiceCollection AddMiniLcmValidators(this IServiceCollection services)
    {
        services.AddTransient<MiniLcmValidators>();
        services.AddTransient<IValidator<ComplexFormType>, ComplexFormTypeValidator>();
        return services;
    }
}
