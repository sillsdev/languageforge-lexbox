using FluentValidation;
using SystemTextJsonPatch;

namespace MiniLcm.Validators;

internal static class JsonPatchValidator
{
    public static IRuleBuilderOptionsConditions<UpdateObjectInput<T>, JsonPatchDocument<T>> DoesNotChangeProperty<T>(
        this IRuleBuilder<UpdateObjectInput<T>, JsonPatchDocument<T>> builder,
        string propertyName//todo handle this better
    ) where T : class
    {
        return builder.Custom((document, context) =>
        {
            if (document.Operations.Any(o =>
                    string.Equals(o.Path,
                        $"/{propertyName}",
                        StringComparison.InvariantCultureIgnoreCase)))
                context.AddFailure(propertyName, "Not allowed to update " + propertyName);
        });
    }

    /// <summary>
    /// Unlike <see cref="DoesNotChangeProperty{T}"/> this also rejects operations on paths
    /// under the property (e.g. "/Types/0"), for list properties that must not be patched by index.
    /// </summary>
    public static IRuleBuilderOptionsConditions<UpdateObjectInput<T>, JsonPatchDocument<T>> DoesNotChangePropertyOrChildren<T>(
        this IRuleBuilder<UpdateObjectInput<T>, JsonPatchDocument<T>> builder,
        string propertyName
    ) where T : class
    {
        return builder.Custom((document, context) =>
        {
            if (document.Operations.Any(o =>
                    string.Equals(o.Path, $"/{propertyName}", StringComparison.InvariantCultureIgnoreCase)
                    || (o.Path?.StartsWith($"/{propertyName}/", StringComparison.InvariantCultureIgnoreCase) ?? false)))
                context.AddFailure(propertyName, "Not allowed to update " + propertyName);
        });
    }

    public static IRuleBuilderOptionsConditions<UpdateObjectInput<T>, JsonPatchDocument<T>> DoesNotChangePropertyTo<T, V>(
        this IRuleBuilder<UpdateObjectInput<T>, JsonPatchDocument<T>> builder,
        string propertyName,
        V forbiddenValue,
        string message
    ) where T : class
    {
        return builder.Custom((document, context) =>
        {
            if (document.Operations.Any(o =>
                    string.Equals(o.Path, $"/{propertyName}", StringComparison.OrdinalIgnoreCase)
                    && Equals(o.Value, forbiddenValue)))
                context.AddFailure(propertyName, message);
        });
    }
}
