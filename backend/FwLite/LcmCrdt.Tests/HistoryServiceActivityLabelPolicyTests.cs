using LcmCrdt.Changes;
using LcmCrdt.Changes.Comments;
using SystemTextJsonPatch;

namespace LcmCrdt.Tests;

/// <summary>
/// Pins down the label policy described on <see cref="ActivityChangeInfoResolver"/> with one test per behavior:
/// identifying labels track the newest known state (so they drift as the data changes), comment snippets quote
/// the written text (so they never drift), plus the sense-numbering quirk the policy causes.
/// </summary>
public class HistoryServiceActivityLabelPolicyTests : HistoryServiceActivityTestsBase
{
    [Fact]
    public async Task ProjectActivity_ChangeInfo_IdentifyingLabels_TrackCurrentState_EvenAfterDelete()
    {
        var entryId = await CreateEntry("run");
        var rename = new JsonPatchDocument<Entry>();
        rename.Replace(e => e.LexemeForm["en"], "jog");
        await DataModel.AddChange(ClientId, new JsonPatchChange<Entry>(entryId, rename), Meta());

        var renamed = await Service.ProjectActivity(0, 100, new ActivityQuery());
        // The change that created the entry as "run" now names it by its current headword.
        renamed.Should().Contain(a => a.Changes.Any(c => c.Entity.Change is CreateEntryChange) && a.Changes[0].Info.Subject == "jog");

        await DataModel.AddChange(ClientId, new SIL.Harmony.Changes.DeleteChange<Entry>(entryId), Meta());

        var deleted = await Service.ProjectActivity(0, 100, new ActivityQuery());
        // Deletion has no effect: the label recovered from the last snapshot is the renamed one.
        deleted.Should().Contain(a => a.Changes.Any(c => c.Entity.Change is CreateEntryChange) && a.Changes[0].Info.Subject == "jog");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_CommentSnippets_QuoteWrittenText_NeverDrift()
    {
        var entryId = await CreateEntry("run");
        var threadId = await AddCommentThread(entryId, SubjectType.Entry);
        var commentId = await AddComment(threadId, "original words");
        await DataModel.AddChange(ClientId, new EditUserCommentChange(commentId, "second words", DateTimeOffset.UtcNow), Meta());
        await DataModel.AddChange(ClientId, new EditUserCommentChange(commentId, "third words", DateTimeOffset.UtcNow), Meta());

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());
        activities.Should().Contain(a => a.Changes.Any(c => c.Entity.Change is CreateUserCommentChange)
            && a.Changes[0].Info.Target == "original words");
        activities.Should().Contain(a => a.Changes.Any(c => c.Entity.Change is EditUserCommentChange)
            && a.Changes[0].Info.Target == "second words");
        activities.Should().Contain(a => a.Changes.Any(c => c.Entity.Change is EditUserCommentChange)
            && a.Changes[0].Info.Target == "third words");
    }

    [Fact]
    public async Task ProjectActivity_ChangeInfo_SenseNumbers_ReflectCurrentPositions()
    {
        var entryId = await CreateEntry("run");
        await CreateSense(entryId, gloss: "first", order: 1.0);
        await CreateSense(entryId, gloss: "early", order: 0.9);

        var activities = await Service.ProjectActivity(0, 100, new ActivityQuery());
        activities.Should().Contain(a => a.Changes.Any(c => c.Entity.Change is CreateSenseChange)
            && a.Changes[0].Info.Target == "first₂");
    }
}
