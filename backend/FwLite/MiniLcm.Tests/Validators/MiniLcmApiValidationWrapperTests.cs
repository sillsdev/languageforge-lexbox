using FluentValidation;
using MiniLcm.Validators;
using Moq;

namespace MiniLcm.Tests.Validators;

public class MiniLcmApiValidationWrapperTests
{
    /// <summary>
    /// Iterates every parameter of every IMiniLcmWriteApi method
    /// verifying that we validate it if we HAVE a validator for its type.
    ///
    /// Also verifies that the inner api is not called if validation fails.
    /// </summary>
    [Fact]
    public async Task EveryWriteValidatesItsValidatableParameters()
    {
        var validatable = ValidatableTypes();
        validatable.Should().NotBeEmpty("otherwise no validators were found and this checks nothing");

        var inner = new Mock<IMiniLcmApi>();

        var checkedMethods = new HashSet<string>();
        var failures = new List<string>();
        foreach (var method in typeof(IMiniLcmWriteApi).GetMethods())
        {
            // One run per validatable parameter, with validators keyed to reject only that parameter
            foreach (var target in method.GetParameters().Where(p => validatable.Contains(p.ParameterType)))
            {
                var args = method.GetParameters().Select(p => BuildArgument(p.ParameterType)).ToArray();
                var argToValidate = args[target.Position] ?? throw new InvalidOperationException($"couldn't build a {target.ParameterType.Name}");
                var validator = RejectingValidators(argToValidate);
                var api = new MiniLcmApiValidationWrapper(inner.Object, validator);

                inner.Invocations.Clear();
                var thrown = await Record.ExceptionAsync(() => (Task)method.Invoke(api, args)!);

                if (target.Name == "before")
                {
                    // The old state may legitimately be invalid; only "after" should get validated.
                    if (thrown is ValidationException)
                        failures.Add($"{method.Name} validated its 'before' parameter, which it shouldn't do");
                    continue;
                }

                checkedMethods.Add(method.Name);

                if (thrown is not ValidationException)
                {
                    failures.Add($"{method.Name} did not validate its '{target.Name}' parameter " +
                        $"(got {thrown?.GetType().Name ?? "no exception"})");
                }
                else if (inner.Invocations.Any(i => i.Method.Name == method.Name))
                {
                    failures.Add($"{method.Name} validated its '{target.Name}' parameter but still wrote to the inner api");
                }
            }
        }

        checkedMethods.Should().Contain([nameof(IMiniLcmWriteApi.CreateEntry), nameof(IMiniLcmWriteApi.SubmitCreateSense)],
            "we just want to make sure we're testing more than nothing");
        // Most validated types are taken by a create, an update and a before/after overload.
        checkedMethods.Should().HaveCountGreaterThan(validatable.Count, "otherwise the reflection stopped matching methods");
        failures.Should().BeEmpty();
    }

    /// <summary>Every T we have an AbstractValidator for.</summary>
    private static HashSet<Type> ValidatableTypes()
    {
        return [.. typeof(MiniLcmValidators).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false })
            .Select(ValidatedTypeOf)
            .OfType<Type>()];
    }

    /// <summary>The T of the AbstractValidator a type derives from, or null if it isn't one.</summary>
    private static Type? ValidatedTypeOf(Type type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AbstractValidator<>)) return t.GetGenericArguments()[0];
        }
        return null;
    }

    /// <summary>
    /// A MiniLcmValidators whose validators reject exactly the given instance and pass everything else,
    /// so the test turns on whether that instance was validated rather than on what each validator considers invalid.
    /// </summary>
    private static MiniLcmValidators RejectingValidators(object target)
    {
        var validators = typeof(MiniLcmValidators).GetConstructors().Single().GetParameters()
            .Select(p => Activator.CreateInstance(
                typeof(RejectingValidator<>).MakeGenericType(p.ParameterType.GetGenericArguments()[0]), target))
            .ToArray();
        return (MiniLcmValidators)Activator.CreateInstance(typeof(MiniLcmValidators), validators)!;
    }

    private sealed class RejectingValidator<T>(object target) : AbstractValidator<T>
    {
        public override Task<FluentValidation.Results.ValidationResult> ValidateAsync(ValidationContext<T> context,
            CancellationToken cancellation = default)
        {
            if (ReferenceEquals(context.InstanceToValidate, target)) throw new ValidationException("rejected");
            return Task.FromResult(new FluentValidation.Results.ValidationResult());
        }
    }

    // Only needs to be constructible: the validators reject by identity without reading it.
    private static object? BuildArgument(Type type)
    {
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(string)) return string.Empty;
        if (type.IsValueType) return Activator.CreateInstance(type);
        if (type.GetConstructor(Type.EmptyTypes) is not null) return Activator.CreateInstance(type);
        // Types without a parameterless constructor (e.g. LcmFileMetadata) are built via their fewest-arg
        // constructor, filling each parameter (defaults where available, otherwise recursively). Values don't
        // matter because the validators reject by reference identity, not by reading the instance.
        var ctor = type.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault();
        if (ctor is null) return null;
        var args = ctor.GetParameters()
            .Select(p => p.HasDefaultValue ? p.DefaultValue : BuildArgument(p.ParameterType))
            .ToArray();
        return ctor.Invoke(args);
    }
}
