using FluentValidation;
using MiniLcm.Models;

namespace MiniLcm.Validators;

internal static class MultiStringValidator
{
    public static IRuleBuilderOptions<T, MultiString> Required<T>(this IRuleBuilder<T, MultiString> ruleBuilder, Func<T, string> getParentId)
    {
        return ruleBuilder.NotEmpty()
            .WithMessage((parent, ms) => $"MultiString must not be empty ({getParentId(parent)})")
            .NoEmptyValues(getParentId);
    }
    public static IRuleBuilderOptions<T, MultiString> NoEmptyValues<T>(this IRuleBuilder<T, MultiString> ruleBuilder, Func<T, string> getParentId)
    {
        return ruleBuilder.Must(ms => ms.Values.All(v => !string.IsNullOrEmpty(v.Value))).WithMessage((parent, ms) =>
            $"MultiString must not contain empty values, but [{string.Join(", ", ms.Values.Where(v => string.IsNullOrWhiteSpace(v.Value)).Select(v => v.Key))}] was empty ({getParentId(parent)})");
    }

    public static IRuleBuilderOptions<T, RichMultiString> Required<T>(this IRuleBuilder<T, RichMultiString> ruleBuilder, Func<T, string> getParentId)
    {
        return ruleBuilder.NotEmpty()
            .WithMessage((parent, ms) => $"RichMultiString must not be empty ({getParentId(parent)})")
            .NoEmptyValues(getParentId);
    }

    public static IRuleBuilderOptions<T, RichMultiString> NoEmptyValues<T>(this IRuleBuilder<T, RichMultiString> ruleBuilder, Func<T, string> getParentId)
    {
        return ruleBuilder.Must(ms => ms.All(v => !string.IsNullOrEmpty(v.Value))).WithMessage((parent, ms) =>
            $"RichMultiString must not contain empty values, but [{string.Join(", ", ms.Where(v => string.IsNullOrWhiteSpace(v.Value)).Select(v => v.Key))}] was empty ({getParentId(parent)})");
    }
}
