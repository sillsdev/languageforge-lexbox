using LexBoxApi.Models.Project;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class ProjectService
{
    private readonly LexBoxDbContext _dbContext;
    private readonly IHgService _hgService;
    private readonly IRepoMigrationService _migrationService;

    public ProjectService(LexBoxDbContext dbContext, IHgService hgService, IRepoMigrationService migrationService)
    {
        _dbContext = dbContext;
        _hgService = hgService;
        _migrationService = migrationService;
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
                ProjectOrigin = ProjectMigrationStatus.Migrated,
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

    public async Task<bool> ProjectExists(string projectCode)
    {
        return await _dbContext.Projects.AnyAsync(p => p.Code == projectCode);
    }

    public async Task<string?> BackupProject(ResetProjectByAdminInput input)
    {
        var backupFile = await _hgService.BackupRepo(input.Code);
        return backupFile;
    }

    public async Task ResetProject(ResetProjectByAdminInput input)
    {
        var rowsAffected = await _dbContext.Projects.Where(p => p.Code == input.Code && p.ResetStatus == ResetStatus.None)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.ResetStatus, ResetStatus.InProgress));
        if (rowsAffected == 0) throw new NotFoundException($"project {input.Code} not ready for reset, either already reset or not found");
        await _hgService.ResetRepo(input.Code);
    }

    public async Task FinishReset(string code, Stream zipFile)
    {
        var project = await _dbContext.Projects.Where(p => p.Code == code).SingleOrDefaultAsync();
        if (project is null) throw new NotFoundException($"project {code} not found");
        if (project.ResetStatus != ResetStatus.InProgress) throw new ProjectResetException($"project {code} not ready to finish reset");
        await _hgService.FinishReset(code, zipFile);
        project.ResetStatus = ResetStatus.None;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<DateTimeOffset?> UpdateLastCommit(string projectCode)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project is null) return null;
        var lastCommitFromHg = await _hgService.GetLastCommitTimeFromHg(projectCode, project.MigrationStatus);
        project.LastCommit = lastCommitFromHg;
        await _dbContext.SaveChangesAsync();
        return lastCommitFromHg;
    }

    public async Task<ProjectMigrationStatus?> GetProjectMigrationStatus(string projectCode)
    {
        var migrationStatus = await _dbContext.Projects.AsNoTracking()
            .Where(p => p.Code == projectCode).Select(p => p.MigrationStatus).FirstOrDefaultAsync();
        if (migrationStatus == default) return null;
        return migrationStatus;
    }

    public async Task MigrateProject(string projectCode)
    {
        var project = await _dbContext.Projects.SingleAsync(p => p.Code == projectCode);
        project.MigrationStatus = ProjectMigrationStatus.Migrating;
        await _dbContext.SaveChangesAsync();
        _migrationService.QueueMigration(projectCode);
    }

    public async Task<bool?> AwaitMigration(string projectCode, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.SingleOrDefaultAsync(p => p.Code == projectCode, cancellationToken);
        if (project is null) return null;
        if (project.MigrationStatus == ProjectMigrationStatus.Migrated) return true;
        try
        {
            await _migrationService.WaitMigrationFinishedAsync(projectCode, cancellationToken);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
