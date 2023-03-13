using LexBoxApi.Models.Project;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Guid> CreateProject(CreateProjectInput input, Guid userId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var projectId = Guid.NewGuid();
        _dbContext.Projects.Add(
            new Project
            {
                Id = projectId,
                Code = input.Code,
                Name = input.Name,
                Description = input.Description,
                Type = input.Type,
                LastCommit = null,
                RetentionPolicy = input.RetentionPolicy,
                Users = new List<ProjectUsers> { new() { UserId = userId, Role = ProjectRole.Manager } }
            });
        await _dbContext.SaveChangesAsync();
        await _hgService.InitRepo(input.Code);
        await transaction.CommitAsync();
        return projectId;
    }

    public async Task<DateTimeOffset?> UpdateLastCommit(string projectCode)
    {
        var lastCommitFromHg = await _hgService.GetLastCommitTimeFromHg(projectCode);
        await _dbContext.Projects.Where(p => p.Code == projectCode)
            .ExecuteUpdateAsync(calls =>
                calls.SetProperty(project => project.LastCommit, lastCommitFromHg)
            );
        return lastCommitFromHg;
    }
}