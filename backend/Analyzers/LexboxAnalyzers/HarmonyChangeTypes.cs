using System.Linq;
using Microsoft.CodeAnalysis;

namespace LexboxAnalyzers;

/// <summary>
/// Shared detection of Harmony change types for the LX rules: a change type is any type that
/// implements <c>SIL.Harmony.Changes.IChange</c>. Each rule applies its own filter on top (LX0001
/// keeps concrete classes only; LX0002 also covers abstract bases and nested helper types).
/// </summary>
internal static class HarmonyChangeTypes
{
    private const string ChangeInterfaceMetadataName = "SIL.Harmony.Changes.IChange";

    /// <summary>
    /// The <c>IChange</c> interface symbol, or <c>null</c> when the compilation doesn't reference
    /// Harmony, in which case there is nothing to police. Resolve it once per compilation, in
    /// <c>RegisterCompilationStartAction</c>, never inside per-symbol or per-operation callbacks.
    /// </summary>
    public static INamedTypeSymbol? ResolveChangeInterface(Compilation compilation) =>
        compilation.GetTypeByMetadataName(ChangeInterfaceMetadataName);

    public static bool IsChangeType(INamedTypeSymbol type, INamedTypeSymbol changeInterface) =>
        type.AllInterfaces.Contains(changeInterface, SymbolEqualityComparer.Default);
}
