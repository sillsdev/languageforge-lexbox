using Soenneker.Utils.AutoBogus.Config;

namespace MiniLcm.Tests.AutoFakerHelpers;

public static class AutoFakerDefault
{
    public static readonly AutoFakerConfig Config = MakeConfig();

    public static AutoFakerConfig MakeConfig(string[]? validWs = null, int repeatCount = 5)
    {
        return new AutoFakerConfig()
        {
            RepeatCount = repeatCount,
            Overrides =
            [
                new MultiStringOverride(validWs),
                new RichMultiStringOverride(validWs),
                new WritingSystemIdOverride(validWs),
                new ObjectWithIdOverride(),
                new OrderableOverride(),
            ]
        };
    }
}
