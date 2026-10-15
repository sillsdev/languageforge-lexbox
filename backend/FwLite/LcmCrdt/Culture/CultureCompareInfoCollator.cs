using System.Collections;
using System.Globalization;
using SIL.WritingSystems;

namespace LcmCrdt.Culture;

/// <summary>
/// Locale collation via .NET CompareInfo, without the legacy case tie-break.
/// </summary>
internal sealed class CultureCompareInfoCollator(CompareInfo compareInfo) : ICollator
{
    public int Compare(string? x, string? y) =>
        compareInfo.Compare(x ?? string.Empty, y ?? string.Empty, CompareOptions.None);

    public SortKey GetSortKey(string source) => compareInfo.GetSortKey(source);

    int IComparer.Compare(object? x, object? y) => Compare(x as string, y as string);
}
