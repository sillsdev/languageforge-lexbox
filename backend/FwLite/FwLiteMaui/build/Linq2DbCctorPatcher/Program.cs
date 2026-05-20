// Stubs the broken static constructor on
// LinqToDB.EntityFrameworkCore.EFCoreMetadataReader+SqlTransparentExpression
// inside linq2db.EntityFrameworkCore 10.3.x.
//
// The shipped .cctor does a GetConstructor lookup for (ExceptExpression,
// RelationalTypeMapping), which doesn't exist on the type — the only declared
// ctor takes (ConstantExpression, RelationalTypeMapping?). The lookup returns
// null and the .cctor throws InvalidOperationException. Reproducible on plain
// net10.0 with RuntimeHelpers.RunClassConstructor.
//
// Desktop CRDT never accesses the affected static fields (only Quote() does),
// so it's silent. Android (and any environment that eagerly initializes the
// type) hits TypeInitializationException on the first CRDT save.
//
// We can't fix this via ILLink.Substitutions.xml on Android Debug because
// PublishTrimmed is false and the linker pass is skipped. And in Release
// publish the staged dll location differs from the ILLink target site, so a
// single substitution hook is fragile. So we Cecil-patch unconditionally at
// build time, on every linq2db.EntityFrameworkCore.dll under the obj tree.
//
// Also removes Quote() so any unexpected caller fails loudly with
// NotImplementedException instead of NRE'ing on the (now-null) _ctor field.
//
// SCOPE: only FwLiteMaui targets net10.0-android today, so this patcher lives
// alongside it. If another csproj ever targets net10.0-android and references
// linq2db.EntityFrameworkCore, lift this into a shared backend/build/ tools
// directory and reference it from each consumer's targets.
//
// KILL-SWITCH: when upstream ships a fixed version (see the version pin
// in FwLiteMaui.csproj — search for _Linq2DbEfCorePatchedVersion), delete this
// project and the two _BuildLinq2DbCctorPatcher / _PatchLinq2Db... targets,
// and unskip backend/FwLite/LcmCrdt.Tests/SqlTransparentExpressionCctorRepro.cs.
using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: Linq2DbCctorPatcher <path-to-linq2db.EntityFrameworkCore.dll>");
    return 1;
}

var dllPath = args[0];
if (!File.Exists(dllPath))
{
    Console.Error.WriteLine($"File not found: {dllPath}");
    return 2;
}

var markerPath = dllPath + ".cctor-patched";
if (File.Exists(markerPath) && File.GetLastWriteTimeUtc(markerPath) >= File.GetLastWriteTimeUtc(dllPath))
{
    Console.WriteLine($"Already patched: {dllPath}");
    return 0;
}

// Structural guards: if upstream restructures any of these, the build must
// break loudly. We do NOT skip-on-mismatch — that would silently ship an
// unprotected dll. Bumping the package without re-checking this code is
// already gated by the MSBuild version pin in FwLiteMaui.csproj, but these
// guards are belt-and-braces for the case where someone widens the pin
// without auditing the IL shape.
static int Fail(string message)
{
    Console.Error.WriteLine("Linq2DbCctorPatcher: " + message);
    Console.Error.WriteLine(
        "linq2db.EntityFrameworkCore structure changed; patcher needs review. " +
        "See backend/FwLite/LcmCrdt/LINQ2DB-V6-NOTES.md (Cctor patcher section).");
    return 3;
}

using (var asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { ReadWrite = true }))
{
    var outer = asm.MainModule.GetType("LinqToDB.EntityFrameworkCore.EFCoreMetadataReader");
    if (outer is null)
        return Fail("EFCoreMetadataReader type not found.");

    var nested = outer.NestedTypes.FirstOrDefault(t => t.Name == "SqlTransparentExpression");
    if (nested is null)
        return Fail("SqlTransparentExpression nested type not found inside EFCoreMetadataReader.");

    var cctor = nested.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
    if (cctor is null || !cctor.HasBody)
        return Fail("SqlTransparentExpression .cctor not found (or has no body).");

    // Sanity-check the cctor shape: at least one stsfld targeting the _ctor field.
    // If upstream renames _ctor or restructures the field init, we want to know.
    var storesCtorField = cctor.Body.Instructions.Any(ins =>
        ins.OpCode == OpCodes.Stsfld
        && ins.Operand is FieldReference fr
        && fr.Name == "_ctor"
        && fr.DeclaringType.FullName == nested.FullName);
    if (!storesCtorField)
        return Fail("SqlTransparentExpression .cctor no longer contains a stsfld for the _ctor field; IL shape changed.");

    var quote = nested.Methods.FirstOrDefault(m => m.Name == "Quote" && m.Parameters.Count == 0);
    if (quote is null || !quote.HasBody)
        return Fail("SqlTransparentExpression.Quote() not found (or has no body).");

    {
        var il = cctor.Body.GetILProcessor();
        cctor.Body.Instructions.Clear();
        cctor.Body.ExceptionHandlers.Clear();
        cctor.Body.Variables.Clear();
        il.Append(Instruction.Create(OpCodes.Ret));
        Console.WriteLine("Stubbed SqlTransparentExpression .cctor to no-op ret");
    }

    {
        // Replace Quote() with `throw new NotImplementedException();` so anything that
        // somehow reaches it fails loud rather than NRE'ing on the now-null _ctor field.
        var nieCtor = asm.MainModule.ImportReference(
            new MethodReference(".ctor", asm.MainModule.TypeSystem.Void,
                asm.MainModule.ImportReference(typeof(NotImplementedException)))
            { HasThis = true });
        var il = quote.Body.GetILProcessor();
        quote.Body.Instructions.Clear();
        quote.Body.ExceptionHandlers.Clear();
        quote.Body.Variables.Clear();
        il.Append(Instruction.Create(OpCodes.Newobj, nieCtor));
        il.Append(Instruction.Create(OpCodes.Throw));
        Console.WriteLine("Replaced SqlTransparentExpression.Quote() with throw NotImplementedException");
    }

    asm.Write();
}

File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
Console.WriteLine($"Patched {dllPath}");
return 0;
