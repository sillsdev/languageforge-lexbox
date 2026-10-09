using FwDataMiniLcmBridge.Collation;
using FwDataMiniLcmBridge.LcmUtils;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;

namespace FwDataMiniLcmBridge.Tests.Collation;

public class WritingSystemCollationExtractorTests
{
    // Validating a collation needs SLDR and ICU
    public WritingSystemCollationExtractorTests() => ProjectLoader.Init();

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

    [Fact]
    public void DefaultOrdering_IsNull()
    {
        WritingSystemCollationExtractor.Extract(Ws("es", new IcuRulesCollationDefinition("standard")))
            .Should().Be(((string?)null, (string?)null));
    }

    [Fact]
    public void CustomSimpleRules_StoresCompiledRules()
    {
        var (rules, locale) = WritingSystemCollationExtractor.Extract(Ws("es", new SimpleRulesCollationDefinition("standard") { SimpleRules = "b B\na A" }));
        locale.Should().BeNull();
        rules.Should().NotBeNullOrEmpty();
        rules!.IndexOf('b').Should().BeLessThan(rules.IndexOf('a'), "simple rules list b before a");
    }
}
