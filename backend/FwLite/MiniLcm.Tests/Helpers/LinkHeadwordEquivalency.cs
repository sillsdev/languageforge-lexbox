using FluentAssertions.Equivalency;

namespace MiniLcm.Tests.Helpers;

public static class LinkHeadwordEquivalency
{
    /// <summary>
    /// Compare complex form and component headwords without their homograph subscripts (bank₂),
    /// for tests whose entries share headwords, so the API's homograph numbering differs from the expectation's.
    /// </summary>
    public static EquivalencyOptions<T> IgnoringLinkHomographNumbers<T>(this EquivalencyOptions<T> options)
    {
        return options
            .Using<string>(ctx => WithoutHomographNumber(ctx.Subject).Should().Be(WithoutHomographNumber(ctx.Expectation)))
            .When(info => info.Path.EndsWith($".{nameof(ComplexFormComponent.ComponentHeadword)}")
                || info.Path.EndsWith($".{nameof(ComplexFormComponent.ComplexFormHeadword)}"));
    }

    private static string? WithoutHomographNumber(string? headword) =>
        headword?.TrimEnd('₀', '₁', '₂', '₃', '₄', '₅', '₆', '₇', '₈', '₉');
}
