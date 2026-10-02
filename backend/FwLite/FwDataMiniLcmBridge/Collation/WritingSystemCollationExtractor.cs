using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;

namespace FwDataMiniLcmBridge.Collation;

public static class WritingSystemCollationExtractor
{
    public static (string? IcuCollationRules, string? SystemCollationLocale) Extract(CoreWritingSystemDefinition ws)
    {
        var cd = ws.DefaultCollation;
        cd.Validate(out _);

        return cd switch
        {
            // liblcm/libpalaso default a writing system without collation to its own language's system collation
            SystemCollationDefinition sys when IsOwnLanguage(sys, ws) => (null, null),
            SystemCollationDefinition sys => (null, sys.LanguageTag),
            RulesCollationDefinition rules when !string.IsNullOrEmpty(rules.CollationRules)
                => (rules.CollationRules, null),
            _ => (null, null)
        };
    }

    private static bool IsOwnLanguage(SystemCollationDefinition sys, CoreWritingSystemDefinition ws) =>
        string.IsNullOrEmpty(sys.LanguageTag) || string.Equals(sys.LanguageTag, ws.LanguageTag, StringComparison.OrdinalIgnoreCase);
}
