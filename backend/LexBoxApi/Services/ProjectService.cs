using System.Data.Common;
using System.Globalization;
using System.IO.Compression;
using LexBoxApi.Jobs;
using LexBoxApi.Models.Project;
using LexBoxApi.Services.Email;
using LexCore.Auth;
using LexCore.Config;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Path = System.IO.Path; // Resolves ambiguous reference with HotChocolate.Path

namespace LexBoxApi.Services;

public class ProjectService(
    LexBoxDbContext dbContext,
    IHgService hgService,
    IOptions<HgConfig> hgConfig,
    IMemoryCache memoryCache,
    IEmailService emailService,
    FwHeadlessClient fwHeadless)
{
    public async Task<Guid> CreateProject(CreateProjectInput input)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var projectId = input.Id ?? Guid.NewGuid();
        var theOrg = await dbContext.Orgs.FindAsync(input.OrgId);
        /* TODO #737 - Remove this draftProject/isConfidentialIsUntrustworthy stuff and just trust input.IsConfidential */
        var draftProject = await dbContext.DraftProjects.FindAsync(projectId);
        // There could be draft projects from before we introduced the IsConfidential field. (i.e. where draftProject.IsConfidential is null)
        // In those cases we can't trust input.IsConfidential == false, because that is the default, but the user will never have had the chance to pick it.
        var isConfidentialIsUntrustworthy = draftProject is not null && draftProject.IsConfidential is null && !input.IsConfidential;
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
                IsConfidential = isConfidentialIsUntrustworthy ? null : input.IsConfidential,
                Organizations = theOrg is not null ? [theOrg] : [],
                Users = input.ProjectManagerId.HasValue ? [new() { UserId = input.ProjectManagerId.Value, Role = ProjectRole.Manager }] : [],
                FlexProjectMetadata = input.Type == ProjectType.FLEx ? new() : null
            });
        // Also delete draft project, if any
        if (draftProject is not null)
        {
            dbContext.DraftProjects.Remove(draftProject);
        }

        var manager = input.ProjectManagerId.HasValue ? await dbContext.Users.FindAsync(input.ProjectManagerId.Value) : null;
        manager?.UpdateCreateProjectsPermission(ProjectRole.Manager);

        await dbContext.SaveChangesAsync();
        try
        {
            await hgService.InitRepo(input.Code);
            InvalidateProjectOrgIdsCache(projectId);
            InvalidateProjectConfidentialityCache(projectId);
            InvalidateProjectCodeCache(input.Code);
            await transaction.CommitAsync();
        }
        catch
        {
            // CommitAsync() did not run [successfully], so we don't want a repo to exist
            await hgService.DeleteRepoIfExists(input.Code);
            throw;
        }
        if (draftProject != null && manager != null)
        {
            await emailService.SendApproveProjectRequestEmail(manager, input);
        }
        return projectId;
    }

    public async Task UpdateProjectLangTags(Guid projectId)
    {
        var project = await dbContext.Projects.FindAsync(projectId);
        if (project is null || project.Type != ProjectType.FLEx) return;
        await dbContext.Entry(project).Reference(p => p.FlexProjectMetadata).LoadAsync();
        var langTags = await hgService.GetProjectWritingSystems(project.Code);
        if (langTags is null) return;
        project.FlexProjectMetadata ??= new FlexProjectMetadata();
        project.FlexProjectMetadata.WritingSystems = langTags;
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateProjectLangProjectId(Guid projectId)
    {
        var project = await dbContext.Projects.FindAsync(projectId);
        if (project is null || project.Type != ProjectType.FLEx) return;
        await dbContext.Entry(project).Reference(p => p.FlexProjectMetadata).LoadAsync();
        var langProjGuid = await hgService.GetProjectIdOfFlexProject(project.Code);
        project.FlexProjectMetadata ??= new FlexProjectMetadata();
        project.FlexProjectMetadata.LangProjectId = langProjGuid;
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateFLExModelVersion(Guid projectId)
    {
        var project = await dbContext.Projects.FindAsync(projectId);
        if (project is null || project.Type != ProjectType.FLEx) return;
        await dbContext.Entry(project).Reference(p => p.FlexProjectMetadata).LoadAsync();
        var modelVersion = await hgService.GetModelVersionOfFlexProject(project.Code);
        if (modelVersion is null) return;
        project.FlexProjectMetadata ??= new FlexProjectMetadata();
        project.FlexProjectMetadata.FlexModelVersion = modelVersion;
        await dbContext.SaveChangesAsync();
    }

    public async Task<Guid> CreateDraftProject(CreateProjectInput input)
    {
        var existingProject = await dbContext.Projects.FindAsync(input.Id);
        if (existingProject is not null)
        {
            throw new InvalidOperationException($"Project was already approved ({input.Id}: {existingProject.Code})");
        }

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
                IsConfidential = input.IsConfidential,
                RetentionPolicy = input.RetentionPolicy,
                ProjectManagerId = input.ProjectManagerId,
                OrgId = input.OrgId
            });
        await dbContext.SaveChangesAsync();
        return projectId;
    }

    public async Task<bool> ProjectExists(string projectCode)
    {
        return await dbContext.Projects.AnyAsync(p => p.Code == projectCode);
    }

    public async ValueTask<Guid?> LookupProjectId(string projectCode)
    {
        var cacheKey = $"ProjectIdForCode:{projectCode}";
        if (memoryCache.TryGetValue(cacheKey, out Guid? projectId)) return projectId;
        projectId = await dbContext.Projects
            .Where(p => p.Code == projectCode)
            .Select(p => (Guid?) p.Id)
            .FirstOrDefaultAsync();
        memoryCache.Set(cacheKey, projectId, TimeSpan.FromHours(1));
        return projectId;
    }

    public void InvalidateProjectCodeCache(string projectCode)
    {
        try { memoryCache.Remove($"ProjectIdForCode:{projectCode}"); }
        catch (Exception) { }; // Never allow this to throw
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
        var projectId = await LookupProjectId(input.Code);
        if (projectId is null) throw new NotFoundException($"project {input.Code} not found", nameof(Project));
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var rowsAffected = await dbContext.Projects.Where(p => p.Id == projectId.Value && p.ResetStatus == ResetStatus.None)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.ResetStatus, ResetStatus.InProgress)
                .SetProperty(p => p.RepoSizeInKb, 0)
                .SetProperty(p => p.LastCommit, null as DateTimeOffset?));
        if (rowsAffected == 0) throw new NotFoundException($"project {input.Code} not ready for reset or already reset", nameof(Project));
        await fwHeadless.DeleteRepo(projectId.Value);
        await ResetLexEntryCount(input.Code);
        await hgService.ResetRepo(input.Code);
        await transaction.CommitAsync();
    }

    public async Task FinishReset(string code, Stream? zipFile = null)
    {
        var project = await dbContext.Projects.Include(p => p.FlexProjectMetadata).Where(p => p.Code == code).SingleOrDefaultAsync();
        if (project is null) throw new NotFoundException($"project {code} not found", nameof(Project));
        if (project.ResetStatus != ResetStatus.InProgress) throw ProjectResetException.ResetNotStarted(code);
        if (zipFile is not null)
        {
            await hgService.FinishReset(code, zipFile);
            await UpdateProjectMetadata(project);
        }
        else
        {
            await hgService.InvalidateDirCache(code);
        }
        project.ResetStatus = ResetStatus.None;
        project.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();
    }

    public async Task<Project?> DeleteProjectPermanently(Guid projectId)
    {
        // This method should only be called for test projects, like those created during E2E tests
        var project = await dbContext.Projects.FindAsync(projectId);
        // These checks *should* be redundant with the ProjectController's checks, but do them again anyway
        if (project is null) return null;
        if (project.RetentionPolicy != RetentionPolicy.Dev) return null;
        // do this first, because it throws if a sync is happening
        await fwHeadless.DeleteProject(projectId);
        dbContext.Projects.Remove(project);
        await hgService.DeleteRepoIfExists(project.Code);
        project.UpdateUpdatedDate();
        // Don't forget to add more Invalidate calls here if we add new caches
        InvalidateProjectCodeCache(project.Code);
        InvalidateProjectConfidentialityCache(projectId);
        InvalidateProjectOrgIdsCache(projectId);
        await dbContext.SaveChangesAsync();
        return project;
    }

    public async ValueTask<Guid[]> LookupProjectOrgIds(Guid projectId)
    {
        var cacheKey = $"ProjectOrgsForId:{projectId}";
        if (memoryCache.TryGetValue(cacheKey, out Guid[]? orgIds)) return orgIds ?? [];
        orgIds = await dbContext.Projects
            .Where(p => p.Id == projectId)
            .Select(p => p.Organizations.Select(o => o.Id).ToArray())
            .FirstOrDefaultAsync();
        memoryCache.Set(cacheKey, orgIds, TimeSpan.FromHours(1));
        return orgIds ?? [];
    }

    public void InvalidateProjectOrgIdsCache(Guid projectId)
    {
        try { memoryCache.Remove($"ProjectOrgsForId:{projectId}"); }
        catch (Exception) { } // Never allow this to throw
    }

    public async ValueTask<bool?> LookupProjectConfidentiality(Guid projectId)
    {
        var cacheKey = $"ProjectConfidentiality:{projectId}";
        if (memoryCache.TryGetValue(cacheKey, out bool? confidential)) return confidential;
        var project = await dbContext.Projects.FindAsync(projectId);
        memoryCache.Set(cacheKey, project?.IsConfidential, TimeSpan.FromHours(1));
        return project?.IsConfidential;
    }

    public void InvalidateProjectConfidentialityCache(Guid projectId)
    {
        try { memoryCache.Remove($"ProjectConfidentiality:{projectId}"); }
        catch (Exception) { } // Never allow this to throw
    }

    public async Task UpdateProjectMetadataForCode(string projectCode)
    {
        var project = await dbContext.Projects
            .Include(p => p.FlexProjectMetadata)
            .FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project is null) return;
        await UpdateProjectMetadata(project);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateProjectMetadata(Project project)
    {
        if (hgConfig.Value.AutoUpdateLexEntryCountOnSendReceive && project is { Type: ProjectType.FLEx } or { Type: ProjectType.WeSay })
        {
            var count = await hgService.GetLexEntryCount(project.Code, project.Type);
            if (project.FlexProjectMetadata is null)
            {
                project.FlexProjectMetadata = new FlexProjectMetadata { LexEntryCount = count };
            }
            else
            {
                project.FlexProjectMetadata.LexEntryCount = count;
            }
        }
        else
        {
            await hgService.InvalidateDirCache(project.Code);
        }

        project.LastCommit = await hgService.GetLastCommitTimeFromHg(project.Code);
        project.RepoSizeInKb = await hgService.GetRepoSizeInKb(project.Code);
        // Caller is responsible for caling dbContext.SaveChangesAsync()
    }

    public async Task<int?> UpdateRepoSizeInKb(string projectCode)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project is null) return null;
        var size = await hgService.GetRepoSizeInKb(projectCode);
        project.RepoSizeInKb = size;
        await dbContext.SaveChangesAsync();
        return size;
    }

    public async Task ResetLexEntryCount(string projectCode)
    {
        var project = await dbContext.Projects
            .Include(p => p.FlexProjectMetadata)
            .FirstOrDefaultAsync(p => p.Code == projectCode);
        if (project is null) return;
        if (project.FlexProjectMetadata is not null)
        {
            project.FlexProjectMetadata.LexEntryCount = null;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<DirectoryInfo?> ExtractLdmlZip(Project project, string destRoot, CancellationToken token = default)
    {
        if (project.Type != ProjectType.FLEx) return null;
        using var zip = await hgService.GetLdmlZip(project.Code, token);
        if (zip is null) return null;
        var path = Path.Join(destRoot, project.Id.ToString());
        if (Directory.Exists(path)) Directory.Delete(path, true);
        var dirInfo = Directory.CreateDirectory(path);
        zip.ExtractToDirectory(dirInfo.FullName, true);
        return dirInfo;
    }

    public async Task<string?> PrepareLdmlZip(Quartz.ISchedulerFactory schedulerFactory, CancellationToken token = default)
    {
        var nowStr = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var path = Path.Join(Path.GetTempPath(), $"sldr-export-{nowStr}");
        if (Directory.Exists(path)) Directory.Delete(path, true);
        Directory.CreateDirectory(path);
        await DeleteTempDirectoryJob.Queue(schedulerFactory, path, TimeSpan.FromHours(4));
        var zipRoot = Path.Join(path, "zipRoot");
        Directory.CreateDirectory(zipRoot);
        await foreach (var project in dbContext.Projects.Where(p => p.Type == ProjectType.FLEx).AsAsyncEnumerable())
        {
            await ExtractLdmlZip(project, zipRoot, token);
        }
        var zipFilename = $"sldr-{nowStr}.zip";
        var zipFilePath = Path.Join(path, zipFilename);
        if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
        // If we would create an empty .zip file, just return null instead (will become a 404)
        if (!Directory.EnumerateDirectories(zipRoot).Any()) return null;
        ZipFile.CreateFromDirectory(zipRoot, zipFilePath, CompressionLevel.Fastest, includeBaseDirectory: false);
        return zipFilePath;
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
        var count = await hgService.GetLexEntryCount(project.Code, project.Type);
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

    public IQueryable<Project> UserProjects(Guid userId)
    {
        return dbContext.Projects.Where(p => p.Users.Select(u => u.UserId).Contains(userId));
    }

    public bool IsCrdtProject(Guid projectId)
    {
        return dbContext.CrdtCommits(projectId).Any();
    }
}
