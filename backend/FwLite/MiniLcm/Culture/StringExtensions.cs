using System.Globalization;
using System.Text;

namespace MiniLcm.Culture;

public static class StringExtensions
{
    /// <summary>
    /// Checks if <paramref name="str"/> contains <paramref name="search"/>, ignoring case.
    /// Diacritics are also ignored unless the search string itself contains diacritics.
    /// </summary>
    public static bool ContainsDiacriticMatch(this string str, string search)
    {
        var options = DiacriticMatchOptions(search);
        return CultureInfo.InvariantCulture.CompareInfo.Contains(str, search, options);
    }

    /// <summary>
    /// Checks if <paramref name="str"/> starts with <paramref name="search"/>, ignoring case.
    /// Diacritics are also ignored unless the search string itself contains diacritics.
    /// </summary>
    public static bool StartsWithDiacriticMatch(this string str, string search)
    {
        var options = DiacriticMatchOptions(search);
        return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(str, search, options);
    }

    private static bool Contains(this CompareInfo compareInfo, string source, string value, CompareOptions options)
    {
        return compareInfo.IndexOf(source, value, options) >= 0;
    }

    private static CompareOptions DiacriticMatchOptions(string search)
    {
        return ContainsDiacritic(search)
            ? CompareOptions.IgnoreCase
            : CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
    }

    public static bool ContainsDiacritic(string value)
    {
        bool hasAccent = false;
        foreach (var ch in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                hasAccent = true;
                break;
            }
        }
        return hasAccent;
    }

    public static bool Equals(this string str,
        string value,
        CultureInfo cultureInfo,
        CompareOptions comparison = CompareOptions.None)
    {
        return cultureInfo.CompareInfo.Compare(str, value, comparison) == 0;
    }
}
