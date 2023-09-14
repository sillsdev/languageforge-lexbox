using LexBoxApi.Auth;
using LexCore;
using LexCore.Auth;
using LexCore.Config;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class LexProxyService : ILexProxyService
{
    private readonly LexAuthService _lexAuthService;
    private readonly ProjectService _projectService;
    private readonly HgConfig _hgConfig;
    private readonly IMemoryCache _memoryCache;
    public LexProxyService(LexAuthService lexAuthService,
        ProjectService projectService,
        IOptions<HgConfig> options,
        IMemoryCache memoryCache)
    {
        _lexAuthService = lexAuthService;
        _projectService = projectService;
        _memoryCache = memoryCache;
        _hgConfig = options.Value;
    }

    public async Task<LexAuthUser?> Login(LoginRequest loginRequest)
    {
        var user = await _lexAuthService.Login(loginRequest);
        return user;
    }

    public async Task RefreshProjectLastChange(string projectCode)
    {
        await _projectService.UpdateLastCommit(projectCode);
    }

    public async ValueTask<string> GetDestinationPrefix(HgType type, string projectCode)
    {
        var projectMigrationInfo = await GetProjectMigrationInfo(projectCode);
        var result = (type, projectMigrationInfo) switch
        {
            (_ , ProjectMigrationStatus.Migrating) => throw new ProjectMigratingException(projectCode),
            //migrated projects
            (HgType.hgWeb, ProjectMigrationStatus.Migrated) => _hgConfig.HgWebUrl,
            (HgType.resumable, ProjectMigrationStatus.Migrated) => _hgConfig.HgResumableUrl,

            //not migrated projects
            (HgType.hgWeb, ProjectMigrationStatus.PublicRedmine) => _hgConfig.PublicRedmineHgWebUrl,
            (HgType.hgWeb, ProjectMigrationStatus.PrivateRedmine) => _hgConfig.PrivateRedmineHgWebUrl,
            //all resumable redmine go to the same place
            (HgType.resumable, ProjectMigrationStatus.PublicRedmine) => _hgConfig.RedmineHgResumableUrl,
            (HgType.resumable, ProjectMigrationStatus.PrivateRedmine) => _hgConfig.RedmineHgResumableUrl,
            _ => throw new ArgumentException($"Unknown HG request type: {type}")
        };
        return result;
    }

    private async ValueTask<ProjectMigrationStatus> GetProjectMigrationInfo(string projectCode)
    {
        var cacheKey = $"ProjectMigrationInfo_{projectCode}";
        if (_memoryCache.TryGetValue(cacheKey, out ProjectMigrationStatus migrationInfo) && migrationInfo is not ProjectMigrationStatus.Unknown)
        {
            return migrationInfo;
        }
        migrationInfo = await _projectService.GetProjectMigrationStatus(projectCode);
        _memoryCache.Set(cacheKey, migrationInfo, TimeSpan.FromMinutes(10));
        return migrationInfo;
    }
}
