using System.Reflection;
using LcmCrdt;
using Reinforced.Typings;
using Reinforced.Typings.Ast;
using Reinforced.Typings.Generators;

namespace FwLiteShared.TypeGen;

/// <summary>
/// Emits a `ChangeType` string-literal union and a `knownChangeTypes` array built from the registered CRDT
/// change types (<see cref="LcmCrdtKernel.AllChangeTypes"/>) and each type's <c>IPolyType.TypeName</c>
/// (the serialized <c>$type</c> discriminator). Attached to the <c>ChangeTypes</c> marker; suppresses the
/// marker's own output.
/// </summary>
public class ChangeTypesCodeGenerator : ClassCodeGenerator
{
    public override RtClass GenerateNode(Type element, RtClass result, TypeResolver resolver)
    {
        var typeNames = LcmCrdtKernel.AllChangeTypes()
            .Select(GetChangeTypeName)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var union = string.Concat(typeNames.Select(name => $"\n  | '{name}'"));
        var array = string.Concat(typeNames.Select(name => $"\n  '{name}',"));

        Context.Location.CurrentNamespace.CompilationUnits.Add(new RtRaw(
            $"export type ChangeType ={union};\n\nexport const knownChangeTypes = [{array}\n] as const satisfies readonly ChangeType[];\n"));

        // Suppress the marker class; only the raw union/array above is emitted.
        return null!;
    }

    private static string GetChangeTypeName(Type changeType)
    {
        var typeNameProperty = changeType.GetProperty("TypeName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return typeNameProperty?.GetValue(null) as string ?? changeType.Name;
    }
}
