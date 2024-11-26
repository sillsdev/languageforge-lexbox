using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

internal static class MultiStringValidator
{
    public static IRuleBuilderOptions<T, MultiString> ValidMultiString<T>(this IRuleBuilder<T, MultiString> ruleBuilder)
    {
        return ruleBuilder.NotEmpty();
    }
}
