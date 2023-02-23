using LexBoxApi.Models.Project;
using LexCore.Entities;
using LexData;

namespace LexBoxApi.Services;

public class ProjectService
{
    private readonly LexBoxDbContext _dbContext;
    private readonly HgService _hgService;

    public ProjectService(LexBoxDbContext dbContext, HgService hgService)
    {
        _dbContext = dbContext;
        _hgService = hgService;
    }

    public async Task<Guid> CreateProject(CreateProjectModel model, Guid userId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var projectId = Guid.NewGuid();
        _dbContext.Projects.Add(
            new Project
            {
                Id = projectId,
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                Type = model.Type,
                RetentionPolicy = model.RetentionPolicy,
                Users = new List<ProjectUsers>
                {
                    new()
                    {
                        UserId = userId,
                        Role = ProjectRole.Admin
                    }
                }
            });
        await _dbContext.SaveChangesAsync();
        await _hgService.InitRepo(model.Code);
        await transaction.CommitAsync();
        return projectId;
    }
}