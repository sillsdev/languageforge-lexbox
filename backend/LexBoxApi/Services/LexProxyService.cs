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
    private readonly UserService _userService;
    private readonly HgConfig _hgConfig;
    private readonly IMemoryCache _memoryCache;
    public LexProxyService(LexAuthService lexAuthService,
        ProjectService projectService,
        IOptions<HgConfig> options,
        IMemoryCache memoryCache,
        UserService userService)
    {
        _lexAuthService = lexAuthService;
        _projectService = projectService;
        _memoryCache = memoryCache;
        _userService = userService;
        _hgConfig = options.Value;
    }

    public async Task<LexAuthUser?> Login(LoginRequest loginRequest)
    {
        var (user, dbUser, _) = await _lexAuthService.Login(loginRequest);
        if (user is not null)
        {
            await _userService.UpdateUserLastActive(user.Id);
            await _userService.UpdatePasswordStrength(user.Id, loginRequest);
        }
        return user;
    }

    public async Task RefreshProjectLastChange(string projectCode)
    {
        await _projectService.UpdateLastCommit(projectCode);
    }

    public async Task UpdateLastEntryCountIfAllowed(string projectCode)
    {
        if (_hgConfig.AutoUpdateLexEntryCountOnSendReceive) {
            await _projectService.UpdateLexEntryCount(projectCode);
        }
    }

    public async ValueTask<RequestInfo?> GetDestinationPrefix(HgType type, string projectCode)
    {
        var maybeProjectMigrationInfo = await GetProjectMigrationInfo(projectCode);
        if (maybeProjectMigrationInfo is null) return null;
        var projectMigrationInfo = maybeProjectMigrationInfo.Value;
        var result = HgService.DetermineProjectUrlPrefix(type, projectCode, projectMigrationInfo, _hgConfig);
        string? trustToken = null;
        if (projectMigrationInfo is ProjectMigrationStatus.PrivateRedmine or ProjectMigrationStatus.PublicRedmine)
        {
            trustToken = _hgConfig.RedmineTrustToken;
        }
        return new RequestInfo(result, trustToken, projectMigrationInfo);
    }


    private async ValueTask<ProjectMigrationStatus?> GetProjectMigrationInfo(string projectCode)
    {
        var cacheKey = GetProjectMigrationInfoCacheKey(projectCode);
        if (_memoryCache.TryGetValue(cacheKey, out ProjectMigrationStatus migrationInfo) && migrationInfo is not ProjectMigrationStatus.Unknown)
        {
            return migrationInfo;
        }
        var maybeMigrationInfo = await _projectService.GetProjectMigrationStatus(projectCode);
        if (maybeMigrationInfo is null) return null;
        migrationInfo = maybeMigrationInfo.Value;
        _memoryCache.Set(cacheKey, migrationInfo, TimeSpan.FromMinutes(10));
        return migrationInfo;
    }

    private string GetProjectMigrationInfoCacheKey(string projectCode)
    {
        return $"ProjectMigrationInfo_{projectCode}";
    }

    public void ClearProjectMigrationInfo(string projectCode)
    {
        _memoryCache.Remove(GetProjectMigrationInfoCacheKey(projectCode));
    }
}
