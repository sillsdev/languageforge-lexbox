using LcmCrdt.Changes;
using LcmCrdt.Changes.Comments;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony.Core;
using Soenneker.Utils.AutoBogus;

namespace LcmCrdt.Tests;

public abstract class HistoryServiceActivityTestsBase : IAsyncLifetime, IAsyncDisposable
{
    private static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(["en"]));
    private MiniLcmApiFixture _fixture = null!;

    protected HistoryService Service => _fixture.GetService<HistoryService>();
    protected DataModel DataModel => _fixture.DataModel;
    protected Guid ClientId => _fixture.GetService<CurrentProjectService>().ProjectData.ClientId;

    public async Task InitializeAsync()
    {
        _fixture = MiniLcmApiFixture.Create();
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();

    protected static CommitMetadata Meta() => new() { AuthorName = "A", AuthorId = "a" };

    protected async Task<Guid> CreateEntry(string headword)
    {
        var entryId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry
        {
            Id = entryId,
            LexemeForm = new MultiString { ["en"] = headword }
        }), Meta());
        return entryId;
    }

    protected async Task<Guid> CreateSense(Guid entryId, string? gloss, double order, IList<SemanticDomain>? semanticDomains = null)
    {
        var senseId = Guid.NewGuid();
        var sense = new Sense
        {
            Id = senseId,
            Order = order,
            Gloss = gloss is null ? [] : new MultiString { ["en"] = gloss },
            SemanticDomains = semanticDomains ?? []
        };
        await DataModel.AddChange(ClientId, new CreateSenseChange(sense, entryId), Meta());
        return senseId;
    }

    protected async Task<ComplexFormComponent> AddComponent(Guid complexFormId, Guid componentId)
    {
        var component = ComplexFormComponent.FromEntries(
            new Entry { Id = complexFormId },
            new Entry { Id = componentId });
        await DataModel.AddChange(ClientId, new AddEntryComponentChange(component), Meta());
        return component;
    }

    protected async Task<Guid> AddCommentThread(Guid subjectId, SubjectType subjectType)
    {
        var threadId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateCommentThreadChange(new CommentThread
        {
            Id = threadId,
            SubjectId = subjectId,
            SubjectType = subjectType,
            AuthorId = "a",
            AuthorName = "A",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }), Meta());
        return threadId;
    }

    protected async Task<Guid> AddComment(Guid threadId, string text)
    {
        var commentId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateUserCommentChange(new UserComment
        {
            Id = commentId,
            CommentThreadId = threadId,
            Text = text,
            AuthorId = "a",
            AuthorName = "A",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }), Meta());
        return commentId;
    }

    protected async Task<Commit> AddEntryCommit(CommitMetadata metadata, string? headword = null)
    {
        var entry = headword is null
            ? await AutoFaker.EntryReadyForCreation(_fixture.Api)
            : new Entry { Id = Guid.NewGuid(), LexemeForm = new MultiString { ["en"] = headword } };
        return await DataModel.AddChange(ClientId, new CreateEntryChange(entry), metadata);
    }

    protected async Task<Commit> AddNewPublicationCommit(CommitMetadata metadata, string publicationName = "Test Publication")
    {
        return await DataModel.AddChange(ClientId, new CreatePublicationChange(Guid.NewGuid(), new MultiString
        {
            ["en"] = publicationName
        }), metadata);
    }

    protected async Task<Commit> AddNewPartOfSpeechCommit(CommitMetadata metadata, string partOfSpeechName = "Test Part of Speech")
    {
        return await DataModel.AddChange(ClientId, new CreatePartOfSpeechChange(Guid.NewGuid(), new MultiString
        {
            ["en"] = partOfSpeechName
        }), metadata);
    }

    protected async Task SetSyncDate(Guid commitId, DateTimeOffset syncDate)
    {
        var db = _fixture.DbContext;
        var commit = await db.Set<Commit>().SingleAsync(c => c.Id == commitId);
        commit.SetSyncDate(syncDate);
        db.Entry(commit).Property(c => c.Metadata).IsModified = true;
        await db.SaveChangesAsync();
    }
}
