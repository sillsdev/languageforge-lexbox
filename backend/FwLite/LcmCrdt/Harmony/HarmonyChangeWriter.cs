using System.Data;
using Microsoft.Extensions.Options;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;

namespace LcmCrdt.Harmony;

public class HarmonyChangeWriter(
    DataModel dataModel,
    CommitMetadataInterceptor commitMetadataInterceptor,
    IOptions<LcmCrdtConfig> config,
    CurrentProjectService projectService,
    CurrentAuthorService currentAuthorService
)
{
    private Guid ClientId { get; } = projectService.ProjectData.ClientId;
    public ProjectData ProjectData => projectService.ProjectData;
    private CommitMetadata NewMetadata()
    {
        //todo, if a user logs out and in with another account, this will be out of date until the next sync
        var author = currentAuthorService.GetCurrentAuthor();
        var metadata = new CommitMetadata
        {
            ClientVersion = AppVersion.Version,
            AuthorName = author.Name,
            AuthorId = author.Id
        };
        commitMetadataInterceptor.Apply(metadata);
        if (config.Value.RequireCommitAuthor && string.IsNullOrEmpty(metadata.AuthorName) && string.IsNullOrEmpty(metadata.AuthorId))
            throw new InvalidOperationException(
                $"Refusing to write a commit with no author, wrap the write in {nameof(CommitMetadataInterceptor)}.{nameof(CommitMetadataInterceptor.Intercept)} to name one");
        return metadata;
    }

    public async Task<Commit> AddChange(IChange change)
    {
        AssertWritable();
        var commit = await dataModel.AddChange(ClientId, change, commitMetadata: NewMetadata());
        return commit;
    }

    public async Task AddChanges(IEnumerable<IChange> changes)
    {
        AssertWritable();
        await dataModel.AddManyChanges(ClientId, changes, commitMetadata: NewMetadata);
    }

    /// <summary>
    /// For comment changes, which roles that can't edit the project (Commenter) may still make.
    /// </summary>
    public async Task AddCommentChange(IChange change)
    {
        AssertCanComment();
        await dataModel.AddChange(ClientId, change, commitMetadata: NewMetadata());
    }

    /// <inheritdoc cref="AddCommentChange"/>
    public async Task AddCommentChanges(IEnumerable<IChange> changes)
    {
        AssertCanComment();
        await dataModel.AddManyChanges(ClientId, changes, commitMetadata: NewMetadata);
    }

    private void AssertCanComment()
    {
        if (!ProjectData.CanComment)
            throw new ReadOnlyException(
                $"you can't comment on this project because you are logged in with the {ProjectData.Role} role.");
    }

    private void AssertWritable()
    {
        if (ProjectData.IsReadonly)
            throw new ReadOnlyException(
                $"project is readonly because you are logged in with the {ProjectData.Role} role. If your role recently changed, try refreshing the server project list on the home page.");
    }
}
