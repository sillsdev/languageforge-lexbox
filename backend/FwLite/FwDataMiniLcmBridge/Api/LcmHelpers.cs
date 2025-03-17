using System.Globalization;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace FwDataMiniLcmBridge.Api;

internal static class LcmHelpers
{
    internal static bool SearchValue(this ITsMultiString multiString, string value)
    {
        for (var i = 0; i < multiString.StringCount; i++)
        {
            var tsString = multiString.GetStringFromIndex(i, out var _);
            if (string.IsNullOrEmpty(tsString.Text)) continue;
            if (tsString.Text.Contains(value, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    internal static readonly char[] WhitespaceChars =
    [
        '\u0009', // Tab
        '\u000A', // Line Feed
        '\u000D', // Carriage Return
        '\u0020', // Space
        '\u00A0', // Non-breaking Space
        '\u2002', // En Space
        '\u2003', // Em Space
        '\u2004', // Three-Per-Em Space
        '\u2005', // Four-Per-Em Space
        '\u2006', // Six-Per-Em Space
        '\u2007', // Figure Space
        '\u2008', // Punctuation Space
        '\u2009', // Thin Space
        '\u200A', // Hair Space
        '\u200B', // Zero Width Space
        '\u200C', // Zero Width Non-Joiner
        '\u200D', // Zero Width Joiner
        '\u200E', // Left-to-Right Mark
        '\u200F',  // Right-to-Left Mark
        '\u2028', // Line Separator
        '\u2029', // Paragraph Separator
        '\u202F', // Narrow No-Break Space
        '\u205F', // Medium Mathematical Space
        '\u3000',  // Ideographic Space
        '\uFEFF', // Zero Width No-Break Space / BOM
    ];

    internal static readonly char[] WhitespaceAndFormattingChars =
    [
        .. WhitespaceChars,
        '\u0640', // Arabic Tatweel
    ];

    internal static void ContributeExemplars(ITsMultiString multiString, IReadOnlyDictionary<int, HashSet<char>> wsExemplars)
    {
        for (var i = 0; i < multiString.StringCount; i++)
        {
            var tsString = multiString.GetStringFromIndex(i, out var ws);
            if (string.IsNullOrEmpty(tsString.Text)) continue;
            var value = tsString.Text.AsSpan().Trim(WhitespaceAndFormattingChars);
            if (!value.IsEmpty && wsExemplars.TryGetValue(ws, out var exemplars))
            {
                //some cases the first character is not a prefix.
                if (CultureInfo.InvariantCulture.CompareInfo.IsPrefix(value, value[0..1], CompareOptions.IgnoreCase))
                    exemplars.Add(char.ToUpperInvariant(value[0]));
            }
        }
    }

    internal static IMoStemAllomorph CreateLexemeForm(this LcmCache cache)
    {
        return cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
    }
}
