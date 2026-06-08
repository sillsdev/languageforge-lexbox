// Cecil-patches the broken cctor on LinqToDB.EntityFrameworkCore.EFCoreMetadataReader+SqlTransparentExpression
// (and replaces Quote() with a loud throw). See https://github.com/sillsdev/languageforge-lexbox/issues/2291
// for the background, kill-switch checklist, and upstream PR link.
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
        "See https://github.com/sillsdev/languageforge-lexbox/issues/2291.");
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

    // The _ctor field is what makes Quote() dangerous, so its continued existence
    // is the real invariant — guard it directly rather than inferring it from the cctor.
    if (!nested.Fields.Any(f => f.Name == "_ctor"))
        return Fail("SqlTransparentExpression no longer declares the _ctor field; IL shape changed.");

    var cctor = nested.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
    if (cctor is null || !cctor.HasBody)
        return Fail("SqlTransparentExpression .cctor not found (or has no body).");

    // The cctor must be in one of two recognised shapes:
    //  - the original reflection-based init, which assigns _ctor via stsfld (we neutralise it below), or
    //  - an effective no-op (just `ret`), which the size-optimising trimmer produces by dropping the
    //    now-unused field init — and which is also what our own stub leaves behind on a re-run.
    // Any other shape (real instructions but no stsfld _ctor) is an unrecognised restructure: fail loud.
    var storesCtorField = cctor.Body.Instructions.Any(ins =>
        ins.OpCode == OpCodes.Stsfld
        && ins.Operand is FieldReference fr
        && fr.Name == "_ctor"
        && fr.DeclaringType.FullName == nested.FullName);
    var isNoOp = cctor.Body.Instructions.All(ins => ins.OpCode == OpCodes.Nop || ins.OpCode == OpCodes.Ret);
    if (!storesCtorField && !isNoOp)
        return Fail("SqlTransparentExpression .cctor has an unrecognised shape (no stsfld for _ctor and not a no-op); IL shape changed.");

    var quote = nested.Methods.FirstOrDefault(m => m.Name == "Quote" && m.Parameters.Count == 0);
    if (quote is null || !quote.HasBody)
        return Fail("SqlTransparentExpression.Quote() not found (or has no body).");

    ReplaceBodyWith(cctor, Instruction.Create(OpCodes.Ret));
    Console.WriteLine(storesCtorField
        ? "Stubbed SqlTransparentExpression .cctor to no-op ret"
        : "SqlTransparentExpression .cctor already a no-op (trimmer-neutralised); ensured ret");

    // Replace Quote() with `throw new NotImplementedException();` so anything that
    // somehow reaches it fails loud rather than NRE'ing on the now-null _ctor field.
    var nieCtor = asm.MainModule.ImportReference(
        typeof(NotImplementedException).GetConstructor(Type.EmptyTypes)!);
    ReplaceBodyWith(quote,
        Instruction.Create(OpCodes.Newobj, nieCtor),
        Instruction.Create(OpCodes.Throw));
    Console.WriteLine("Replaced SqlTransparentExpression.Quote() with throw NotImplementedException");

    asm.Write();
}

File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
Console.WriteLine($"Patched {dllPath}");
return 0;

static void ReplaceBodyWith(MethodDefinition method, params Instruction[] instructions)
{
    method.Body.Instructions.Clear();
    method.Body.ExceptionHandlers.Clear();
    method.Body.Variables.Clear();
    var il = method.Body.GetILProcessor();
    foreach (var ins in instructions) il.Append(ins);
}
