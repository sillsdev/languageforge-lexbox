using LcmCrdt;
using Reinforced.Typings;
using Reinforced.Typings.Ast;
using Reinforced.Typings.Generators;

namespace FwLiteShared.TypeGen;

/// <summary>
/// Emits a `ChangeType` string-literal union and a `knownChangeTypes` array built from the registered CRDT
/// change types and each type's serialized <c>$type</c> discriminator, taken straight from Harmony's
/// registration (<see cref="LcmCrdtKernel.AllRegisteredChanges"/>) so the generated list can't drift from
/// what the serializer writes. Attached to the <c>ChangeType</c> marker; suppresses the marker's own output.
/// </summary>
public class ChangeTypesCodeGenerator : ClassCodeGenerator
{
    public override RtClass GenerateNode(Type element, RtClass result, TypeResolver resolver)
    {
        var typeNames = LcmCrdtKernel.AllRegisteredChanges()
            .Select(c => c.Discriminator)
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
}
