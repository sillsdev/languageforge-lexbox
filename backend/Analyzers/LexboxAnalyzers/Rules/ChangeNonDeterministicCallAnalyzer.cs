using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LexboxAnalyzers.Rules;

/// <summary>
/// LX0002: code in a Harmony change type (a class implementing <c>SIL.Harmony.Changes.IChange</c>)
/// must not call non-deterministic APIs (<c>Guid.NewGuid</c>, <c>DateTime.Now</c>, <c>Random</c>, ...)
/// anywhere except its instance constructors.
/// <para>
/// Harmony replays every change on every client, and again whenever snapshots are regenerated, by
/// calling <c>ApplyChange</c> / <c>NewEntity</c>. Everything on that path has to produce identical
/// results everywhere; a fresh Guid or timestamp minted during replay gives each client its own
/// value, which surfaces as sync divergence rather than an error. Such values must be generated
/// once, in the constructor on the originating client, and carried in a serialized property.
/// </para>
/// <para>
/// Instance constructors are exempt: the originating client runs them once, and the JSON
/// constructor used on replay only receives already-serialized values. Everything else in the type
/// is checked (methods including private helpers, property and field initializers, lambdas, local
/// functions, nested types, static constructors) because the analyzer cannot tell which helpers end
/// up on the replay path. A deliberate exception can be suppressed with <c>#pragma warning disable</c>
/// and a justification.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ChangeNonDeterministicCallAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Well-known non-deterministic members, keyed by the declaring type's metadata name. An empty
    /// member list bans every member of the type (construction, statics and instance calls alike).
    /// Matching is by name, so members missing from older target frameworks simply never match.
    /// </summary>
    private static readonly (string TypeMetadataName, string[] Members)[] BannedMembers =
    [
        ("System.Guid", ["NewGuid", "CreateVersion7"]),
        ("System.DateTime", ["Now", "UtcNow", "Today"]),
        ("System.DateTimeOffset", ["Now", "UtcNow"]),
        ("System.TimeProvider", ["System", "GetUtcNow", "GetLocalNow", "GetTimestamp"]),
        ("System.Random", []),
        ("System.Security.Cryptography.RandomNumberGenerator", []),
        ("System.Diagnostics.Stopwatch", []),
        // Everything on Environment varies per machine or process: tick counts, user and machine
        // names, and NewLine (which differs between the Windows and Android clients).
        ("System.Environment", []),
        ("System.IO.Path", ["GetRandomFileName", "GetTempFileName"]),
    ];

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ChangeMustBeDeterministic,
        title: "CRDT change types must be deterministic outside their constructors",
        messageFormat: "'{0}' is non-deterministic and must not be used in change type '{1}' outside its constructor; changes are replayed on every client, so generate the value once in the constructor and store it in a serialized property",
        category: "LcmCrdt.Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Harmony replays every change on every client and whenever snapshots are regenerated. A non-deterministic call (Guid.NewGuid, DateTime.Now, Random, ...) anywhere in a change type other than its constructor produces different state per client, corrupting sync. Generate such values once in the constructor and serialize them.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static start =>
        {
            // Resolve the marker symbols once per compilation; never inside per-operation callbacks.
            var changeInterface = HarmonyChangeTypes.ResolveChangeInterface(start.Compilation);
            if (changeInterface is null) return; // Compilation doesn't reference Harmony — nothing to police.

            var banned = ResolveBannedMembers(start.Compilation);
            if (banned.IsEmpty) return;

            start.RegisterOperationBlockStartAction(block =>
            {
                // Instance constructors run once on the originating client (or receive serialized
                // values on replay); everything else in a change type is treated as replay code.
                if (block.OwningSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor }) return;

                var changeType = EnclosingChangeType(block.OwningSymbol, changeInterface);
                if (changeType is null) return;

                block.RegisterOperationAction(
                    ctx => AnalyzeOperation(ctx, banned, changeType),
                    OperationKind.Invocation,
                    OperationKind.PropertyReference,
                    OperationKind.ObjectCreation,
                    OperationKind.MethodReference);
            });
        });
    }

    private static ImmutableDictionary<INamedTypeSymbol, ImmutableHashSet<string>> ResolveBannedMembers(Compilation compilation)
    {
        var builder = ImmutableDictionary.CreateBuilder<INamedTypeSymbol, ImmutableHashSet<string>>(SymbolEqualityComparer.Default);
        foreach (var (typeMetadataName, members) in BannedMembers)
        {
            var type = compilation.GetTypeByMetadataName(typeMetadataName);
            if (type is null) continue; // Not available on this target framework.
            builder[type] = ImmutableHashSet.Create(members);
        }
        return builder.ToImmutable();
    }

    /// <summary>
    /// The change type that <paramref name="owner"/> (a method, property, field, ...) belongs to,
    /// looking through nested types so a private nested helper class is covered too.
    /// </summary>
    private static INamedTypeSymbol? EnclosingChangeType(ISymbol owner, INamedTypeSymbol changeInterface)
    {
        for (var type = owner as INamedTypeSymbol ?? owner.ContainingType; type is not null; type = type.ContainingType)
        {
            if (HarmonyChangeTypes.IsChangeType(type, changeInterface)) return type;
        }
        return null;
    }

    private static void AnalyzeOperation(
        OperationAnalysisContext context,
        ImmutableDictionary<INamedTypeSymbol, ImmutableHashSet<string>> banned,
        INamedTypeSymbol changeType)
    {
        ISymbol? member = context.Operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IPropertyReferenceOperation property => property.Property,
            IObjectCreationOperation creation => creation.Constructor,
            IMethodReferenceOperation methodReference => methodReference.Method,
            _ => null,
        };
        if (member is null) return;

        member = Normalize(member);
        var declaringType = member.ContainingType?.OriginalDefinition;
        if (declaringType is null) return;
        if (!banned.TryGetValue(declaringType, out var members)) return;
        if (!members.IsEmpty && !members.Contains(member.Name)) return;

        // nameof(DateTime.Now) only takes the name; nothing runs.
        if (IsInsideNameOf(context.Operation)) return;

        var display = member is IMethodSymbol { MethodKind: MethodKind.Constructor }
            ? $"new {declaringType.Name}"
            : $"{declaringType.Name}.{member.Name}";
        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), display, changeType.Name));
    }

    /// <summary>
    /// Walks an override or extension-method call back to the member it was declared on, so a call
    /// through a derived type (e.g. a <c>TimeProvider</c> subclass) still matches the banned base member.
    /// </summary>
    private static ISymbol Normalize(ISymbol member)
    {
        switch (member)
        {
            case IMethodSymbol method:
                method = method.ReducedFrom ?? method;
                while (method.OverriddenMethod is { } overridden) method = overridden;
                return method.OriginalDefinition;
            case IPropertySymbol property:
                while (property.OverriddenProperty is { } overridden) property = overridden;
                return property.OriginalDefinition;
            default:
                return member.OriginalDefinition;
        }
    }

    private static bool IsInsideNameOf(IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is INameOfOperation) return true;
        }
        return false;
    }
}
