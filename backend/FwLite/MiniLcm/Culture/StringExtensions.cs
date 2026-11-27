using System.Globalization;
using System.Text;

namespace MiniLcm.Culture;

public static class StringExtensions
{
    /// <summary>
    /// Checks if <paramref name="str"/> contains <paramref name="search"/>, ignoring case.
    /// Diacritics are also ignored unless <paramref name="matchDiacritics"/> is set or the search string itself contains diacritics.
    /// </summary>
    public static bool ContainsDiacriticMatch(this string str, string search, bool matchDiacritics = false)
    {
        var options = DiacriticMatchOptions(search, matchDiacritics);
        return CultureInfo.InvariantCulture.CompareInfo.Contains(str, search, options);
    }

    /// <summary>
    /// Checks if <paramref name="str"/> starts with <paramref name="search"/>, ignoring case.
    /// Diacritics are also ignored unless <paramref name="matchDiacritics"/> is set or the search string itself contains diacritics.
    /// </summary>
    public static bool StartsWithDiacriticMatch(this string str, string search, bool matchDiacritics = false)
    {
        var options = DiacriticMatchOptions(search, matchDiacritics);
        return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(str, search, options);
    }

    private static bool Contains(this CompareInfo compareInfo, string source, string value, CompareOptions options)
    {
        return compareInfo.IndexOf(source, value, options) >= 0;
    }

    private static CompareOptions DiacriticMatchOptions(string search, bool matchDiacritics)
    {
        return matchDiacritics || ContainsDiacritic(search)
            ? CompareOptions.IgnoreCase
            : CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
    }

    public static bool ContainsDiacritic(string value)
    {
        // runes, not chars, so marks outside the BMP (e.g. Adlam) count too
        foreach (var rune in value.Normalize(NormalizationForm.FormD).EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark) return true;
        }
        return false;
    }

    public static bool Equals(this string str,
        string value,
        CultureInfo cultureInfo,
        CompareOptions comparison = CompareOptions.None)
    {
        return cultureInfo.CompareInfo.Compare(str, value, comparison) == 0;
    }
}
