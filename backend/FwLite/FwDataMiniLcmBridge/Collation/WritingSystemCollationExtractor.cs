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
            // Custom ICU and Custom Simple both compile to CollationRules
            RulesCollationDefinition rules when !string.IsNullOrEmpty(rules.CollationRules)
                => (rules.CollationRules, null),
            // FLEx "Default Ordering" is an IcuRulesCollationDefinition with empty rules, i.e. ICU root.
            // We map it to null (FwLite's .NET culture fallback) rather than storing an empty rule set.
            // https://github.com/sillsdev/libpalaso/blob/32ca8a0a/SIL.Windows.Forms.WritingSystems/WritingSystemSetupModel.cs#L43-L47
            _ => (null, null)
        };
    }

    private static bool IsOwnLanguage(SystemCollationDefinition sys, CoreWritingSystemDefinition ws) =>
        string.IsNullOrEmpty(sys.LanguageTag) || string.Equals(sys.LanguageTag, ws.LanguageTag, StringComparison.OrdinalIgnoreCase);
}
