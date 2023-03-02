using System.Security.Claims;
using LexBoxApi.Auth;
using LexCore;
using LexCore.Auth;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class ProxyAuthService : IProxyAuthService
{
    private readonly LexAuthService _lexAuthService;
    public ProxyAuthService(LexAuthService lexAuthService)
    {
        _lexAuthService = lexAuthService;
    }

    public async Task<ClaimsPrincipal?> Login(LoginRequest loginRequest)
    {
        var user = await _lexAuthService.Login(loginRequest);
        if (user is null) return null;
        return new ClaimsPrincipal(new ClaimsIdentity(user.GetClaims()));
    }
}