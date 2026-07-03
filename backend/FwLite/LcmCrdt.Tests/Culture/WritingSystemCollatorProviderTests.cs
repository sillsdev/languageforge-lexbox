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
    public void GetCollator_UsesIcu4NLocaleCollator_WhenLocaleSet()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { SystemCollationLocale = "de" });
        collator.Should().BeOfType<Icu4NLocaleCollator>();
    }

    [Fact]
    public void GetCollator_UsesIcu4NRulesCollator_WhenRulesSet()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { IcuCollationRules = "&a < b" });
        collator.Should().BeOfType<Icu4NRulesCollator>();
    }

    [Fact]
    public void GetCollator_UsesLegacyFallback_WhenCollationUnset()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs());
        collator.Should().NotBeOfType<Icu4NLocaleCollator>();
        collator.Should().NotBeOfType<Icu4NRulesCollator>();
    }

    [Fact]
    public void Icu4NRulesCollator_SortsByCustomRules()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { IcuCollationRules = "&z < a" });
        collator.Compare("z", "a").Should().BeLessThan(0);
    }

    [Fact]
    public void Icu4NRulesCollator_ReversesAAndB()
    {
        var provider = CreateProvider();
        var collator = provider.GetCollator(BaseWs() with { IcuCollationRules = "&b < a &B < A" });
        collator.Compare("Banane", "Apfel").Should().BeLessThan(0);
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
        collator.Should().NotBeOfType<Icu4NRulesCollator>();
        collator.Should().BeOfType<LegacyCompareInfoCollator>();
    }
}
