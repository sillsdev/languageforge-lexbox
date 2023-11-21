using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LexBoxApi.Services;

public class PermissionService : IPermissionService
{
    private readonly LoggedInContext _loggedInContext;
    private readonly LexBoxDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private LexAuthUser? User => _loggedInContext.MaybeUser;

    public PermissionService(LoggedInContext loggedInContext,
        LexBoxDbContext dbContext,
        IMemoryCache memoryCache)
    {
        _loggedInContext = loggedInContext;
        _dbContext = dbContext;
        _memoryCache = memoryCache;
    }

    public async ValueTask<bool> CanAccessProject(string projectCode)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        return CanAccessProject(await LookupProjectId(projectCode));
    }

    public bool CanAccessProject(Guid projectId)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        return User.Projects.Any(p => p.ProjectId == projectId);
    }

    public async ValueTask AssertCanAccessProject(string projectCode)
    {
        if (!await CanAccessProject(projectCode)) throw new UnauthorizedAccessException();
    }

    public bool CanManageProject(Guid projectId)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        return User.Projects.Any(p => p.ProjectId == projectId && p.Role == ProjectRole.Manager);
    }

    public void AssertCanManageProject(Guid projectId)
    {
        if (!CanManageProject(projectId)) throw new UnauthorizedAccessException();
    }

    public void AssertCanManageProjectMemberRole(Guid projectId, Guid userId)
    {
        if (User is null) throw new UnauthorizedAccessException();
        AssertCanManageProject(projectId);
        if (User.Role != UserRole.admin && userId == User.Id)
            throw new UnauthorizedAccessException("Not allowed to change own project role.");
    }

    private async ValueTask<Guid> LookupProjectId(string projectCode)
    {
        var cacheKey = $"ProjectIdForCode:{projectCode}";
        if (_memoryCache.TryGetValue(cacheKey, out Guid projectId)) return projectId;
        projectId = await _dbContext.Projects
            .Where(p => p.Code == projectCode)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        _memoryCache.Set(cacheKey, projectId, TimeSpan.FromHours(1));
        return projectId;
    }

    public void AssertIsAdmin()
    {
        if (User is not { Role: UserRole.admin }) throw new UnauthorizedAccessException();
    }

    public void AssertCanDeleteAccount(Guid userId)
    {
        if (User is { Role: UserRole.admin } || User?.Id == userId)
            return;
        throw new UnauthorizedAccessException();
    }

    public bool HasProjectCreatePermission()
    {
        return User is { CanCreateProjects: true } or { Role: UserRole.admin };
    }
}
