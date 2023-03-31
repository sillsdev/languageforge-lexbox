using System.Security.Claims;
using LexBoxApi.Auth;
using LexCore;
using LexCore.Auth;
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

    public async Task<LexAuthUser?> Login(LoginRequest loginRequest)
    {
        var user = await _lexAuthService.Login(loginRequest);
        return user;
    }

    public async Task RefreshProjectLastChange(string projectCode)
    {
        await _projectService.UpdateLastCommit(projectCode);
    }
}