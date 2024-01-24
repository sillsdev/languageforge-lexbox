using LexBoxApi.Models.Project;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LexBoxApi.Services;

public class ProjectService(LexBoxDbContext dbContext, IHgService hgService, IRepoMigrationService migrationService, IMemoryCache memoryCache)
{
    public async Task<Guid> CreateProject(CreateProjectInput input, Guid userId)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var projectId = input.Id ?? Guid.NewGuid();
        dbContext.Projects.Add(
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
        await dbContext.SaveChangesAsync();
        await hgService.InitRepo(input.Code);
        await transaction.CommitAsync();
        return projectId;
    }

    public async Task<bool> ProjectExists(string projectCode)
    {
        return await dbContext.Projects.AnyAsync(p => p.Code == projectCode);
    }

    public async ValueTask<Guid> LookupProjectId(string projectCode)
    {
        var cacheKey = $"ProjectIdForCode:{projectCode}";
        if (memoryCache.TryGetValue(cacheKey, out Guid projectId)) return projectId;
        projectId = await dbContext.Projects
            .Where(p => p.Code == projectCode)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        memoryCache.Set(cacheKey, projectId, TimeSpan.FromHours(1));
        return projectId;
    }

    public async Task<string?> BackupProject(ResetProjectByAdminInput input)
    {
        var backupFile = await hgService.BackupRepo(input.Code);
        return backupFile;
    }

    public async Task ResetProject(ResetProjectByAdminInput input)
    {
        var rowsAffected = await dbContext.Projects.Where(p => p.Code == input.Code && p.ResetStatus == ResetStatus.None)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.ResetStatus, ResetStatus.InProgress));
        if (rowsAffected == 0) throw new NotFoundException($"project {input.Code} not ready for reset, either already reset or not found");
        await hgService.ResetRepo(input.Code);
    }

    public async Task FinishReset(string code, Stream? zipFile = null)
    {
        var project = await dbContext.Projects.Where(p => p.Code == code).SingleOrDefaultAsync();
        if (project is null) throw new NotFoundException($"project {code} not found");
        if (project.ResetStatus != ResetStatus.InProgress) throw ProjectResetException.ResetNotStarted(code);
        if (zipFile is not null)
        {
            await hgService.FinishReset(code, zipFile);
            project.LastCommit = await hgService.GetLastCommitTimeFromHg(project.Code, project.MigrationStatus);
        }
        project.ResetStatus = ResetStatus.None;
        await dbContext.SaveChangesAsync();
    }

    public async Task<DateTimeOffset?> UpdateLastCommit(string projectCode)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project is null) return null;
        var lastCommitFromHg = await hgService.GetLastCommitTimeFromHg(projectCode, project.MigrationStatus);
        project.LastCommit = lastCommitFromHg;
        await dbContext.SaveChangesAsync();
        return lastCommitFromHg;
    }

    public async Task<int?> GetLexEntryCount(string projectCode)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project?.MigrationStatus is not ProjectMigrationStatus.Migrated) return null;
        if (project?.Type is not ProjectType.FLEx) return null;
        return await hgService.GetLexEntryCount(projectCode);
    }

    public async Task<int?> UpdateLexEntryCount(string projectCode)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project?.MigrationStatus is not ProjectMigrationStatus.Migrated) return null;
        if (project?.Type is not ProjectType.FLEx) return null;
        var count = await hgService.GetLexEntryCount(projectCode);
        if (project.FlexProjectMetadata is null)
        {
            project.FlexProjectMetadata = new FlexProjectMetadata { LexEntryCount = count };
        }
        else
        {
            project.FlexProjectMetadata.LexEntryCount = count;
        }
        await dbContext.SaveChangesAsync();
        return count;
    }

    public async Task<ProjectMigrationStatus?> GetProjectMigrationStatus(string projectCode)
    {
        var migrationStatus = await dbContext.Projects.AsNoTracking()
            .Where(p => p.Code == projectCode).Select(p => p.MigrationStatus).FirstOrDefaultAsync();
        if (migrationStatus == default) return null;
        return migrationStatus;
    }

    public async Task MigrateProject(string projectCode)
    {
        var project = await dbContext.Projects.SingleAsync(p => p.Code == projectCode);
        project.MigrationStatus = ProjectMigrationStatus.Migrating;
        await dbContext.SaveChangesAsync();
        migrationService.QueueMigration(projectCode);
    }

    public async Task MigrateProjects(string[] projectCodes)
    {
        var projects = await dbContext.Projects.Where(p => p.MigrationStatus != ProjectMigrationStatus.Migrated && projectCodes.Contains(p.Code)).ToArrayAsync();
        foreach (var project in projects)
        {
            project.MigrationStatus = ProjectMigrationStatus.Migrating;
        }
        await dbContext.SaveChangesAsync();
        foreach (var projectCode in projectCodes)
        {
            migrationService.QueueMigration(projectCode);
        }
    }

    public async Task<bool?> AwaitMigration(string projectCode, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(p => p.Code == projectCode, cancellationToken);
        if (project is null) return null;
        if (project.MigrationStatus == ProjectMigrationStatus.Migrated) return true;
        try
        {
            await migrationService.WaitMigrationFinishedAsync(projectCode, cancellationToken);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
