using LexBoxApi.Auth;
using LexCore;
using LexCore.Auth;
using LexCore.Config;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class LexProxyService : ILexProxyService
{
    private readonly LexAuthService _lexAuthService;
    private readonly ProjectService _projectService;
    private readonly UserService _userService;
    private readonly HgConfig _hgConfig;

    public LexProxyService(LexAuthService lexAuthService,
        ProjectService projectService,
        IOptions<HgConfig> options,
        UserService userService)
    {
        _lexAuthService = lexAuthService;
        _projectService = projectService;
        _userService = userService;
        _hgConfig = options.Value;
    }

    public async Task<LexAuthUser?> Login(LoginRequest loginRequest)
    {
        var (user, _) = await _lexAuthService.Login(loginRequest);
        if (user is not null) await _userService.UpdateUserLastActive(user.Id);
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

    public RequestInfo GetDestinationPrefix(HgType type)
    {
        return new RequestInfo(HgService.DetermineProjectUrlPrefix(type, _hgConfig));
    }
}
