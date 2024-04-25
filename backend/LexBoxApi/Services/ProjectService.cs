using System.Data.Common;
using LexBoxApi.Models.Project;
using LexCore.Config;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class ProjectService(LexBoxDbContext dbContext, IHgService hgService, IOptions<HgConfig> hgConfig, IMemoryCache memoryCache)
{
    public async Task<Guid> CreateProject(CreateProjectInput input)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var projectId = input.Id ?? Guid.NewGuid();
        dbContext.Projects.Add(
            new Project
            {
                Id = projectId,
                Code = input.Code,
                Name = input.Name,
                ProjectOrigin = ProjectMigrationStatus.Migrated,
                Description = input.Description,
                Type = input.Type,
                LastCommit = null,
                RetentionPolicy = input.RetentionPolicy,
                Users = input.ProjectManagerId.HasValue ? [new() { UserId = input.ProjectManagerId.Value, Role = ProjectRole.Manager }] : [],
            });
        // Also delete draft project, if any
        await dbContext.DraftProjects.Where(dp => dp.Id == projectId).ExecuteDeleteAsync();
        if (input.ProjectManagerId.HasValue)
        {
            var manager = await dbContext.Users.FindAsync(input.ProjectManagerId.Value);
            manager?.UpdateCreateProjectsPermission(ProjectRole.Manager);

        }
        await dbContext.SaveChangesAsync();
        await hgService.InitRepo(input.Code);
        await transaction.CommitAsync();
        return projectId;
    }

    public async Task<Guid> CreateDraftProject(CreateProjectInput input)
    {
        // No need for a transaction if we're just saving a single item
        var projectId = input.Id ?? Guid.NewGuid();
        dbContext.DraftProjects.Add(
            new DraftProject
            {
                Id = projectId,
                Code = input.Code,
                Name = input.Name,
                Description = input.Description,
                Type = input.Type,
                RetentionPolicy = input.RetentionPolicy,
                ProjectManagerId = input.ProjectManagerId,
            });
        await dbContext.SaveChangesAsync();
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

    public async Task<BackupExecutor?> BackupProject(string code)
    {
        var exists = await dbContext.Projects.Where(p => p.Code == code)
            .AnyAsync();
        if (!exists) return null;
        var backupExecutor = hgService.BackupRepo(code);
        return backupExecutor;
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
            project.LastCommit = await hgService.GetLastCommitTimeFromHg(project.Code);
        }
        project.ResetStatus = ResetStatus.None;
        project.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateProjectMetadata(string projectCode)
    {
        var project = await dbContext.Projects
            .Include(p => p.FlexProjectMetadata)
            .FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project is null) return;
        if (hgConfig.Value.AutoUpdateLexEntryCountOnSendReceive && project is { Type: ProjectType.FLEx } or { Type: ProjectType.WeSay })
        {
            var count = await hgService.GetLexEntryCount(projectCode, project.Type);
            if (project.FlexProjectMetadata is null)
            {
                project.FlexProjectMetadata = new FlexProjectMetadata { LexEntryCount = count };
            }
            else
            {
                project.FlexProjectMetadata.LexEntryCount = count;
            }
        }

        project.LastCommit = await hgService.GetLastCommitTimeFromHg(projectCode);
        await dbContext.SaveChangesAsync();
    }

    public async Task<DateTimeOffset?> UpdateLastCommit(string projectCode)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project is null) return null;
        var lastCommitFromHg = await hgService.GetLastCommitTimeFromHg(projectCode);
        project.LastCommit = lastCommitFromHg;
        await dbContext.SaveChangesAsync();
        return lastCommitFromHg;
    }

    public async Task<int?> UpdateLexEntryCount(string projectCode)
    {
        var project = await dbContext.Projects.Include(p => p.FlexProjectMetadata).FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project?.Type is not (ProjectType.FLEx or ProjectType.WeSay)) return null;
        var count = await hgService.GetLexEntryCount(projectCode, project.Type);
        if (project.FlexProjectMetadata is null)
        {
            project.FlexProjectMetadata = new FlexProjectMetadata { LexEntryCount = count };
        }
        else
        {
            project.FlexProjectMetadata.LexEntryCount = count;
        }
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbException e) when (e.SqlState == "23505")
        {
            // 23505 is "Duplicate key value violates unique constraint", i.e. another process
            // already created the FlexProjectMetadata entry with the correct value.
            // We'll silently ignore it since the other process has already succeeded
        }
        return count;
    }
}
