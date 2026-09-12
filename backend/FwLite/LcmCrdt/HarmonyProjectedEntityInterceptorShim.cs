// TEMPORARY SHIM — remove once the Harmony PR that adds IProjectedEntityInterceptor is released and
// the SIL.Harmony package version in Directory.Packages.props is bumped to include it.
// https://github.com/sillsdev/harmony (the "reduce-sync-work" change: "Add once-per-save projected entity interceptor").
//
// The published SIL.Harmony package does not yet contain these types, so LcmCrdt would not compile
// against the package (which is what CI and the default build use). This file provides source-compatible
// copies so the build stays green. It is compiled ONLY when building against the package: the
// HARMONY_PACKAGE_SHIM constant is defined in LcmCrdt.csproj when UseHarmonySource != true. When building
// against Harmony source (UseHarmonySource=true) the real types from SIL.Harmony are used instead, so
// these copies must NOT be present then — otherwise they would shadow the real interface and the
// interceptor registrations would silently bind to the shadow, disabling the mechanism.
//
// Against the package these types are never instantiated or invoked (that older Harmony projects via EF
// change tracking and never calls the interceptor); they exist purely to satisfy compilation.
#if HARMONY_PACKAGE_SHIM
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIL.Harmony.Db;

public enum ProjectedChangeKind
{
    Upsert,
    Delete
}

public interface IProjectedEntityInterceptor
{
    ValueTask OnProjectedEntitiesChanged(ProjectedEntityBatch batch);
}

public sealed class ProjectedEntityBatch
{
    public required ICrdtDbContext DbContext { get; init; }
    public required IReadOnlyList<ProjectedEntityChange> Changes { get; init; }
}

public sealed class ProjectedEntityChange
{
    public required object Entity { get; init; }
    public required Guid EntityId { get; init; }
    public required Type ClrType { get; init; }
    public required ProjectedChangeKind Kind { get; init; }
}
#endif
