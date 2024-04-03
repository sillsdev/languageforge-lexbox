using LexBoxApi.Auth;
using LexBoxApi.Jobs;
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
    private readonly Quartz.ISchedulerFactory _schedulerFactory;
    private readonly HgConfig _hgConfig;

    public LexProxyService(LexAuthService lexAuthService,
        ProjectService projectService,
        IOptions<HgConfig> options,
        Quartz.ISchedulerFactory schedulerFactory,
        UserService userService)
    {
        _lexAuthService = lexAuthService;
        _projectService = projectService;
        _userService = userService;
        _schedulerFactory = schedulerFactory;
        _hgConfig = options.Value;
    }

    public async Task<LexAuthUser?> Login(LoginRequest loginRequest)
    {
        var (user, _) = await _lexAuthService.Login(loginRequest);
        if (user is not null) await _userService.UpdateUserLastActive(user.Id);
        return user;
    }

    public async Task QueueProjectMetadataUpdate(string projectCode)
    {
        await UpdateProjectMetadataJob.Queue(_schedulerFactory, projectCode);
    }

    public RequestInfo GetDestinationPrefix(HgType type)
    {
        return new RequestInfo(HgService.DetermineProjectUrlPrefix(type, _hgConfig));
    }
}
