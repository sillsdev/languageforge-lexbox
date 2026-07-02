using System.Collections;
using System.Globalization;
using SIL.WritingSystems;

namespace LcmCrdt.Culture;

/// <summary>
/// Pre-collation-import fallback: case-insensitive compare with lowercase-before-uppercase tie-break.
/// </summary>
internal sealed class LegacyCompareInfoCollator(CompareInfo compareInfo) : ICollator
{
    public int Compare(string? x, string? y)
    {
        x ??= string.Empty;
        y ??= string.Empty;
        var caseInsensitiveResult = compareInfo.Compare(x, y, CompareOptions.IgnoreCase);
        if (caseInsensitiveResult != 0)
        {
            return caseInsensitiveResult;
        }

        return compareInfo.Compare(x, y, CompareOptions.None);
    }

    public SortKey GetSortKey(string source) => compareInfo.GetSortKey(source);

    int IComparer.Compare(object? x, object? y) => Compare(x as string, y as string);
}
