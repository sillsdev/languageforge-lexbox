using Microsoft.Extensions.Caching.Memory;
using MiniLcm.Culture;
using MiniLcm.Models;
using SIL.WritingSystems;

namespace LcmCrdt.Culture;

public class WritingSystemCollatorProvider(
    IMemoryCache cache,
    IMiniLcmCultureProvider cultureProvider) : IWritingSystemCollatorProvider
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
            return new SystemCollator(writingSystem.SystemCollationLocale);
        }

        if (!string.IsNullOrEmpty(writingSystem.IcuCollationRules))
        {
            WritingSystemCollationInit.EnsureInitialized();
            return new IcuRulesCollator(writingSystem.IcuCollationRules);
        }

        // FLEx default ordering also lands here until we choose to match its empty-rule ICU default.
        return new LegacyCompareInfoCollator(cultureProvider.GetCompareInfo(writingSystem));
    }

    private static string CacheKey(WritingSystem writingSystem) =>
        $"collator|{writingSystem.WsId}|{writingSystem.Type}|{writingSystem.IcuCollationRules}|{writingSystem.SystemCollationLocale}";
}
