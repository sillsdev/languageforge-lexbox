using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MiniLcm.Culture;
using MiniLcm.Models;
using SIL.WritingSystems;

namespace LcmCrdt.Culture;

public class WritingSystemCollatorProvider(
    IMemoryCache cache,
    IMiniLcmCultureProvider cultureProvider,
    ILogger<WritingSystemCollatorProvider> logger) : IWritingSystemCollatorProvider
{
    public ICollator GetCollator(WritingSystem writingSystem)
    {
        return cache.GetOrCreate(CacheKey(writingSystem), entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return CreateCollator(writingSystem);
        }) ?? CreateCollator(writingSystem);
    }

    private ICollator CreateCollator(WritingSystem writingSystem)
    {
        if (!string.IsNullOrEmpty(writingSystem.SystemCollationLocale))
        {
            return TryCreateLocaleCollator(writingSystem);
        }

        if (!string.IsNullOrEmpty(writingSystem.IcuCollationRules))
        {
            return TryCreateRulesCollator(writingSystem);
        }

        return new LegacyCompareInfoCollator(cultureProvider.GetCompareInfo(writingSystem));
    }

    private ICollator TryCreateLocaleCollator(WritingSystem writingSystem)
    {
        try
        {
            return new Icu4NLocaleCollator(writingSystem.SystemCollationLocale!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to create ICU4N locale collator for '{Locale}' on writing system '{WsId}'; using .NET collation fallback",
                writingSystem.SystemCollationLocale,
                writingSystem.WsId);
            return CreateCultureCollator(writingSystem.SystemCollationLocale!);
        }
    }

    private ICollator TryCreateRulesCollator(WritingSystem writingSystem)
    {
        try
        {
            return new Icu4NRulesCollator(writingSystem.IcuCollationRules!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to create ICU4N rules collator for writing system '{WsId}'; using legacy collation fallback",
                writingSystem.WsId);
            return new LegacyCompareInfoCollator(cultureProvider.GetCompareInfo(writingSystem));
        }
    }

    private ICollator CreateCultureCollator(string locale)
    {
        try
        {
            return new CultureCompareInfoCollator(CultureInfo.GetCultureInfo(locale).CompareInfo);
        }
        catch (CultureNotFoundException ex)
        {
            logger.LogWarning(ex, "Unknown system collation locale '{Locale}'; using invariant collation", locale);
            return new CultureCompareInfoCollator(CultureInfo.InvariantCulture.CompareInfo);
        }
    }

    private static string CacheKey(WritingSystem writingSystem) =>
        $"collator|{writingSystem.WsId}|{writingSystem.Type}|{writingSystem.IcuCollationRules}|{writingSystem.SystemCollationLocale}";
}
