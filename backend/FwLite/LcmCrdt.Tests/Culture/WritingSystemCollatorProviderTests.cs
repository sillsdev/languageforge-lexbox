using LcmCrdt.Culture;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Culture;
using MiniLcm.Models;
using SIL.WritingSystems;

namespace LcmCrdt.Tests.Culture;

public class WritingSystemCollatorProviderTests
{
    private static IWritingSystemCollatorProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<IMiniLcmCultureProvider, LcmCrdtCultureProvider>();
        services.AddSingleton<IWritingSystemCollatorProvider, WritingSystemCollatorProvider>();
        return services.BuildServiceProvider().GetRequiredService<IWritingSystemCollatorProvider>();
    }

    private static WritingSystem BaseWs() => new()
    {
        Id = Guid.NewGuid(),
        WsId = "en",
        Name = "English",
        Abbreviation = "En",
        Font = "Arial",
        Type = WritingSystemType.Vernacular,
    };

    [Fact]
    public void GetCollator_UsesIcuCollator_WhenLocaleSet()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { SystemCollationLocale = "de" });
        collator.Should().BeOfType<IcuCollator>();
    }

    [Fact]
    public void GetCollator_UsesIcuCollator_WhenRulesSet()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { IcuCollationRules = "&a < b" });
        collator.Should().BeOfType<IcuCollator>();
    }

    [Fact]
    public void GetCollator_UsesLegacyFallback_WhenCollationUnset()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs());
        collator.Should().BeOfType<LegacyCompareInfoCollator>();
    }

    [Fact]
    public void IcuRulesCollator_SortsByCustomRules()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { IcuCollationRules = "&z < a" });
        collator.Compare("z", "a").Should().BeLessThan(0);
    }

    [Fact]
    public void IcuRulesCollator_ReversesAAndB()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { IcuCollationRules = "&b < a &B < A" });
        collator.Compare("Banane", "Apfel").Should().BeLessThan(0);
    }

    [Fact]
    public void IcuLocaleCollator_SortsByLocale()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { SystemCollationLocale = "sv" });
        // Swedish sorts ä after z
        collator.Compare("ä", "z").Should().BeGreaterThan(0);
    }

    [Fact]
    public void LegacyCollator_PrefersLowercaseOnCaseInsensitiveTie()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs());
        collator.Compare("Ab", "ab").Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetCollator_FallsBackToLegacy_WhenRulesInvalid()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { IcuCollationRules = "not valid icu rules <<<<" });
        collator.Should().BeOfType<LegacyCompareInfoCollator>();
    }
}
