using System.Collections;
using System.Globalization;
using ICU4N.Text;
using SIL.WritingSystems;

namespace LcmCrdt.Culture;

internal sealed class Icu4NLocaleCollator : ICollator
{
    private readonly Collator _collator;

    public Icu4NLocaleCollator(string locale) =>
        _collator = Collator.GetInstance(CultureInfo.GetCultureInfo(locale));

    public int Compare(string? x, string? y) =>
        _collator.Compare(x ?? string.Empty, y ?? string.Empty);

    public SortKey GetSortKey(string source) =>
        throw new NotSupportedException("ICU4N sort keys are not used by FwLite collation.");

    int IComparer.Compare(object? x, object? y) => Compare(x as string, y as string);
}
