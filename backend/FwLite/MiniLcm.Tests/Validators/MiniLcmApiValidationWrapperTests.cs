using FluentValidation;
using MiniLcm.Validators;
using Moq;

namespace MiniLcm.Tests.Validators;

public class MiniLcmApiValidationWrapperTests
{
    /// <summary>The wrapper is hand-written, so a forgotten ValidateAndThrow compiles fine.</summary>
    [Fact]
    public async Task EveryWriteValidatesTheTypesWeHaveValidatorsFor()
    {
        var validatable = ValidatableTypes();
        validatable.Should().NotBeEmpty("otherwise no validators were found and this checks nothing");

        var inner = new Mock<IMiniLcmApi>();
        var api = new MiniLcmApiValidationWrapper(inner.Object, RejectingValidators());

        var checkedMethods = new List<string>();
        var failures = new List<string>();
        foreach (var method in typeof(IMiniLcmWriteApi).GetMethods())
        {
            // The parameter the method is expected to validate: "after" for before/after pairs, else the only one.
            var target = method.GetParameters().Where(p => validatable.Contains(p.ParameterType))
                .OrderByDescending(p => p.Name == "after").FirstOrDefault();
            if (target is null) continue;
            checkedMethods.Add(method.Name);

            inner.Invocations.Clear();
            var args = method.GetParameters().Select(p => BuildArgument(p.ParameterType)).ToArray();
            // Only the validators throw this, so it arriving means the method ran the one for target's type.
            var thrown = await Record.ExceptionAsync(() => (Task)method.Invoke(api, args)!);

            if (thrown is not ValidationException)
            {
                failures.Add($"{method.Name} did not validate its '{target.Name}' parameter " +
                    $"(got {thrown?.GetType().Name ?? "no exception"})");
            }
            else if (inner.Invocations.Any(i => i.Method.Name == method.Name))
            {
                failures.Add($"{method.Name} validated but still wrote to the inner api");
            }
        }

        checkedMethods.Should().Contain([nameof(IMiniLcmWriteApi.CreateEntry), nameof(IMiniLcmWriteApi.SubmitCreateSense)],
            "otherwise this test isn't testing anything");
        // Most validated types are taken by a create, an update and a before/after overload.
        checkedMethods.Should().HaveCountGreaterThan(validatable.Count, "otherwise the reflection stopped matching methods");
        failures.Should().BeEmpty();
    }

    /// <summary>Every T we have an AbstractValidator&lt;T&gt; for.</summary>
    private static HashSet<Type> ValidatableTypes()
    {
        return [.. typeof(MiniLcmValidators).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false })
            .Select(ValidatedTypeOf)
            .OfType<Type>()];
    }

    /// <summary>The T of the AbstractValidator&lt;T&gt; a type derives from, or null if it isn't one.</summary>
    private static Type? ValidatedTypeOf(Type type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AbstractValidator<>)) return t.GetGenericArguments()[0];
        }
        return null;
    }

    /// <summary>
    /// A MiniLcmValidators whose validators reject everything, so the test turns on whether the validator was
    /// called rather than on what each one considers invalid.
    /// </summary>
    private static MiniLcmValidators RejectingValidators()
    {
        var validators = typeof(MiniLcmValidators).GetConstructors().Single().GetParameters()
            .Select(p => Activator.CreateInstance(
                typeof(RejectingValidator<>).MakeGenericType(p.ParameterType.GetGenericArguments()[0])))
            .ToArray();
        return (MiniLcmValidators)Activator.CreateInstance(typeof(MiniLcmValidators), validators)!;
    }

    private sealed class RejectingValidator<T> : AbstractValidator<T>
    {
        public override Task<FluentValidation.Results.ValidationResult> ValidateAsync(ValidationContext<T> context,
            CancellationToken cancellation = default) => throw new ValidationException("rejected");
    }

    // Only needs to be constructible: the validators reject everything without reading it.
    private static object? BuildArgument(Type type)
    {
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type.IsValueType) return Activator.CreateInstance(type);
        return type.GetConstructor(Type.EmptyTypes) is null ? null : Activator.CreateInstance(type);
    }
}
