using FwDataMiniLcmBridge.Collation;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;

namespace FwDataMiniLcmBridge.Tests.Collation;

public class WritingSystemCollationExtractorTests
{
    private static CoreWritingSystemDefinition Ws(string tag, CollationDefinition collation) =>
        new(tag) { DefaultCollation = collation };

    [Fact]
    public void OwnLanguageSystemCollation_IsDefaultOrdering()
    {
        WritingSystemCollationExtractor.Extract(Ws("es", new SystemCollationDefinition { LanguageTag = "es" }))
            .Should().Be(((string?)null, (string?)null));
    }

    [Fact]
    public void OtherLanguageSystemCollation_StoresLocale()
    {
        WritingSystemCollationExtractor.Extract(Ws("es", new SystemCollationDefinition { LanguageTag = "fr" }))
            .Should().Be(((string?)null, "fr"));
    }

    [Fact]
    public void CustomIcuRules_StoresRules()
    {
        WritingSystemCollationExtractor.Extract(Ws("es", new IcuRulesCollationDefinition("standard") { IcuRules = "&b < a" }))
            .IcuCollationRules.Should().Contain("&b < a");
    }
}
