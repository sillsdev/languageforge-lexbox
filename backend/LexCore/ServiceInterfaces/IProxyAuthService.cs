using System.Security.Claims;

namespace LexCore.ServiceInterfaces;

public interface IProxyAuthService
{
    Task<ClaimsPrincipal?> Login(LoginRequest loginRequest);
}