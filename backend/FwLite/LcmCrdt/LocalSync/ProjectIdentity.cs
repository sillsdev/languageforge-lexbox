namespace LcmCrdt.LocalSync;

/// <summary>
/// What two copies of a project must agree on before local (offline) sync may merge them.
/// </summary>
/// <param name="ProjectId">The project Id, the same on every copy (matches the Lexbox project Id).</param>
/// <param name="RootCommitId">Id of the first commit in Harmony's default order. Commit ids are minted once and copied
/// verbatim by sync, so copies that share history agree on it. Null only for a project with no commits.</param>
/// <param name="WritingSystemIds">The distinct writing system ids (e.g. "seh", "en") across vernacular and analysis.
/// Copies only need one in common: a writing system added on one device has to be able to reach the others.</param>
public record ProjectIdentity(Guid ProjectId, Guid? RootCommitId, IReadOnlySet<string> WritingSystemIds)
{
    /// <summary>
    /// Compares the local copy of a project (null when this device doesn't have it) with a remote one.
    /// Only <see cref="IdentityComparison.Match"/> may be synced. The two "Different..." lineage results mean
    /// the same project Id with a different history, which must never be merged.
    /// </summary>
    public static IdentityComparison Compare(ProjectIdentity? local, ProjectIdentity remote)
    {
        if (local is null) return IdentityComparison.NoLocalProject;
        if (local.ProjectId != remote.ProjectId) return IdentityComparison.DifferentProject;
        if (local.RootCommitId != remote.RootCommitId) return IdentityComparison.DifferentRootCommit;
        if (local.WritingSystemIds.Count > 0 && remote.WritingSystemIds.Count > 0
            && !local.WritingSystemIds.Overlaps(remote.WritingSystemIds)) return IdentityComparison.DifferentWritingSystems;
        return IdentityComparison.Match;
    }
}

public enum IdentityComparison
{
    /// <summary>This device doesn't have the project; the caller may offer to download it.</summary>
    NoLocalProject,
    /// <summary>The remote is a different project; the caller may offer to switch to it.</summary>
    DifferentProject,
    /// <summary>Same project, same history: safe to sync.</summary>
    Match,
    /// <summary>Same project Id but a different root commit: refuse, never merge.</summary>
    DifferentRootCommit,
    /// <summary>Same project Id but no writing system in common: refuse, never merge.</summary>
    DifferentWritingSystems,
}
