using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LexboxAnalyzers.Rules;

/// <summary>
/// LX0001: every concrete Harmony change type (a class implementing
/// <c>SIL.Harmony.Changes.IChange</c>) must declare a constructor with a <c>Guid entityId</c>
/// parameter.
/// <para>
/// Change types are reconstructed from JSON, and Harmony maps the <c>entityId</c> constructor
/// parameter onto the <c>EntityId</c> property. A concrete change type without such a constructor
/// (or with the parameter named something other than <c>entityId</c>) fails to deserialize at
/// runtime, which surfaces as sync corruption rather than a compile error — hence a build-time rule.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ChangeEntityIdConstructorAnalyzer : DiagnosticAnalyzer
{
    private const string ChangeInterfaceMetadataName = "SIL.Harmony.Changes.IChange";
    private const string GuidMetadataName = "System.Guid";
    private const string EntityIdParameterName = "entityId";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ChangeMustHaveEntityIdConstructor,
        title: "CRDT change types must declare a Guid entityId constructor",
        messageFormat: "Change type '{0}' must declare a constructor with a 'Guid entityId' parameter (required for JSON deserialization)",
        category: "LcmCrdt.Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Harmony reconstructs change types from JSON by matching the 'entityId' constructor parameter to the EntityId property. A concrete change type without a 'Guid entityId' constructor fails to deserialize at runtime.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static start =>
        {
            // Resolve the marker symbols once per compilation and close over them; never resolve
            // symbols inside the per-symbol callback (it runs on every keystroke in the IDE).
            var changeInterface = start.Compilation.GetTypeByMetadataName(ChangeInterfaceMetadataName);
            if (changeInterface is null) return; // Compilation doesn't reference Harmony — nothing to police.

            var guidType = start.Compilation.GetTypeByMetadataName(GuidMetadataName);
            if (guidType is null) return;

            start.RegisterSymbolAction(
                ctx => AnalyzeNamedType(ctx, changeInterface, guidType),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        INamedTypeSymbol changeInterface,
        INamedTypeSymbol guidType)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        // Only concrete classes can be instantiated/deserialized. Abstract change bases
        // (CreateChange<T>, EditChange<T>, intermediate LcmCrdt bases) are exempt.
        if (type.TypeKind != TypeKind.Class) return;
        if (type.IsAbstract || type.IsStatic || type.IsImplicitlyDeclared) return;

        if (!type.AllInterfaces.Contains(changeInterface, SymbolEqualityComparer.Default)) return;

        // Constructors are not inherited, so the concrete type must declare its own. This also
        // matches primary constructors, whose parameters appear on the InstanceConstructors symbol.
        var hasEntityIdConstructor = type.InstanceConstructors.Any(ctor =>
            ctor.Parameters.Any(p =>
                p.Name == EntityIdParameterName &&
                SymbolEqualityComparer.Default.Equals(p.Type, guidType)));
        if (hasEntityIdConstructor) return;

        var location = type.Locations.FirstOrDefault(l => l.IsInSource) ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name));
    }
}
