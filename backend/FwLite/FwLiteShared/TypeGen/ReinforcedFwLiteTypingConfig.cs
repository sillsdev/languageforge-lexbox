using System.Reflection;
using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using MiniLcm;
using MiniLcm.Models;
using Reinforced.Typings;
using Reinforced.Typings.Ast.Dependency;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Fluent;
using Reinforced.Typings.Visitors.TypeScript;

namespace FwLiteShared.TypeGen;

public static class ReinforcedFwLiteTypingConfig
{
    public static void Configure(ConfigurationBuilder builder)
    {
        builder.Global(c => c.AutoAsync()
            .UseModules()
            .UnresolvedToUnknown()
            .CamelCaseForProperties()
            .CamelCaseForMethods()
            .AutoOptionalProperties()
            .UseVisitor<TypedImportsVisitor>());
        DisableEsLintChecks(builder);
        builder.Substitute(typeof(WritingSystemId), new RtSimpleTypeName("string"));
        builder.Substitute(typeof(Guid), new RtSimpleTypeName("string"));
        builder.Substitute(typeof(DateTimeOffset), new RtSimpleTypeName("string"));
        //todo generate a multistring type rather than just substituting it everywhere
        builder.ExportAsThirdParty<MultiString>().WithName("IMultiString").Imports([new ()
        {
            From = "$lib/dotnet-types/i-multi-string",
            Target = "type {IMultiString}"
        }]);
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
        builder.ExportAsEnum<WritingSystemType>().UseString();
        builder.ExportAsInterface<IMiniLcmApi>().FlattenHierarchy().WithPublicProperties().WithPublicMethods(
            exportBuilder =>
            {
                var isUpdatePatchMethod = exportBuilder.Member.GetParameters()
                    .Any(p => p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == (typeof(UpdateObjectInput<>)));
                if (isUpdatePatchMethod)
                {
                    exportBuilder.Ignore();
                }
            });
        builder.ExportAsEnum<SortField>().UseString();
        builder.ExportAsInterfaces([typeof(QueryOptions), typeof(SortOptions), typeof(ExemplarOptions)],
            exportBuilder => exportBuilder.WithProperties(BindingFlags.Public | BindingFlags.Instance));
        builder.ExportAsInterface<FwLiteProvider>().WithPublicMethods();

        builder.ExportAsEnum<DotnetService>().UseString();
        builder.ExportAsInterface<AuthService>().WithPublicMethods();
        builder.ExportAsInterface<ImportFwdataService>().WithPublicMethods();
        builder.ExportAsInterface<ServerStatus>().WithPublicProperties();
        builder.ExportAsInterface<CombinedProjectsService>().WithPublicMethods();
        builder.ExportAsInterface<ProjectModel>().WithPublicProperties();
        builder.ExportAsInterface<ServerProjects>().WithPublicProperties();
        builder.ExportAsInterface<LexboxServer>().WithPublicProperties();
    }

    private static void DisableEsLintChecks(ConfigurationBuilder builder)
    {
        typeof(ExportContext).GetProperty(nameof(ExportContext.FileOperations))?.SetValue(builder.Context,
            new EsLintDisableFileOperation() { Context = builder.Context });
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

    internal class TypedImportsVisitor(TextWriter writer, ExportContext exportContext)
        : TypeScriptExportVisitor(writer, exportContext)
    {
        public override void Visit(RtImport node)
        {
            //change import, was:
            // import { IEntry} from './i-entry';
            //now:
            // import type {IEntry} from './i-entry';
            if (!node.Target.StartsWith("type "))
            {
                node.Target = $"type {node.Target.Replace("{ ", "{").Replace(" }", "}")}";
            }
            base.Visit(node);
        }
    }
}
