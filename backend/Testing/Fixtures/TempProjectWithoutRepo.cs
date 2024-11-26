using LexCore.Entities;
using LexData;
using Testing.Services;

namespace Testing.Fixtures;

public class TempProjectWithoutRepo(LexBoxDbContext dbContext, Project project) : IAsyncDisposable
{
    public Project Project => project;
    public static async Task<TempProjectWithoutRepo> Create(LexBoxDbContext dbContext, bool isConfidential = false, Guid? managerId = null)
    {
        var config = Utils.GetNewProjectConfig(isConfidential: isConfidential);
        var project = new Project
        {
            Name = config.Name,
            Code = config.Code,
            IsConfidential = config.IsConfidential,
            LastCommit = null,
            Organizations = [],
            Users = [],
            RetentionPolicy = RetentionPolicy.Test,
            Type = ProjectType.FLEx,
            Id = config.Id,
        };
        if (managerId is Guid id)
        {
            project.Users.Add(new ProjectUsers { ProjectId = project.Id, UserId = id, Role = ProjectRole.Manager });
        }
        dbContext.Add(project);
        await dbContext.SaveChangesAsync();
        return new TempProjectWithoutRepo(dbContext, project);
    }

    public async ValueTask DisposeAsync()
    {
        dbContext.Remove(project);
        await dbContext.SaveChangesAsync();
    }
}
