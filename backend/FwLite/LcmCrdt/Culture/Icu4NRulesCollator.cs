using System.Collections;
using System.Globalization;
using ICU4N.Text;
using SIL.WritingSystems;

namespace LcmCrdt.Culture;

internal sealed class Icu4NRulesCollator : ICollator
{
    private readonly RuleBasedCollator _collator;

    public Icu4NRulesCollator(string rules) => _collator = new RuleBasedCollator(rules);

    public int Compare(string? x, string? y) =>
        _collator.Compare(x ?? string.Empty, y ?? string.Empty);

    public SortKey GetSortKey(string source) =>
        throw new NotSupportedException("ICU4N sort keys are not used by FwLite collation.");

    int IComparer.Compare(object? x, object? y) => Compare(x as string, y as string);
}
