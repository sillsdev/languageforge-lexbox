using FwLiteShared.Services;
using MiniLcm;
using MiniLcm.Models;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Fluent;
using TypeGen.Core.SpecGeneration;

namespace FwLiteShared.TypeGen;

public class FwLiteTypeGenSpec: GenerationSpec
{
    public FwLiteTypeGenSpec()
    {
        AddClass<IMiniLcmApi>();
        AddInterface<FwLiteProvider>();
    }

    public override void OnBeforeGeneration(OnBeforeGenerationArgs args)
    {
        args.GeneratorOptions.CustomTypeMappings.Add(typeof(WritingSystemId).FullName!, "string");
    }
}

public static class ReinforcedFwLiteTypingConfig
{
    public static void Configure(ConfigurationBuilder builder)
    {
        builder.Substitute(typeof(WritingSystemId), new RtSimpleTypeName("string"));
        builder.Substitute(typeof(Guid), new RtSimpleTypeName("string"));
        builder.Substitute(typeof(DateTimeOffset), new RtSimpleTypeName("string"));
        builder.Substitute(typeof(MultiString), new RtDictionaryType(new RtSimpleTypeName("string"), new RtSimpleTypeName("string")));
        builder.ExportAsInterfaces([
            typeof(Entry),
            typeof(Sense),
            typeof(ExampleSentence),
            typeof(WritingSystem),
            typeof(PartOfSpeech),
            typeof(SemanticDomain),
            typeof(ComplexFormType),
            typeof(ComplexFormComponent),
        ], exportBuilder => exportBuilder.WithPublicProperties());
        builder.ExportAsInterface<IMiniLcmApi>().FlattenHierarchy().WithPublicProperties().WithPublicMethods();
        builder.ExportAsInterface<FwLiteProvider>().WithPublicMethods();
    }
}
