using System.Globalization;
using System.Text;

namespace MiniLcm.Culture;

public static class StringExtensions
{
    public static bool Contains(this string str, string value, CultureInfo cultureInfo, CompareOptions comparison = CompareOptions.None)
    {
        return cultureInfo.CompareInfo.IndexOf(str, value, comparison) >= 0;
    }

    /// <summary>
    /// searches a string for a match ignoring diacritics, but only when the search string does not contain diacritics
    /// </summary>
    /// <param name="str">source of the search</param>
    /// <param name="search">string to search for</param>
    public static bool ContainsDiacriticMatch(this string str, string search)
    {
        if (ContainsDiacritic(search))
        {
            return Contains(str, search, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
        }

        return Contains(str,
            search,
            CultureInfo.InvariantCulture,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
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
