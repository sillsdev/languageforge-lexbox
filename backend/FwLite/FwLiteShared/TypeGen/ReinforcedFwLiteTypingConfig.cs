﻿using System.Reflection;
using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using LcmCrdt;
using Microsoft.JSInterop;
using MiniLcm;
using MiniLcm.Models;
using Reinforced.Typings;
using Reinforced.Typings.Ast.Dependency;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Fluent;
using Reinforced.Typings.Visitors.TypeScript;

namespace FwLiteShared.TypeGen;

//docs https://github.com/reinforced/Reinforced.Typings/wiki/Fluent-configuration
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

        builder.Substitute(typeof(Guid), new RtSimpleTypeName("string"));
        builder.Substitute(typeof(Uri), new RtSimpleTypeName("string"));
        builder.Substitute(typeof(DateTimeOffset), new RtSimpleTypeName("string"));
        builder.SubstituteGeneric(typeof(ValueTask<>), (type, resolver) => resolver.ResolveTypeName(typeof(Task<>).MakeGenericType(type.GenericTypeArguments[0]), true));
        var dotnetObjectRefInterface = typeof(DotNetObjectReference<>).GetInterfaces().First();
        builder.SubstituteGeneric(typeof(DotNetObjectReference<>), (type, resolver) => resolver.ResolveTypeName(dotnetObjectRefInterface));
        builder.ExportAsThirdParty([dotnetObjectRefInterface],
            exportBuilder => exportBuilder.WithName("DotNet.DotNetObject").Imports([
                new() { From = "@microsoft/dotnet-js-interop", Target = "type {DotNet}" }
            ]));

        ConfigureMiniLcmTypes(builder);
        ConfigureFwLiteSharedTypes(builder);
    }

    private static void ConfigureMiniLcmTypes(ConfigurationBuilder builder)
    {
        builder.Substitute(typeof(WritingSystemId), new RtSimpleTypeName("string"));
        //todo generate a multistring type rather than just substituting it everywhere
        builder.ExportAsThirdParty<MultiString>().WithName("IMultiString").Imports([
            new() { From = "$lib/dotnet-types/i-multi-string", Target = "type {IMultiString}" }
        ]);
        builder.ExportAsInterface<Sense>().WithPublicNonStaticProperties(exportBuilder =>
        {
            if (exportBuilder.Member.Name == nameof(Sense.Order))
            {
                exportBuilder.Ignore();
            }
        });
        builder.ExportAsInterfaces([
                typeof(Entry),
                typeof(ExampleSentence),
                typeof(WritingSystem),
                typeof(WritingSystems),
                typeof(PartOfSpeech),
                typeof(SemanticDomain),
                typeof(ComplexFormType),
                typeof(ComplexFormComponent),

                typeof(MiniLcmJsInvokable.MiniLcmFeatures),
            ],
            exportBuilder => exportBuilder.WithPublicNonStaticProperties());
        builder.ExportAsEnum<WritingSystemType>().UseString();
        builder.ExportAsInterface<MiniLcmJsInvokable>()
            .FlattenHierarchy()
            .WithPublicProperties()
            .WithPublicMethods(b => b.AlwaysReturnPromise().OnlyJsInvokable());
        builder.ExportAsEnum<SortField>().UseString();
        builder.ExportAsInterfaces([typeof(QueryOptions), typeof(SortOptions), typeof(ExemplarOptions)],
            exportBuilder => exportBuilder.WithPublicNonStaticProperties());
    }

    private static void ConfigureFwLiteSharedTypes(ConfigurationBuilder builder)
    {

        builder.ExportAsEnum<DotnetService>().UseString();
        builder.ExportAsEnum<FwLitePlatform>().UseString();
        builder.ExportAsEnum<ProjectDataFormat>();
        builder.ExportAsInterfaces([
            typeof(AuthService),
            typeof(ImportFwdataService),
            typeof(CombinedProjectsService),
            typeof(MiniLcmApiProvider)
        ], exportBuilder => exportBuilder.WithPublicMethods(b => b.AlwaysReturnPromise().OnlyJsInvokable()));

        builder.ExportAsInterfaces([
            typeof(ServerStatus),
            typeof(ProjectModel),
            typeof(ServerProjects),
            typeof(LexboxServer),
            typeof(CrdtProject),
            typeof(ProjectData),
            typeof(IProjectIdentifier),
            typeof(FwLiteConfig)
        ], exportBuilder => exportBuilder.WithPublicProperties());
    }

    private static MethodExportBuilder AlwaysReturnPromise(this MethodExportBuilder exportBuilder)
    {
        var isUpdatePatchMethod = exportBuilder.Member.GetParameters()
            .Any(p => p.ParameterType.IsGenericType &&
                      p.ParameterType.GetGenericTypeDefinition() == (typeof(UpdateObjectInput<>)));
        if (isUpdatePatchMethod)
        {
            exportBuilder.Ignore();
            return exportBuilder;
        }

        var isTaskMethod = (exportBuilder.Member.ReturnType.IsGenericType &&
                            (exportBuilder.Member.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                             || exportBuilder.Member.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
                           || exportBuilder.Member.ReturnType == typeof(Task)
                           || exportBuilder.Member.ReturnType == typeof(ValueTask);
        if (!isTaskMethod)
        {
            if (exportBuilder.Member.ReturnType == typeof(void))
            {
                exportBuilder.Returns(typeof(Task));
            }
            else
            {
                exportBuilder.Returns(typeof(Task<>).MakeGenericType(exportBuilder.Member.ReturnType));
            }
        }
        return exportBuilder;
    }

    private static void OnlyJsInvokable(this MethodExportBuilder exportBuilder)
    {
        if (exportBuilder.Member.GetCustomAttribute<JSInvokableAttribute>() is null)
            exportBuilder.Ignore();
    }

    private static T WithPublicNonStaticProperties<T>(this T tc, Action<PropertyExportBuilder>? configuration = null)
        where T : ClassOrInterfaceExportBuilder
    {
        return tc.WithProperties(BindingFlags.Public | BindingFlags.Instance, configuration);
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
