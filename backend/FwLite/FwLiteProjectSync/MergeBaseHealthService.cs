using LcmCrdt;
using Microsoft.Extensions.Options;
using MiniLcm;
using SIL.Harmony.Core;

namespace FwLiteProjectSync;

public enum MergeBaseVerdict
{
    /// <summary>The base names a commit the CRDT still agrees with. Safe to sync against.</summary>
    Healthy,

    /// <summary>Pre-provenance base, or one whose commit isn't in this database. Can't be checked either way.</summary>
    Unverifiable,

    /// <summary>
    /// The CRDT holds sync-authored commits the base doesn't know about, so an earlier sync applied fwdata
    /// changes and then died before recording them. Syncing against this base pushes those leftovers into fwdata.
    /// </summary>
    Stale,
}

/// <param name="UnrecordedSyncCommits">
/// How many sync-authored commits after the base were found, up to the scan cap. 0 unless the verdict is Stale.
/// </param>
public record MergeBaseHealth(MergeBaseVerdict Verdict, string Reason, int UnrecordedSyncCommits, bool ScanCapReached);

/// <summary>
/// Answers "can this merge base be trusted?" — see docs/sync-atomicity/README.md. Atomic sync commit
/// stops new bad bases from being written; this catches the ones already on disk, whatever wrote them.
/// Resolve from the same scope as the CRDT project being checked.
/// </summary>
public class MergeBaseHealthService(
    CrdtHistoryHeadService historyHeadService,
    IOptions<LcmCrdtConfig> crdtConfig)
{
    /// <summary>
    /// Above this many commits after the base we stop counting: it's already more divergence than any healthy
    /// project has, and the answer doesn't change.
    /// </summary>
    private const int ScanCap = 10_000;

    public async Task<MergeBaseHealth> Check(ProjectSnapshot mergeBase)
    {
        if (crdtConfig.Value.DefaultAuthorForCommits is null)
        {
            // Without a configured sync author, sync commits are indistinguishable from a signed-out user's, so
            // every commit would look sync-authored. FwHeadless configures one; nothing else runs this check.
            return new MergeBaseHealth(MergeBaseVerdict.Unverifiable,
                $"{nameof(LcmCrdtConfig.DefaultAuthorForCommits)} is not configured, so sync commits can't be told apart from anyone else's.",
                0, false);
        }

        var baseCommitId = mergeBase.Provenance?.CrdtCommitId;
        if (baseCommitId is null)
        {
            return new MergeBaseHealth(MergeBaseVerdict.Unverifiable,
                "The merge base records no CRDT commit, so it predates provenance stamping.", 0, false);
        }

        var baseCommit = await historyHeadService.FindCommit(baseCommitId.Value);
        if (baseCommit is null)
        {
            return new MergeBaseHealth(MergeBaseVerdict.Unverifiable,
                $"The merge base names CRDT commit {baseCommitId} which this database does not contain. " +
                "It may belong to another project, or the database was replaced.", 0, false);
        }

        var head = await historyHeadService.GetHeadCommitId();
        if (head == baseCommitId)
        {
            return new MergeBaseHealth(MergeBaseVerdict.Healthy, "The merge base is at the CRDT head.", 0, false);
        }

        var after = await historyHeadService.GetCommitsAfter(baseCommit, ScanCap);
        var capReached = after.Length == ScanCap;
        var unrecordedSyncCommits = after.Count(IsSyncAuthored);
        if (unrecordedSyncCommits == 0)
        {
            // Commits after the base authored by people are the normal case and the whole point of the second
            // sync pass: FW Lite edits arrive after the base and get pushed to fwdata.
            return new MergeBaseHealth(MergeBaseVerdict.Healthy,
                $"{after.Length} commit(s) after the merge base, none of them written by the sync.", 0, capReached);
        }

        return new MergeBaseHealth(MergeBaseVerdict.Stale,
            $"{unrecordedSyncCommits} of {after.Length} commit(s) after the merge base were written by the sync, " +
            "so an earlier sync applied fwdata changes and did not record them.",
            unrecordedSyncCommits,
            capReached);
    }

    /// <remarks>
    /// Recognised by author name, the same signal <see cref="SnapshotAtCommitService"/> uses to tell sync commits
    /// apart. Correct for FwHeadless, where no user is signed in so every sync commit gets the configured default
    /// author. A sync run by a signed-in user would be attributed to that user and undercount here; no production
    /// path does that today.
    /// </remarks>
    private bool IsSyncAuthored(CommitBase commit)
    {
        return commit.Metadata.AuthorName == crdtConfig.Value.DefaultAuthorForCommits;
    }
}
