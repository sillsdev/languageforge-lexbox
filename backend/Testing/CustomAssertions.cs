using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace Testing;

public static class CustomAssertions
{
    public static T ShouldNotBeNull<T>([NotNull] this T? actual, string? customMessage = null)
        where T : class
    {
        actual.Should().NotBeNull(customMessage);
        return actual ?? throw new InvalidOperationException("ShouldNotBeNull failed");
    }

    public static T ShouldNotBeNull<T>([NotNull] this T? actual, string? customMessage = null)
        where T : struct
    {
        actual.Should().NotBeNull(customMessage);
        return actual ?? throw new InvalidOperationException("ShouldNotBeNull failed");
    }

    public static void ShouldNotBeNullOrEmpty<T>([NotNull] this IEnumerable<T>? actual, string? customMessage = null)
    {
        actual.ShouldNotBeNull(customMessage);
        actual.Should().NotBeEmpty(customMessage);
    }

    public static T ShouldBeOfType<T>([NotNull] this object? actual, string? customMessage = null)
    {
        actual.ShouldNotBeNull(customMessage);
        actual.Should().BeOfType<T>(customMessage);
        return (T)actual;
    }
}
