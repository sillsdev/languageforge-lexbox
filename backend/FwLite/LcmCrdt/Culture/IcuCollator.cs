using System.Collections;
using System.Globalization;
using SIL.WritingSystems;

namespace LcmCrdt.Culture;

/// <summary>
/// Adapts an icu.net collator (locale or custom rules) to <see cref="ICollator"/>.
/// </summary>
internal sealed class IcuCollator : ICollator
{
    private readonly Icu.Collation.Collator _collator;

    private IcuCollator(Icu.Collation.Collator collator) => _collator = collator;

    public static IcuCollator FromLocale(string locale)
    {
        IcuInit.EnsureInitialized();
        return new(Icu.Collation.Collator.Create(locale, Icu.Collation.Collator.Fallback.FallbackAllowed));
    }

    public static IcuCollator FromRules(string rules)
    {
        IcuInit.EnsureInitialized();
        return new(new Icu.Collation.RuleBasedCollator(rules));
    }

    public int Compare(string? x, string? y) =>
        _collator.Compare(x ?? string.Empty, y ?? string.Empty);

    public SortKey GetSortKey(string source) =>
        throw new NotSupportedException("ICU sort keys are not used by FwLite collation.");

    int IComparer.Compare(object? x, object? y) => Compare(x as string, y as string);
}
