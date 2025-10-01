using System.Text.Json;
using Soenneker.Utils.AutoBogus.Config;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Generators;
using Soenneker.Utils.AutoBogus.Override;
using SystemTextJsonPatch;

namespace MiniLcm.Tests.AutoFakerHelpers;

public static class AutoFakerDefault
{
    public static readonly AutoFakerConfig Config = MakeConfig();

    public static AutoFakerConfig MakeConfig(string[]? validWs = null, int repeatCount = 5, bool minimalRichSpans = false)
    {
        return new AutoFakerConfig()
        {
            RepeatCount = repeatCount,
            SkipPaths = [
                // I opened an issue, because I would expect this to be WritingSystem instead of string[]
                // https://github.com/soenneker/soenneker.utils.autobogus/issues/831
                $"{typeof(string[]).FullName}.{nameof(WritingSystem.LatinExemplars)}"
            ],
            Overrides =
            [
                new MultiStringOverride(validWs),
                new RichMultiStringOverride(validWs),
                new RichSpanOverride(minimalRichSpans),
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
                }, true),
                new PredicateOverride<MorphType>(morph =>
                {
                    // these values map to null and get replaced with MorphType.Stem so they're no round-tripped
                    return morph is not MorphType.Unknown and not MorphType.Other;
                }, true),
                new SimpleGenericOverride(typeof(JsonPatchDocument<>), context =>
                {
                    context.Instance = Activator.CreateInstance(context.GenerateType.Type!)!;
                }, false),
                new SimpleOverride<JsonSerializerOptions>(context =>
                {
                    var typeName = context.GenerateType.Type?.FullName;
                    throw new InvalidOperationException(
                        $"You should not be generating JsonSerializerOptions. You're probably didn't intend to generate an instance of the current type: {typeName}.");
                }, false)
            ]
        };
    }

    private class PredicateOverride<T>(Func<T, bool> predicate, bool preInit = false) : AutoFakerOverride<T>
    {
        public override bool Preinitialize { get; } = preInit;

        public override void Generate(AutoFakerOverrideContext context)
        {
            var value = context.Instance;
            while (value is not T instance || !predicate(instance))
            {
                value = context.AutoFaker.Generate<T>();
            }
            context.Instance = value;
        }
    }
}

public class SimpleOverride<T>(Action<AutoFakerOverrideContext> execute, bool preInit = false) : AutoFakerOverride<T>
{
    public override bool Preinitialize { get; } = preInit;

    public override void Generate(AutoFakerOverrideContext context)
    {
        execute(context);
    }
}

public class SimpleGenericOverride(Type genericTypeDefinition, Action<AutoFakerOverrideContext> execute, bool preInit = false) : AutoFakerGeneratorOverride
{
    public override bool Preinitialize { get; } = preInit;

    public override bool CanOverride(AutoFakerContext context)
    {
        return context.GenerateType.IsGenericType && context.GenerateType.GetGenericTypeDefinition() == genericTypeDefinition;
    }

    public override void Generate(AutoFakerOverrideContext context)
    {
        execute(context);
    }
}
