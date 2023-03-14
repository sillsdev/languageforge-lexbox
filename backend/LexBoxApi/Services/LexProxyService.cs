using System.Security.Claims;
using LexBoxApi.Auth;
using LexCore;
using LexCore.ServiceInterfaces;

namespace LexBoxApi.Services;

public class LexProxyService : ILexProxyService
{
    private readonly LexAuthService _lexAuthService;
    private readonly ProjectService _projectService;
    public LexProxyService(LexAuthService lexAuthService, ProjectService projectService)
    {
        _lexAuthService = lexAuthService;
        _projectService = projectService;
    }

    public async Task<ClaimsPrincipal?> Login(LoginRequest loginRequest)
    {
        var user = await _lexAuthService.Login(loginRequest);
        if (user is null) return null;
        return new ClaimsPrincipal(new ClaimsIdentity(user.GetClaims()));
    }

    public async Task RefreshProjectLastChange(string projectCode)
    {
        await _projectService.UpdateLastCommit(projectCode);
    }
}