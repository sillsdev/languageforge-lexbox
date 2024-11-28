using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

internal static class MultiStringValidator
{
    public static IRuleBuilderOptions<T, MultiString> Required<T>(this IRuleBuilder<T, MultiString> ruleBuilder)
    {
        return ruleBuilder.NotEmpty().NoEmptyValues();
    }
    public static IRuleBuilderOptions<T, MultiString> NoEmptyValues<T>(this IRuleBuilder<T, MultiString> ruleBuilder)
    {
        return ruleBuilder.Must(ms => ms.Values.All(v => !string.IsNullOrEmpty(v.Value))).WithMessage((parent, ms) =>
            $"MultiString must not contain empty values, but [{string.Join(", ", ms.Values.Where(v => string.IsNullOrWhiteSpace(v.Value)).Select(v => v.Key))}] was empty");
    }
}
