using Soenneker.Utils.AutoBogus.Config;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

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
                new ColorOverride(),
                //these are too complicated to generate, so we'll just use null for tests
                new SimpleOverride<RichTextObjectData>(context => context.Instance = null!),
                new SimpleOverride<PartOfSpeech>(context =>
                {
                    if (context.Instance is PartOfSpeech pos)
                    {
                        pos.Predefined = false;
                    }
                }, true),
                new SimpleOverride<SemanticDomain>(context =>
                {
                    if (context.Instance is SemanticDomain domain)
                    {
                        domain.Predefined = false;
                    }
                }, true)
            ]
        };
    }

    private class SimpleOverride<T>(Action<AutoFakerOverrideContext> execute, bool preInit = false) : AutoFakerOverride<T>
    {
        public override bool Preinitialize { get; } = preInit;

        public override void Generate(AutoFakerOverrideContext context)
        {
            execute(context);
        }
    }
}
