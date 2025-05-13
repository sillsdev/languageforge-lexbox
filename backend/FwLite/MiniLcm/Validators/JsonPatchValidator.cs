using FluentValidation;
using SystemTextJsonPatch;

namespace MiniLcm.Validators;

internal static class JsonPatchValidator
{
    public static IRuleBuilderOptionsConditions<UpdateObjectInput<T>, JsonPatchDocument<T>> NoOperation<T>(
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
}
