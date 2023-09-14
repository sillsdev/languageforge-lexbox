using LexBoxApi.Models.Project;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class ProjectService
{
    private readonly LexBoxDbContext _dbContext;
    private readonly IHgService _hgService;

    public ProjectService(LexBoxDbContext dbContext, IHgService hgService)
    {
        _dbContext = dbContext;
        _hgService = hgService;
    }

    public async Task<Guid> CreateProject(CreateProjectInput input, Guid userId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var projectId = input.Id ?? Guid.NewGuid();
        _dbContext.Projects.Add(
            new Project
            {
                Id = projectId,
                Code = input.Code,
                Name = input.Name,
                MigrationStatus = ProjectMigrationStatus.Migrated,
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

    public async Task<string?> BackupProject(ResetProjectByAdminInput input)
    {
        var backupFile = await _hgService.BackupRepo(input.Code);
        return backupFile;
    }

    public async Task ResetProject(ResetProjectByAdminInput input)
    {
        await _hgService.ResetRepo(input.Code);
    }

    public async Task<DateTimeOffset?> UpdateLastCommit(string projectCode)
    {
        var lastCommitFromHg = await _hgService.GetLastCommitTimeFromHg(projectCode);
        await _dbContext.Projects.Where(p => p.Code == projectCode)
            .ExecuteUpdateAsync(_ =>
                _.SetProperty(project => project.LastCommit, lastCommitFromHg)
            );
        return lastCommitFromHg;
    }

    public async Task<ProjectMigrationStatus> GetProjectMigrationStatus(string projectCode)
    {
        //todo remove this when we have the migration info in the db
        return ProjectMigrationStatus.Migrated;
        // var project = await _dbContext.Projects.AsNoTracking()
        // .Where(p => p.Code == projectCode)
        // .Select(p => new ProjectMigrationInfo(p.IsMigrated, p.RedminePublic))
        // .FirstOrDefaultAsync();
        // return project;
    }
}
