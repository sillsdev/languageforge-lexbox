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
    CurrentProjectService projectService
)
{
    private Guid ClientId { get; } = projectService.ProjectData.ClientId;
    public ProjectData ProjectData => projectService.ProjectData;
    private CommitMetadata NewMetadata()
    {
        var metadata = new CommitMetadata
        {
            ClientVersion = AppVersion.Version,
            //todo, if a user logs out and in with another account, this will be out of date until the next sync
            AuthorName = ProjectData.LastUserName ?? config.Value.DefaultAuthorForCommits,
            AuthorId = ProjectData.LastUserId
        };
        commitMetadataInterceptor.Apply(metadata);
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

    private void AssertWritable()
    {
        if (ProjectData.IsReadonly)
            throw new ReadOnlyException(
                $"project is readonly because you are logged in with the {ProjectData.Role} role. If your role recently changed, try refreshing the server project list on the home page.");
    }
}
