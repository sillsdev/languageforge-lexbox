using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using EntityFrameworkCore.Projectables;
using LexCore.ServiceInterfaces;

namespace LexCore.Entities;

public class Project : EntityBase
{
    public Guid? ParentId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required RetentionPolicy RetentionPolicy { get; set; }
    public required ProjectType Type { get; set; }
    public required List<ProjectUsers> Users { get; set; }
    public required DateTimeOffset? LastCommit { get; set; }
    public DateTimeOffset? DeletedDate { get; set; }

    public required ProjectMigrationStatus ProjectOrigin { get; set; } = ProjectMigrationStatus.Migrated;
    public required ProjectMigrationStatus MigrationStatus { get; set; } = ProjectMigrationStatus.Migrated;

    [NotMapped]
    [Projectable(UseMemberBody = nameof(SqlUserCount))]
    public int UserCount { get; set; }
    private static Expression<Func<Project, int>> SqlUserCount => project => project.Users.Count;

    public async Task<Changeset[]> GetChangesets(IHgService hgService)
    {
        var age = DateTimeOffset.UtcNow.Subtract(CreatedDate);
        if (age.TotalSeconds < 40 || MigrationStatus == ProjectMigrationStatus.Migrating)
        {
            // The repo is unstable and potentially unavailable for a short while after creation, so don't read from it right away.
            // See: https://github.com/sillsdev/languageforge-lexbox/issues/173#issuecomment-1665478630
            return Array.Empty<Changeset>();
        }
        else
        {
            return await hgService.GetChangesets(Code, MigrationStatus);
        }
    }
}

public enum ProjectMigrationStatus
{
    //default value
    Unknown,
    Migrated,
    Migrating,
    PrivateRedmine,
    PublicRedmine,
}

public enum ProjectType
{
    Unknown = 0,
    FLEx = 1,
    WeSay = 2,
    OneStoryEditor = 3,
    OurWord = 4
}

public class Changeset
{
    public string Node { get; set; }
    public double[] Date { get; set; }
    public string Desc { get; set; }

    public string Branch { get; set; }

// commented out because I'm not sure of the shape and you can't use JsonArray as an output of gql
    // public JsonArray Bookmarks { get; set; }
    public string[] Tags { get; set; }
    public string User { get; set; }
    public string Phase { get; set; }
    public string[] Parents { get; set; }
}
