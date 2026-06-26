using FluentValidation;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

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

    public static IRuleBuilderOptionsConditions<UpdateObjectInput<T>, JsonPatchDocument<T>> DoesNotChangePropertyTo<T, V>(
        this IRuleBuilder<UpdateObjectInput<T>, JsonPatchDocument<T>> builder,
        string propertyName,
        V forbiddenValue,
        string message
    ) where T : class
    {
        return builder.Custom((document, context) =>
        {
            // Only ops that actually set the value (add/replace) can change the property to the forbidden value;
            // a non-mutating op like test must not trip this.
            if (document.Operations.Any(o =>
                    o.OperationType is OperationType.Add or OperationType.Replace
                    && string.Equals(o.Path, $"/{propertyName}", StringComparison.OrdinalIgnoreCase)
                    && Equals(o.Value, forbiddenValue)))
                context.AddFailure(propertyName, message);
        });
    }
}
