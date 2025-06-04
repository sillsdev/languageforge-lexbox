using System.Globalization;

namespace MiniLcm.Culture;

public static class StringExtensions
{
    public static bool Contains(this string str, string value, CultureInfo cultureInfo, CompareOptions comparison = CompareOptions.None)
    {
        return cultureInfo.CompareInfo.IndexOf(str, value, comparison) >= 0;
    }

    public static bool Equals(this string str,
        string value,
        CultureInfo cultureInfo,
        CompareOptions comparison = CompareOptions.None)
    {
        return cultureInfo.CompareInfo.Compare(str, value, comparison) == 0;
    }
}
