using System.Reflection;
using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using MiniLcm;
using MiniLcm.Models;
using Reinforced.Typings;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Fluent;

namespace FwLiteShared.TypeGen;

public static class ReinforcedFwLiteTypingConfig
{
    public static void Configure(ConfigurationBuilder builder)
    {
        builder.Global(c => c.AutoAsync().UseModules());
        typeof(ExportContext).GetProperty(nameof(ExportContext.FileOperations))?.SetValue(builder.Context, new EsLintDisableFileOperation()
        {
            Context = builder.Context
        });
        builder.Substitute(typeof(WritingSystemId), new RtSimpleTypeName("string"));
        builder.Substitute(typeof(Guid), new RtSimpleTypeName("string"));
        builder.Substitute(typeof(DateTimeOffset), new RtSimpleTypeName("string"));
        //todo generate a multistring type rather than just substituting it everywhere
        builder.Substitute(typeof(MultiString),
            new RtDictionaryType(new RtSimpleTypeName("string"), new RtSimpleTypeName("string")));
        builder.ExportAsInterfaces([
                typeof(Entry),
                typeof(Sense),
                typeof(ExampleSentence),
                typeof(WritingSystem),
                typeof(WritingSystems),
                typeof(PartOfSpeech),
                typeof(SemanticDomain),
                typeof(ComplexFormType),
                typeof(ComplexFormComponent),
            ],
            exportBuilder => exportBuilder.WithPublicProperties());
        builder.ExportAsEnum<WritingSystemType>();
        builder.ExportAsInterface<IMiniLcmApi>().FlattenHierarchy().WithPublicProperties().WithPublicMethods();
        builder.ExportAsEnum<SortField>();
        builder.ExportAsInterfaces([typeof(QueryOptions), typeof(SortOptions), typeof(ExemplarOptions)],
            exportBuilder => exportBuilder.WithProperties(BindingFlags.Public | BindingFlags.Instance));
        builder.ExportAsInterface<FwLiteProvider>().WithPublicMethods();

        builder.ExportAsInterface<CombinedProjectsService>().WithPublicMethods();
        builder.ExportAsInterface<ProjectModel>().WithPublicProperties();
        builder.ExportAsInterface<ServerProjects>().WithPublicProperties();
        builder.ExportAsInterface<LexboxServer>().WithPublicProperties();
        builder.SubstituteGeneric(typeof(IAsyncEnumerable<>),
            (type, resolver) =>
            {
                return new RtAsyncType(
                    new RtArrayType(resolver.ResolveTypeName(type.GenericTypeArguments[0]))
                );
            });
    }

    internal class EsLintDisableFileOperation : FilesOperations
    {
        protected override void ExportCore(StreamWriter tw, ExportedFile file)
        {
            tw.WriteLine("/* eslint-disable */");
            base.ExportCore(tw, file);
            tw.WriteLine("/* eslint-enable */");
        }
    }
}
