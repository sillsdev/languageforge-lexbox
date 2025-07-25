using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using LexCore.ServiceInterfaces;
using NeinLinq;

namespace LexCore.Entities;

public class Project : EntityBase
{

    public const string ProjectCodeRegex = @"^[a-z\d][a-z-\d]*$";
    public Guid? ParentId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required RetentionPolicy RetentionPolicy { get; set; }
    public required ProjectType Type { get; set; }
    public required bool? IsConfidential { get; set; }
    public FlexProjectMetadata? FlexProjectMetadata { get; set; }
    public required List<ProjectUsers> Users { get; set; }
    public required List<Organization> Organizations { get; set; }
    public required DateTimeOffset? LastCommit { get; set; }
    public int? RepoSizeInKb { get; set; }
    public DateTimeOffset? DeletedDate { get; set; }
    public ResetStatus ResetStatus { get; set; } = ResetStatus.None;

    //historical reference for if this project originated here (migrated), or came from redmine, public or private
    public ProjectMigrationStatus ProjectOrigin { get; set; } = ProjectMigrationStatus.Migrated;
    public DateTimeOffset? MigratedDate { get; set; } = null;

    [NotMapped]
    [InjectLambda(nameof(SqlUserCount))]
    public int UserCount { get; set; }
    private static Expression<Func<Project, int>> SqlUserCount => project => project.Users.Count;

    public async Task<Changeset[]> GetChangesets(IHgService hgService)
    {
        var age = DateTimeOffset.UtcNow.Subtract(CreatedDate);
        if (age.TotalSeconds < 6) // slightly longer than the refreshinterval (5s) in hgweb.hgrc
        {
            // The repo is unstable and potentially unavailable for a short while after creation, so don't read from it right away.
            // See: https://github.com/sillsdev/languageforge-lexbox/issues/173#issuecomment-1665478630
            // Update: Although we've greatly improved stability here, we're still at the will of the hgweb refresh interval.
            return [];
        }
        else
        {
            return await hgService.GetChangesets(Code);
        }
    }

    public bool GetHasAbandonedTransactions(IHgService hgService)
    {
        return hgService.HasAbandonedTransactions(Code);
    }

    public Task<bool> GetIsLanguageForgeProject(IIsLanguageForgeProjectDataLoader loader)
    {
        if (Type is ProjectType.Unknown or ProjectType.FLEx)
        {
            return loader.LoadAsync(Code);
        }
        return Task.FromResult(false);
    }

    public Task<bool> GetHasHarmonyCommits(IIsHarmonyProjectDataLoader loader)
    {
        if (Type is ProjectType.Unknown or ProjectType.FLEx)
        {
            return loader.LoadAsync(Id);
        }
        return Task.FromResult(false);
    }
}

public record FieldWorksLiteProject(
    Guid Id,
    string Code,
    string Name,
    bool IsFwDataProject,
    bool IsCrdtProject,
    [property:JsonConverter(typeof(JsonStringEnumConverter))]ProjectRole Role);

public enum ProjectMigrationStatus
{
    //default value
    Unknown,
    Migrated,
    Migrating,
    PrivateRedmine,
    PublicRedmine,
}

public enum ResetStatus
{
    None = 0,
    InProgress = 1
}

public enum ProjectType
{
    Unknown = 0,
    FLEx = 1,
    WeSay = 2,
    OneStoryEditor = 3,
    OurWord = 4,
    AdaptIt = 5,
}

public class Changeset
{
    public required string Node { get; set; }
    public int Rev { get; set; }
    public required double[] Date { get; set; }
    public required string Desc { get; set; }

    public required string Branch { get; set; }

// commented out because I'm not sure of the shape and you can't use JsonArray as an output of gql
    // public JsonArray Bookmarks { get; set; }
    public required string[] Tags { get; set; }
    public required string User { get; set; }
    public required string Phase { get; set; }
    public required string[] Parents { get; set; }
}
