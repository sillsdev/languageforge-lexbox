using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Models;

namespace MiniLcm.Validators;

public record MiniLcmValidators(IValidator<ComplexFormType> ComplexFormTypeValidator);

public static class MiniLcmValidatorsExtensions
{
    public static IServiceCollection AddMiniLcmValidators(this IServiceCollection services)
    {
        services.AddTransient<MiniLcmValidators>();
        services.AddTransient<IValidator<ComplexFormType>, ComplexFormTypeValidator>();
        return services;
    }
}
