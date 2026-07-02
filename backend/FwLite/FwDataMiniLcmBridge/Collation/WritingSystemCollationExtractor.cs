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
            SystemCollationDefinition sys => (null, sys.LanguageTag),
            RulesCollationDefinition rules when !string.IsNullOrEmpty(rules.CollationRules)
                => (rules.CollationRules, null),
            _ => (null, null)
        };
    }
}
